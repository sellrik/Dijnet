using System;
using System.Threading;

namespace dijnet
{
    class Program
    {
        static void Main(string[] args)
        {
            string userName, password;

            userName = GetArgumentValue(args, "userName");
            password = GetArgumentValue(args, "password");

            if (userName == null)
            {
                Console.WriteLine("Felhasználónév:");
                userName = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(userName))
                {
                    Console.WriteLine("Hibás paraméter");
                    return;
                }
            }

            if (password == null)
            {
                Console.WriteLine("Jelszó:");
                password = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(password))
                {
                    return;
                }
            }

            var client = new DijnetClient(userName, password);
            client.DownloadAll();
        }

        private static string GetArgumentValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLower();
                var startString = $"{name.ToLower()}=";
                if (arg.StartsWith(startString))
                {
                    var value = arg.Replace(startString, "").Trim();
                    return value;
                }
            }

            return null;
        }
    }
}
