namespace CheckStats
{
    internal partial class Program
    {
        static class WMIClasses
        {
            public const string DesktopMonitor = "Win32_DesktopMonitor";
            public const string UserAccount = "Win32_UserAccount";
            public const string PhysicalMemory = "Win32_PhysicalMemory";
            public const string Processor = "Win32_Processor";
            public const string DiskDrive = "Win32_DiskDrive";
            public const string MotherboardDevice = "Win32_MotherboardDevice";
            public const string Motherboard= "Win32_BaseBoard";
            public const string VideoController = "Win32_VideoController";

            //public const string SMARTCheck = "MSStorageDriver_FailurePredictData";
        }

        
    }
}
