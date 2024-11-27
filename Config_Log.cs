using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Config_Log
    {
        public string db_config { get; set; }
        public string selct_card { get; set; } //
        public Config_Log() 
        {
            this.db_config = db_config;
            this.selct_card = selct_card;

        
        }
        public static void log(string log)
        {
            Console.WriteLine(log);
            File.AppendAllText(@"log.txt", log + Environment.NewLine);
        }
    }
}
