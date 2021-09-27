using System.Web.Script.Serialization;

namespace CheckStats
{
    internal class CPU:Base
    {
        public string Name { get; set; }
        public string ProcessorId { get; set; }
        [ScriptIgnore]
        public uint Architecture { get; set; }
        public string Type
        {
            get
            {
                switch (Architecture)
                {
                    case 0: return "x86 ";
                    case 1: return "MIPS";
                    case 2: return "Alpha";
                    case 3: return "PowerPC";
                    case 5: return "ARM";
                    case 6: return "ia64";
                    case 9: return "x64";
                    default: return null;
                }
            }
        }
        public uint NumberOfLogicalProcessors { get; set; }
        public float?[] Temperature{ get; set; }
        public float? Load { get; set; }
    }
}
