using System;
using System.Threading;

namespace dijnet
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new DijnetClient();
            client.DownloadAll();
        }
    }
}
