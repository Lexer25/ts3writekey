using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService1
{
    public class COM
    {
        dynamic a;

        public bool isTest=false;
        
        public COM()
        {
            a = Activator.CreateInstance(Type.GetTypeFromCLSID(Guid.Parse("EAE30322-9FA6-4466-B3AE-DFB1D58813D3")));//load driver by guid
        }
        public dynamic Getcom()
        {
            return a;
        }
        public void SetupString(string str)
        {
            a.SetupString = str;
        }
       
        /*
        private List<string> commands = new List<string>();
        public List<string> ComandsExclude()
        {
            List<string> lists = new List<string>();
            foreach (string comand in commands) lists.Add(a.Execute(comand));
            return lists;
        }

        */
        public string ComandExclude(string comand)
        {
          
                //commands.Add(comand);//30.01.2025 вот это непонятно зачем. Надо проверить и убрать, если не нужен.
                return a.Execute(comand);
            
        }

        public string ComandExecute(string comand)
        {
            if (isTest)
            {
                return "Ok";
            }
            else
            {
                //commands.Add(comand);//30.01.2025 вот это непонятно зачем. Надо проверить и убрать, если не нужен.
                return a.Execute(comand);

            }
        }


            public bool ReportStatus()
        {
            if (isTest)
            {
                return true;
            }
            else
            {
                return ((string)a.Execute($@"ReportStatus")).Split('=')[1] == "Yes";
            }
        }
    }
}