using System;

namespace CheckStats
{
    internal class VideoAdapter
    {
        public string Name { get;set;}
        public string AdapterDACType{get;set;}
        public string Status { get;set;}
        public uint AdapterRAM {get;set;}
        public uint CurrentHorizontalResolution {get;set;}
        public uint CurrentRefreshRate {get;set;}
        public uint CurrentVerticalResolution {get;set;}
        public string DriverVersion{get;set;}
    }
}
