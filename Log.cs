using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ConsoleApp1
{
    public static class Log
    {
        public static void log(string log)
        {
            string date = DateTime.UtcNow.ToString("yyyy - MM - dd HH - mm - ss");
            string timeandlog = $@"{date} {log}" + Environment.NewLine;
            Console.WriteLine(log);
            using (var stream = File.Open("log.txt", FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                byte[] input = Encoding.Default.GetBytes(timeandlog);
                stream.Write(input, 0, input.Length);
            }
        }
    }
}
