using System.Collections.Generic;


namespace CheckStats
{
    internal partial class Program
    {
        private class InfoModel
        {
            public string OS { get; set; }
            public string DesktopName { get; set; }
            public string Domain { get; set; }
            public string IPAddress { get; set; }
            public string MACAddress { get; set; }

            public CPU Processor { get; set; }
            public Motherboard[] Motherboards { get; set; }
            public RAM[] RAMs { get; set; }
            public VideoAdapter[] VideoAdapters { get; set; }
            public Monitor[] Monitors { get; set; }
            public Disk[] Disks { get; set; }
            private Dictionary<string, double> Temperatures;
            public User[] Users { get; set; }
            private string HDDBrokenSectors;
        }
    }
}
