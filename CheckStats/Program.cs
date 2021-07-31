using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;

namespace CheckStats
{
    internal partial class Program
    {
        private static InfoModel model = new InfoModel();

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
            //Timer timer = new Timer()
            //{
            //    AutoReset = true,
            //    Enabled = true,
            //    Interval = TimeSpan.FromSeconds(3).TotalMilliseconds
            //};
            //timer.Elapsed += new ElapsedEventHandler(UpdateInfo);
            UpdateInfo(null, null);
            Console.Read();
        }

        private static void UpdateInfo(object source, ElapsedEventArgs e)
        {
            #region Set properties
            model.Domain = Environment.UserDomainName;
            model.DesktopName = Environment.MachineName;
            model.OS = Environment.OSVersion.VersionString;
            using (var searcher = new ManagementObjectSearcher($"select * from Win32_NetworkAdapterConfiguration where IPEnabled=true"))
            {
                foreach (var item in searcher.Get())
                {
                    model.MACAddress = item.Properties["MACAddress"].Value.ToString();
                    model.IPAddress = item.Properties["IPAddress"].Value.ToString();
                }
            }
            //model.Users = GetModelsArray<User>(WMIClasses.UserAccount);
            var m1 = GetModelsArray<Motherboard>(WMIClasses.Motherboard);
            var m2 = GetModelsArray<Motherboard>(WMIClasses.MotherboardDevice);
            for (int i = 0; i < m1.Length; i++)
            {
                typeof(Motherboard).GetProperties().Where(x => x.GetValue(m1[i]) == null).ToList().ForEach(x =>
                {
                    x.SetValue(m1[i], typeof(Motherboard).GetProperty(x.Name).GetValue(m2[i]));
                });
            }
            model.Motherboards = m1;
            model.Processor = GetModelsArray<CPU>(WMIClasses.Processor).First();
            model.VideoAdapters = GetModelsArray<VideoAdapter>(WMIClasses.VideoController);
            model.RAMs = GetModelsArray<RAM>(WMIClasses.PhysicalMemory);
            model.Monitors = GetModelsArray<Monitor>(WMIClasses.DesktopMonitor);
            //model.Disks = GetModelsArray<Disk>(WMIClasses.DiskDrive);
            #endregion
            string json = new JavaScriptSerializer().Serialize(model);
            Console.WriteLine(json);
            model = new InfoModel();
        }

        private static T[] GetModelsArray<T>(string className) where T : class, new()
        {
            T[] array = null;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + className))
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
                                    modelProperty.SetValue(model, value);
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
