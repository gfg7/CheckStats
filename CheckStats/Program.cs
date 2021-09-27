using OpenHardwareMonitor.Hardware;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Timers;
using System.Web.Script.Serialization;

namespace CheckStats
{
    internal partial class Program : IVisitor
    {
        private static Computer computer { get; set; }
        private static BaseModel model { get; set; }
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
            #region Create Task 
            string path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "createTask.ps1");
            if (!File.Exists(path))
            {
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
                Process.Start(startInfo);
            }
            #endregion
            model = new BaseModel();
            Program program = new Program();
            computer = new Computer() { CPUEnabled = true, RAMEnabled = true, MainboardEnabled = true, GPUEnabled = true, HDDEnabled = true };
            computer.Open();
            computer.Accept(program);
            #region Timer
            //Timer timer = new Timer()
            //{
            //    AutoReset = true,
            //    Enabled = true,
            //    Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
            //};
            //timer.Elapsed += new ElapsedEventHandler(UpdateInfo);
            #endregion
            UpdateInfo(null, null);
            computer.Close();
        }

        private static void UpdateInfo(object source, ElapsedEventArgs e)
        {
            #region Set properties
            model.Domain = Environment.UserDomainName;
            model.DesktopName = Environment.MachineName;
            model.OS = Environment.OSVersion.VersionString;
            model.MACAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                x.OperationalStatus == OperationalStatus.Up)
                .First().GetPhysicalAddress().ToString();
            model.IPAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Select(x => x.GetIPProperties())
                .Where(x => x.GatewayAddresses
                .Where(g => g.Address.AddressFamily == AddressFamily.InterNetwork).Count() > 0)
                .FirstOrDefault()?.UnicastAddresses?
                .Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?
                .FirstOrDefault()?.Address.ToString();
            model.User = GetArray<User>(WMIClasses.UserAccount);
            model.Motherboard = GetArray<Motherboard>(WMIClasses.Motherboard)[0];
            model.Processor = GetArray<CPU>(WMIClasses.Processor)[0];
            var cpu = computer.Hardware
                .First(x => x.HardwareType == HardwareType.CPU);
            model.Processor.Temperature = cpu.Sensors
                .Where(x => x.SensorType == SensorType.Temperature)
                .Select(x => x.Value)
                .ToArray();
            model.Processor.Load = cpu.Sensors
                .Where(x => x.SensorType == SensorType.Load)
                .Select(x => x.Value).First();
            model.VideoAdapter = GetArray<VideoAdapter>(WMIClasses.VideoController);
            model.RAM = GetArray<RAM>(WMIClasses.PhysicalMemory);
            //var ram = computer.Hardware.Where(x => x.HardwareType == HardwareType.RAM);
            //for (int i = 0; i < 2; i++)
            //{
            //    model.RAMLoad = ram.Sensors
            //    .Where(x => x.SensorType == SensorType.Load && x.Name == model.RAM[i].Model)
            //    .Select(x => x.Value);
            //    model.RAM[i].Available = ram.Sensors
            //    .Where(x => x.SensorType == SensorType.Load && x.Name == model.RAM[i].Model)
            //    .Select(x => x.Value).First();
            //}
            model.Monitor = GetArray<Monitor>(WMIClasses.DesktopMonitor);
            var d = GetArray<MonitorI>(WMIClasses.MonitorID, "\\\\.\\ROOT\\WMI");
            for (int i = 0; i < model.Monitor.Length; i++)
            {
                model.Monitor[i]._Monitor = d[i];
            }
            model.Disk = GetArray<PhysicalDisk>(WMIClasses.DiskDrive);
            model.LogicalPartition = GetArray<LogicalDisk>(WMIClasses.LogicalDisk);
            #endregion
            string json = new JavaScriptSerializer().Serialize(model);
            model = null;
            Console.WriteLine(json);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            model = new BaseModel();
            Console.ReadKey();
        }

        private static T[] GetArray<T>(string className, string scope = null) where T : class, new()
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
    }
}
