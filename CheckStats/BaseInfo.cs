namespace CheckStats
{

    internal abstract class DeviceBaseInfo
    {
        public int Availability { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Status { get; set; }
        public int LastErrorCode { get; set; }
        public int ConfigManagerErrorCode { get; set; }
    }

    internal abstract class HardwareBaseInfo : DeviceBaseInfo
    {
        public string SerialNumber { get; set; }
    }
}
