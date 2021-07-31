namespace CheckStats
{
    internal class Disk 
    {
        public uint BytesPerSector { get; set; }
        public string CompressionMethod { get; set; }
        public string FirmwareRevision { get; set; }
        public string InterfaceType { get; set; }
        public bool MediaLoaded { get; set; }
        public bool NeedsCleaning { get; set; }
        public uint Partitions { get; set; }
        public uint SCSIBus { get; set; }
        public ushort SCSILogicalUnit { get; set; }
        public ushort SCSIPort { get; set; }
        public ushort SCSITargetId { get; set; }
        public ulong Size { get; set; }
        public ulong TotalSectors { get; set; }
    }
}
