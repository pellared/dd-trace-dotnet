using System;

namespace GetMsiDetails
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("Usage: Get-MsiDetails.exe <path_to_msi_package>");
                return;
            }

            string[] propertyNames =
            {
                "ProductName",
                "ProductCode",
                "ProductVersion"
            };

            MsiPackage msi = null;

            try
            {
                msi = new MsiPackage(args[0]);

                foreach (string propertyName in propertyNames)
                {
                    string propertyValue = msi.GetPackageProperty(propertyName);
                    Console.WriteLine($"{propertyName} = {propertyValue}");
                }
            }
            finally
            {
                msi?.Dispose();
            }
        }
    }
}
