﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    class DEV
    {
        public string ip;
        public int id;
        public string controllerName;
        public bool connect;
        public List<string> commands;
        public DEV(DataRow row)
        {
            this.ip = (string)row["netaddr"];
            this.id = (int)row["id_controller"];
            this.controllerName = (string)row["controllerName"];
        }

    }
}
