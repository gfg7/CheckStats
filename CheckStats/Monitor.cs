using System;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace CheckStats
{
    internal class Monitor: Hardware
    {
        [ScriptIgnore]
        public MonitorI _Monitor;

        public uint Bandwidth { get; set; }

        public new string SerialNumber
        {
            get
            {
                string res = new string(Encoding.ASCII.GetString(_Monitor.SerialNumberID.SelectMany(BitConverter.GetBytes)
                    .ToArray())
                    .TakeWhile(x => x != '\0')
                    .ToArray());
                return res != "0" ? res : null;
            }
        }

        public string MonitorType { get; set; }

        public string ProductName => _Monitor.InstanceName?.Split('\\')[1];

        [ScriptIgnore]
        public string MonitorManufacturer { get; set; }

        public string Manufacturer
        {
            get
            {
                return new string(Encoding.Unicode.GetString(
                    _Monitor.UserFriendlyName != null ? _Monitor.UserFriendlyName.SelectMany(BitConverter.GetBytes)
                    .ToArray() :
                    MonitorManufacturer != null ? Encoding.ASCII.GetBytes(MonitorManufacturer) :
                    _Monitor.ManufacturerName?.SelectMany(BitConverter.GetBytes).ToArray())
                    .TakeWhile(x => x != '\0')
                    .ToArray());
            }
        }

        public uint PixelsPerXLogicalInch { get; set; }

        public uint PixelsPerYLogicalInch { get; set; }
    }

    internal class MonitorI { 
        public ushort[] SerialNumberID { get; set; }
        public ushort[] UserFriendlyName { get; set; }
        public string InstanceName { get; set; }
        public ushort[] ManufacturerName { get; set; }
    }
}
