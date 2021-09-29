namespace CheckStats
{

    internal class DiskPartition
    {
        public string DeviceID { get; set; }
        public uint Index { get; set; }
        public uint DiskIndex { get; set; }
        public string Type { get; set; }
        public ulong Size { get; set; }
        public ulong FreeSpace { get; set; }
    }
}
