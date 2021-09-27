using System.Web.Script.Serialization;

namespace CheckStats
{
    internal class RAM : Hardware
    {
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public ulong Capacity { get; set; }
        //public float? Load { get; set; }
        //public float? Used { get; set; }
        //public float? Available { get; set; }
        [ScriptIgnore]
        public int MemoryType { get; set; }
        public string Type
        {
            get
            {
                switch (MemoryType)
                {
                    case 0: return "Unknown";
                    case 1: return "Other";
                    case 2: return "DRAM";
                    case 3: return "Synchronous DRAM";
                    case 4: return "Cache DRAM";
                    case 5: return "EDO";
                    case 6: return "EDRAM";
                    case 7: return "VRAM";
                    case 8: return "SRAM";
                    case 9: return "RAM";
                    case 10: return "ROM";
                    case 11: return "Flash";
                    case 12: return "EEPROM";
                    case 13: return "FEPROM";
                    case 14: return "EPROM";
                    case 15: return "CDRAM";
                    case 16: return "3DRAM";
                    case 17: return "SDRAM";
                    case 18: return "SGRAM";
                    case 19: return "RDRAM";
                    case 20: return "DDR";
                    case 21: return "DDR2";
                    case 22: return "DDR2 FB-DIMM";
                    case 24: return "DDR3";
                    case 25: return "FBD2";
                    case 26: return "DDR4";
                    default: return "Undefined";
                }
            }
        }
    }
}
