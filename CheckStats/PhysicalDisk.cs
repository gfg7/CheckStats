namespace CheckStats
{
    internal class PhysicalDisk : Hardware
    {
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public uint Index { get; set; }
        public string FirmwareRevision { get; set; }
        public string InterfaceType { get; set; }
        public ulong Size { get; set; }
        public float? Temperature { get; set; }
    }
}
