using System.Web.Script.Serialization;

namespace CheckStats
{
    internal class CPU
    {
        public string Name { get; set; }
        public string ProcessorId { get; set; }
        [ScriptIgnore]
        public uint Architecture { get; set; }
        public string ArchitectureType 
        {
            get {
                string outValue=null;
            switch (Architecture)
                {
                    case 0: outValue = "x86 "; break;
                    case 1: outValue = "MIPS"; break;
                    case 2: outValue = "Alpha"; break;
                    case 3: outValue = "PowerPC"; break;
                    case 5: outValue = "ARM"; break;
                    case 6: outValue = "ia64"; break;
                    case 9: outValue = "x64"; break;
                }
                return outValue;
            } 
        }
        public uint NumberOfLogicalProcessors { get; set; }
    }
}
