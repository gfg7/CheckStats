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
        private static readonly string programPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static readonly string resultsPath = Path.Combine(programPath, "CheckDiskHealth.txt");

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

        private static async Task Main(string[] args)
        {
            await FirstRunAsync();
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

            model.Motherboard = (await WMISearch<Motherboard>(WMIClasses.Motherboard))[0];
            p.motherboard = c.Hardware
                .First(x => x.HardwareType == HardwareType.Mainboard)
                .SubHardware
                .First();

            model.Processor = (await WMISearch<CPU>(WMIClasses.Processor))[0];
            p.cpu = c.Hardware
                .First(x => x.HardwareType == HardwareType.CPU);

            p.gpu = c.Hardware.Where(x => x.HardwareType == HardwareType.GpuAti || x.HardwareType == HardwareType.GpuNvidia);

            p.disks = c.Hardware.Where(x => x.HardwareType == HardwareType.HDD);

            p.rams = c.Hardware.Where(x => x.HardwareType == HardwareType.RAM);

            model.Monitor = await WMISearch<Monitor>(WMIClasses.DesktopMonitor);
            var d = await WMISearch<MonitorI>(WMIClasses.MonitorID, "\\\\.\\ROOT\\WMI");
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
            await SetDynamicProperties();
            string json = new JavaScriptSerializer().Serialize(model);
            try
            {
                await client.PostAsync(ConfigurationManager.AppSettings.Get("Server").First().ToString(), new StringContent(json));
            }
            catch { }
            Console.WriteLine(json);
            GC.Collect();
        }

        private static async Task SetDynamicProperties()
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

            model.User = await WMISearch<User>(WMIClasses.UserAccount);

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

            model.GPU = await WMISearch<VideoAdapter>(WMIClasses.VideoController);
            if (p.gpu is null)
            {
                model.GPU[0].Temperature = model.Motherboard.Temperature;
            }
            else
            {
                for (int i = 0; i < p.gpu.Count(); i++)
                {
                    var gpu = p.gpu.ElementAt(i);
                    p.VisitHardware(gpu);
                    model.GPU[i].Temperature = gpu.Sensors.First(x => x.SensorType == SensorType.Temperature).Value;
                }
            }

            model.RAM = await WMISearch<RAM>(WMIClasses.PhysicalMemory);
            for (int i = 0; i < p.rams.Count(); i++)
            {
                var ram = p.rams.ElementAt(i);
                p.VisitHardware(ram);
                model.RAM[i].Available = ram.Sensors.First(x => x.Name == "Available Memory").Value;
                model.RAM[i].Used = ram.Sensors.First(x => x.Name == "Used Memory").Value;
            }

            model.Disk = await WMISearch<PhysicalDisk>(WMIClasses.DiskDrive);
            for (int i = 0; i < p.disks.Count(); i++)
            {
                var disk = p.disks.ElementAt(i);
                p.VisitHardware(disk);
                model.Disk.First(x => x.Model == disk.Name).Temperature = disk.Sensors.First(x => x.SensorType == SensorType.Temperature).Value;
            }

            model.LogicalPartition = await WMISearch<LogicalDisk>(WMIClasses.LogicalDisk, null);
            await ReadResultFileAsync();
        }

        private static async Task<T[]> WMISearch<T>(string className, string scope = null) where T : class, new()
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

        private static async Task FirstRunAsync()
        {
            if (ConfigurationManager.AppSettings.Get("NeedTask").First() == '1')
            {
                string path = Path.Combine(programPath, "SendStats.ps1");
                string script = await Task.Run(ScanLogicalDisk);
                File.WriteAllText(path,
                    $@"Get-ScheduledTask -TaskName ""CheckDiskHealth"" -ErrorAction SilentlyContinue -OutVariable disk 
                    if (!$disk){{
                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                    $User = ""NT AUTHORITY\SYSTEM""
                    $Action = New-ScheduledTaskAction -Execute ""{script}""
                    Register-ScheduledTask -TaskName ""CheckDiskHealth"" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }}
                    Get-ScheduledTask -TaskName ""SendStats"" -ErrorAction SilentlyContinue -OutVariable task 
                    if (!$task){{
                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                    $User = ""NT AUTHORITY\SYSTEM""
                    $Action = New-ScheduledTaskAction -Execute ""{programPath}""
                    Register-ScheduledTask -TaskName ""SendStats"" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }}");
                await StartPowershell(path);
                ChangeConfig("NeedTask", "0");
            }
        }

        private static void ChangeConfig(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationManager.AppSettings.Set(key, value);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(key);
        }

        private static async Task<string> StartPowershell(string path, bool redirect = false)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Verb = "runas",
                Arguments = $@"powershell -executionpolicy remotesigned -File {path}",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = redirect
            };
            var task = Process.Start(startInfo);
            string result = null;
            if (redirect)
            {
                using (var stream = task.StandardOutput.ReadToEndAsync())
                {
                    result = await stream;
                }
            }
            task.WaitForExit();
            return result;
        }

        private static string ScanLogicalDisk()
        {
            string scriptPath = Path.Combine(programPath, "CheckDiskHealth.ps1");
            File.WriteAllText(scriptPath,
                $"Start-Transcript -path {resultsPath} " +
                $"$var=@(Get-Volume | Select -Property DriveLetter, Drivetype | " +
                @"Where-Object { ($_.DriveLetter -ne $Null) -and($_.Drivetype -like ""Fixed"")} | Select -Property DriveLetter)" +
                "foreach ($disk in $var) " +
                "{" +
                "$result = Repair-Volume -DriveLetter ($disk.DriveLetter) -Scan " +
                "($disk.DriveLetter) + '=' + $result " +
                "} " +
                "Stop-Transcript"
            );
            return scriptPath;
        }

        private static async Task ReadResultFileAsync()
        {
            if (File.Exists(resultsPath))
            {
                var result = File.ReadAllText(resultsPath).Split('*').Where(x => !string.IsNullOrEmpty(x)).ToArray()[1].Split('\n').Skip(1);
                if (result.Count() == model.LogicalPartition.Length)
                {
                    try
                    {
                        foreach (var item in result)
                        {
                            var str = item.Split('=');
                            model.LogicalPartition.First(x => x.Name == str[0]).HealthStatus = str[1];
                        }
                        return;
                    }
                    catch { }
                }
            }
            ChangeConfig("NeedTask", "1");
            await FirstRunAsync();
            //await ReadResultFileAsync();
        }
    }
}
