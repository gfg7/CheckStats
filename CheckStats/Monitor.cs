namespace CheckStats
{
    internal class Monitor : DeviceBaseInfo
    {
        public uint Bandwidth { get; set; }

        public ushort DisplayType { get; set; }

        public bool IsLocked { get; set; }

        public string MonitorManufacturer { get; set; }

        public string MonitorType { get; set; }

        public uint PixelsPerXLogicalInch { get; set; }

        public uint PixelsPerYLogicalInch { get; set; }
    }
}
