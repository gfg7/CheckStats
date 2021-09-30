using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
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
        private static Computer c;
        private static HttpClient client;
        private static Program p;
        private static BaseModel model;

        private IHardware motherboard;
        private IHardware cpu;
        private IEnumerable<IHardware> gpu;
        private IEnumerable<IHardware> rams;
        private IEnumerable<IHardware> disks;

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
            Task.Run(FirstRun);
            model = new BaseModel();
            p = new Program();
            client = new HttpClient();
            c = new Computer()
            {
                CPUEnabled = true,
                RAMEnabled = true,
                MainboardEnabled = true,
                GPUEnabled = true,
                HDDEnabled = true,
                FanControllerEnabled = true
            };
            c.Accept(p);
            c.Open();
            p.VisitComputer(c);
            #region Static info
            model.DesktopName = Environment.MachineName;

            model.OS = Environment.OSVersion.VersionString;

            model.Motherboard = GetArray<Motherboard>(WMIClasses.Motherboard)[0];
            p.motherboard = c.Hardware
                .First(x => x.HardwareType == HardwareType.Mainboard)
                .SubHardware
                .First();

            model.Processor = GetArray<CPU>(WMIClasses.Processor)[0];
            p.cpu = c.Hardware
                .First(x => x.HardwareType == HardwareType.CPU);

            p.gpu = c.Hardware.Where(x => x.HardwareType == HardwareType.GpuAti || x.HardwareType == HardwareType.GpuNvidia);

            p.disks = c.Hardware.Where(x => x.HardwareType == HardwareType.HDD);

            p.rams= c.Hardware.Where(x => x.HardwareType == HardwareType.RAM);

            model.Monitor = GetArray<Monitor>(WMIClasses.DesktopMonitor);
            var d = GetArray<MonitorI>(WMIClasses.MonitorID, "\\\\.\\ROOT\\WMI");
            for (int i = 0; i < model.Monitor.Length; i++)
            {
                model.Monitor[i]._Monitor = d[i];
            }
            #endregion

            #region Timer
            //Timer timer = new Timer()
            //{
            //    AutoReset = true,
            //    Enabled = true,
            //    Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
            //};
            //timer.Elapsed += new ElapsedEventHandler(SendInfoAsync);
            #endregion

            SendInfoAsync(null, null);
            c.Close();
            Console.Read();
        }

        private static async void SendInfoAsync(object source, ElapsedEventArgs e)
        {
            SetDynamicProperties();
            string json = new JavaScriptSerializer().Serialize(model);
            try
            {
                await client.PostAsync(ConfigurationManager.AppSettings.Get("Server").First().ToString(), new StringContent(json));
            }
            catch { }
            Console.WriteLine(json);
            GC.Collect();
        }

        private static void SetDynamicProperties()
        {
            model.Domain = Environment.UserDomainName;

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

            p.VisitHardware(p.motherboard);
            model.Motherboard.Temperature = p.motherboard.Sensors
                .First(x => x.SensorType == SensorType.Temperature)
                .Value;

            p.VisitHardware(p.cpu);
            model.Processor.Temperature = p.cpu.Sensors
                .Where(x => x.SensorType == SensorType.Temperature)
                .Select(x => x.Value)
                .ToArray();
            model.Processor.Load = p.cpu.Sensors
                .Where(x => x.SensorType == SensorType.Load)
                .Select(x => x.Value).First();

            model.VideoAdapter = GetArray<VideoAdapter>(WMIClasses.VideoController);
            if (p.gpu is null)
            {

            }
            else
            {

            }

            #region RAM
            model.RAM = GetArray<RAM>(WMIClasses.PhysicalMemory);
            for (int i = 0; i < p.rams.Count(); i++)
            {
                var ram = p.rams.ElementAt(i);
                p.VisitHardware(ram);
                model.RAM[i].Available = ram.Sensors.First(x => x.Name == "Available Memory").Value;
                model.RAM[i].Used = ram.Sensors.First(x => x.Name == "Used Memory").Value;
            }
            #endregion

            model.Disk = GetArray<PhysicalDisk>(WMIClasses.DiskDrive);
            for (int i = 0; i < p.disks.Count(); i++)
            {
                var disk = p.disks.ElementAt(i);
                p.VisitHardware(disk);
                model.Disk.First(x => x.Model == disk.Name).Temperature = disk.Sensors.First(x => x.SensorType == SensorType.Temperature).Value;
            }

            model.LogicalPartition = GetArray<LogicalDisk>(WMIClasses.LogicalDisk);
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

        private static void FirstRun()
        {
            if (ConfigurationManager.AppSettings.Get("NeedTask").First() == '1')
            {
                string path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "createTask.ps1"),
                    dskchkPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "CheckDisk.ps1");
                File.WriteAllText(dskchkPath,
                    $@"$(Get-WmiObject -namespace root\wmi –class MSStorageDriver_FailurePredictStatus) *>&1 > {Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "output.txt")}");
                File.WriteAllText(path,
                    $@"Get-ScheduledTask -TaskName ""CheckDisk"" -ErrorAction SilentlyContinue -OutVariable chkdsk 
                                    if (!$chkdsk){{
                                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                                    $User = ""NT AUTHORITY\SYSTEM""
                                    $Action = New-ScheduledTaskAction -Execute ""{dskchkPath}""
                                    Register-ScheduledTask -TaskName ""CheckDisk"" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }}
                      Get-ScheduledTask -TaskName ""SendStats"" -ErrorAction SilentlyContinue -OutVariable task 
                                    if (!$task){{
                                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                                    $User = ""NT AUTHORITY\SYSTEM""
                                    $Action = New-ScheduledTaskAction -Execute ""{Process.GetCurrentProcess().MainModule.FileName}""
                                    Register-ScheduledTask -TaskName ""SendStats"" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Verb = "runas",
                    Arguments = $@"powershell -executionpolicy remotesigned -File {path}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };
                var task = Process.Start(startInfo);
                task.WaitForExit();
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                ConfigurationManager.AppSettings.Set("NeedTask", "0");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("NeedTask");
            }
        }
    }
}
