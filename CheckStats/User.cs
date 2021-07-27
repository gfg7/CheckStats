using System;
using System.Runtime.Serialization;

namespace CheckStats
{
    internal partial class Program
    {
        private class User 
        {
            public int AccountType { get; set; }

            public bool Disabled { get; set; }

            public string Domain { get; set; }

            public string FullName { get; set; }

            public bool LocalAccount { get; set; }

            public bool Lockout { get; set; }

            public string Name { get; set; }

            public bool PasswordChangeable { get; set; }

            public bool PasswordExpires { get; set; }

            public bool PasswordRequired { get; set; }

            public string SID { get; set; }

            public int SIDType { get; set; }

            public string Status { get; set; }
        }
    }
}
