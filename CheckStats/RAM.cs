using System.Web.Script.Serialization;

namespace CheckStats
{
    internal class RAM 
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public ulong Capacity { get; set; }
        [ScriptIgnore]
        public int MemoryType { get; set; }
        public string RAMType
        {
            get
            {
                string outValue;
                switch (MemoryType)
                {
                    case 0: outValue = "Unknown"; break;
                    case 1: outValue = "Other"; break;
                    case 2: outValue = "DRAM"; break;
                    case 3: outValue = "Synchronous DRAM"; break;
                    case 4: outValue = "Cache DRAM"; break;
                    case 5: outValue = "EDO"; break;
                    case 6: outValue = "EDRAM"; break;
                    case 7: outValue = "VRAM"; break;
                    case 8: outValue = "SRAM"; break;
                    case 9: outValue = "RAM"; break;
                    case 10: outValue = "ROM"; break;
                    case 11: outValue = "Flash"; break;
                    case 12: outValue = "EEPROM"; break;
                    case 13: outValue = "FEPROM"; break;
                    case 14: outValue = "EPROM"; break;
                    case 15: outValue = "CDRAM"; break;
                    case 16: outValue = "3DRAM"; break;
                    case 17: outValue = "SDRAM"; break;
                    case 18: outValue = "SGRAM"; break;
                    case 19: outValue = "RDRAM"; break;
                    case 20: outValue = "DDR"; break;
                    case 21: outValue = "DDR2"; break;
                    case 22: outValue = "DDR2 FB-DIMM"; break;
                    case 24: outValue = "DDR3"; break;
                    case 25: outValue = "FBD2"; break;
                    case 26: outValue = "DDR4"; break;
                    default: outValue = "Undefined"; break;
                }
                return outValue;
            }
        }
        public string SerialNumber { get; set; }
    }
}
