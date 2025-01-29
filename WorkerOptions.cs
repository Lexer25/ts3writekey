using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    public class WorkerOptions
    {
        public string db_config {  get; set; }
       // public CardWrite CardWrite { get; set; }
        public CardWriteConfig CardWriteConfig { get; set; }
        //... other properties
    }
    public class CardWriteConfig
    {
        public string SqlGetDevice {  get; set; }
        public string stopList { get; set; }
        public string timestart { get; set; }
        public string timeout { get; set; }
        public bool run_now { get; set; }
        public string upconf { get; set; }
    }
}
