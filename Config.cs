using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Config
    {
        public string db_config { get; set; }
        public string selct_card { get; set; }

        public string stopList { get; set; }

        //public bool log_console { get; set; }
        public Config() 
        {
            this.db_config = db_config;
            this.selct_card = selct_card;
            this.stopList = stopList;
            //this.log_console = log_console;

        }

    }
}
