using System;
using System.IO;
using System.Text;

namespace Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //var path = GetArgumentValue(args, "path"); // TODO: szóköz?
            var path = @"C:\Users\Balazs\Downloads\Díjnet számlák\Automatikusan letöltött számlák";

            if (path == null)
            {
                Console.WriteLine("Könyvtár:");
                path = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    Console.WriteLine("Hibás paraméter");
                    return;
                }

            }

            var processor = new Processor();
            processor.Process(path);
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
