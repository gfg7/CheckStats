﻿using System.Collections.Generic;


namespace CheckStats
{
    internal abstract class Base
    {
        public string Status { get; set; }
    }

    internal abstract class Hardware: Base
    {
        public string SerialNumber { get; set; }
    }

    internal partial class Program
    {
        private class BaseModel
        {
            public string OS { get; set; }
            public string DesktopName { get; set; }
            public string Domain { get; set; }
            public string IPAddress { get; set; }
            public string MACAddress { get; set; }
            public CPU Processor { get; set; }
            public Motherboard Motherboard { get; set; }
            public RAM[] RAM { get; set; }
            public VideoAdapter[] VideoAdapter { get; set; }
            public Monitor[] Monitor { get; set; }
            public LogicalDisk[] LogicalPartition { get; set; }
            //public DiskPartition[] Partitions { get; set; }
            public PhysicalDisk[] Disk { get; set; }
            private Dictionary<string, double> Temperatures;
            public User[] User { get; set; }
        }
    }
}
