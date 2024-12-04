using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public static class Log
    {
        private static readonly object locker = new object();
        public static void log(string log)
        {
            lock (locker)
            {
                Console.WriteLine(log);
                StreamWriter SW = File.AppendText(@"log.txt");
                SW.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + log + Environment.NewLine);
                SW.Close();
            }
        }
    }
}
