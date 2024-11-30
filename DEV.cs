using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class DEV
    {
        public string ip;
        public int id;
        public string controllerName;
        public DEV(DataRow row)
        {
            this.ip =(string) row["netaddr"];
            this.id = (int)row["id_controller"];
            this.controllerName = (string)row["controllerName"];
        }
       
    }
}
