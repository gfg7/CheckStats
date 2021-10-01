using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace CheckStats
{
    internal partial class Program
    {
        private static readonly string path = Path.Combine
            (Path.GetDirectoryName
            (Process.GetCurrentProcess().MainModule.FileName),
            "script.ps1");
        private static readonly WebClient client = new WebClient();

        private static Dictionary<string, object> model;
        private static Dictionary<string, object> GPU;
        private static Dictionary<string, object> Monitor;
        private static Dictionary<string, object> RAM;
        private static Dictionary<string, object> Motherboard;
        private static Dictionary<string, object> CPU;
        private static Dictionary<string, object> Users;
        private static Dictionary<string, object> Disk;
        private static Dictionary<string, object> DiskVolume;
        private static Dictionary<string, object> Net;
        private static Dictionary<string, object> System;

        private static void Main()
        {
            model = new Dictionary<string, object>
            {
                { nameof(GPU), GPU },
                { nameof(Monitor), Monitor },
                { nameof(RAM), RAM },
                { nameof(Motherboard), Motherboard },
                { nameof(CPU), CPU },
                { nameof(Users), Users },
                { nameof(Disk), Disk },
                { nameof(DiskVolume), DiskVolume },
                { nameof(Net), Net },
                { nameof(System), System }
            };
            #region Timer
            Timer timer = new Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
            };
            timer.Elapsed += new ElapsedEventHandler(SendInfoAsync);
            #endregion
            Console.Read();
        }

        private static async void GetStats()
        {
            GPU = new Dictionary<string, object>();
            Monitor = new Dictionary<string, object>();
            RAM = new Dictionary<string, object>();
            Motherboard = new Dictionary<string, object>();
            CPU = new Dictionary<string, object>();
            Users = new Dictionary<string, object>();
            Disk = new Dictionary<string, object>();
            DiskVolume = new Dictionary<string, object>();
            Net = new Dictionary<string, object>();
            System = new Dictionary<string, object>();
            var info = string.Join("",(await StartPowershell(path, true))
                .Split('*')
                .SkipWhile(x => string.IsNullOrEmpty(x)))
                .Split('#')
                .SkipWhile(x=>string.IsNullOrEmpty(x));
            info
                .ToList()
                .Skip(1)
                .SkipWhile(x=> string.IsNullOrEmpty(x))
                .ToList()
                .ForEach(x=> Task.Run(() => SetInfo(x.Split())));
        }

        private static void SetInfo(string[] info) => model[info[0]] = info.Skip(1)
            .ToDictionary(x => x.Split(':')[0].Trim(), y => (object)y.Split(':')[1].Trim());

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
            if (redirect)
            {
                return await task.StandardOutput.ReadToEndAsync();
            }
            return null;
        }



        private static async void SendInfoAsync(object source, ElapsedEventArgs e)
        {
            GetStats();
            .string json = "";
            try
            {
                client.UploadStringAsync(new Uri(ConfigurationManager.AppSettings.Get("Server").First().ToString()), json);
            }
            catch { }
            model.Values.ToList().ForEach(x => x = null);
            GC.Collect();
            Console.WriteLine(json);
        }

        private static async Task FirstRunAsync()
        {
            if (ConfigurationManager.AppSettings.Get("NeedTask").First() == '1')
            {
                string path = Path.Combine("", "SendStats.ps1");
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
                    $Action = New-ScheduledTaskAction -Execute ""{""}""
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


        private static string ScanLogicalDisk()
        {
            string scriptPath = Path.Combine("", "CheckDiskHealth.ps1");
            File.WriteAllText(scriptPath,
                $"Start-Transcript -path {""} " +
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
    }
}
