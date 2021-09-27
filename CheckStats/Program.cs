using OpenHardwareMonitor.Hardware;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;

namespace CheckStats
{
    internal partial class Program : IVisitor
    {
        private static Computer _computer;
        private static HttpClient _client;
        private static BaseModel _model;
        #region OpenHardwareMonitor
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
        #endregion
        private static void Main(string[] args)
        {
            Task.Run(CreateTask);
            _client = new HttpClient();
            Program program = new Program();
            _computer = new Computer()
            {
                CPUEnabled = true,
                RAMEnabled = true,
                MainboardEnabled = true,
                GPUEnabled = true,
                HDDEnabled = true
            };
            _computer.Accept(program);
            Timer timer = new Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
            };
            timer.Elapsed += new ElapsedEventHandler(SendInfoAsync);
            Console.Read();
        }

        private static async void SendInfoAsync(object source, ElapsedEventArgs e)
        {
            _model = new BaseModel();
            _computer.Open();
            SetProperties();
            _computer.Close();
            string json = new JavaScriptSerializer().Serialize(_model);
            _model = null;
            try
            {
                await _client.PostAsync(ConfigurationManager.AppSettings.Get("Server").First().ToString(), new StringContent(json));
            }
            catch { }
            Console.WriteLine(json);
            GC.Collect();
        }

        private static void SetProperties()
        {
            _model.Domain = Environment.UserDomainName;
            _model.DesktopName = Environment.MachineName;
            _model.OS = Environment.OSVersion.VersionString;
            _model.MACAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                x.OperationalStatus == OperationalStatus.Up)
                .First().GetPhysicalAddress().ToString();
            _model.IPAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Select(x => x.GetIPProperties())
                .Where(x => x.GatewayAddresses
                .Where(g => g.Address.AddressFamily == AddressFamily.InterNetwork).Count() > 0)
                .FirstOrDefault()?.UnicastAddresses?
                .Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?
                .FirstOrDefault()?.Address.ToString();
            _model.User = GetArray<User>(WMIClasses.UserAccount);
            _model.Motherboard = GetArray<Motherboard>(WMIClasses.Motherboard)[0];
            _model.Processor = GetArray<CPU>(WMIClasses.Processor)[0];
            var cpu = _computer.Hardware
                .First(x => x.HardwareType == HardwareType.CPU);
            _model.Processor.Temperature = cpu.Sensors
                .Where(x => x.SensorType == SensorType.Temperature)
                .Select(x => x.Value)
                .ToArray();
            _model.Processor.Load = cpu.Sensors
                .Where(x => x.SensorType == SensorType.Load)
                .Select(x => x.Value).First();
            _model.VideoAdapter = GetArray<VideoAdapter>(WMIClasses.VideoController);
            _model.RAM = GetArray<RAM>(WMIClasses.PhysicalMemory);
            _model.Monitor = GetArray<Monitor>(WMIClasses.DesktopMonitor);
            var d = GetArray<MonitorI>(WMIClasses.MonitorID, "\\\\.\\ROOT\\WMI");
            for (int i = 0; i < _model.Monitor.Length; i++)
            {
                _model.Monitor[i]._Monitor = d[i];
            }
            _model.Disk = GetArray<PhysicalDisk>(WMIClasses.DiskDrive);
            _model.LogicalPartition = GetArray<LogicalDisk>(WMIClasses.LogicalDisk);
        }

        private static T[] GetArray<T>(string className, string scope = null) where T : class, new()
        {
            var task = Task.Run(() => WMISearch<T>(className, scope));
            task.Wait();
            return task.Result;
        }

        private static T[] WMISearch<T>(string className, string scope) where T : class, new()
        {
            T[] array = null;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                new ManagementScope(scope),
                new ObjectQuery("select * from " + className)))
            {
                array = new T[searcher.Get().Count];
                int count = 0;
                foreach (var item in searcher.Get())
                {
                    T model = new T();
                    foreach (var property in item.Properties)
                    {
                        try
                        {
                            var modelProperty = typeof(T).GetProperty(property.Name);
                            if (modelProperty != null)
                            {
                                var value = item.GetPropertyValue(property.Name);
                                if (value != null)
                                {
                                    if (value.GetType() == typeof(string))
                                        value = value.ToString().Trim();
                                    modelProperty.SetValue(model, value);
                                }
                            }
                        }
                        catch { }
                    }
                    array[count] = model;
                    count++;
                }
            }
            return array;
        }

        private static void CreateTask()
        {
            if (ConfigurationManager.AppSettings.Get("NeedTask").First() == '1')
            {
                string path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "createTask.ps1");
                File.WriteAllText(path,
                    $@"Get-ScheduledTask -TaskName ""SendStatsTask"" -ErrorAction SilentlyContinue -OutVariable task 
                                    if (!$task){{
                                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                                    $User = ""NT AUTHORITY\SYSTEM""
                                    $Action = New-ScheduledTaskAction -Execute ""{Process.GetCurrentProcess().MainModule.FileName}""
                                    Register-ScheduledTask -TaskName ""SendStatsTask"" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }}");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Verb = "runas",
                    Arguments = $@"powershell -executionpolicy remotesigned -File {path}",
                    //Arguments = $@"powershell -noexit -executionpolicy remotesigned -File {path}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var task = Process.Start(startInfo);
                task.WaitForExit();
                if (task.HasExited)
                    File.Delete(path);
                ConfigurationManager.AppSettings.Set("NeedTask", "0");
            }
        }
    }
}
