using System;

namespace CheckStats
{
    internal class VideoAdapter:HardwareBaseInfo
    {
        public string AdapterCompatibility{get;set;}
        public string AdapterDACType{get;set;}
        public uint AdapterRAM {get;set;}
        public bool ConfigManagerUserConfig{get;set;}
        public uint CurrentHorizontalResolution {get;set;}
        public uint CurrentRefreshRate {get;set;}
        public uint CurrentVerticalResolution {get;set;}
        public DateTime DriverDate{get;set;}
        public string DriverVersion{get;set;}
        public ushort VideoArchitecture {get;set;}
        public ushort VideoMemoryType {get;set;}
    }
}
