namespace CheckStats
{
    internal partial class Program
    {
        private class User
        {
            public bool Disabled { get; set; }
            public string Domain { get; set; }
            public string FullName { get; set; }
            public bool LocalAccount { get; set; }
            public string Name { get; set; }
            public bool PasswordChangeable { get; set; }
            public bool PasswordRequired { get; set; }
        }
    }
}
