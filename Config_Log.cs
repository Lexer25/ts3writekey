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
        public bool log_console { get; set; }
        public Config_Log() 
        {
            this.db_config = db_config;
            this.selct_card = selct_card;
            this.log_console = log_console;
        
        }
        public static void log(string log, Config_Log cl)
        {
            if(cl.log_console)
            Console.WriteLine(log);
            File.AppendAllText(@"log.txt", DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss") +" "+ log + Environment.NewLine);
        }
    }
}
