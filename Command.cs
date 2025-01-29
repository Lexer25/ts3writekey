using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    internal class Command
    {
        public Command(DataRow dataRow, string command)
        {
            this.dataRow = dataRow;
            this.command = command;
        }
        public DataRow dataRow;
        public string command;
    }
}
