using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class Comand
    {
        dynamic a;
        public Comand()
        {
            a = Activator.CreateInstance(Type.GetTypeFromCLSID(Guid.Parse("EAE30322-9FA6-4466-B3AE-DFB1D58813D3")));//load driver by guid
        }
        public dynamic Getcom()
        {
            return a;
        }
        public void SetupString(string str)
        {
            a.SetupString=str; 
        }
        private List<string> commands = new List<string>();
        public List<string> ComandsExclude()
        {
            List<string> lists = new List<string>();
            foreach (string comand in commands)  lists.Add(a.Execute(comand));
            return lists;
        }
        public string ComandExclude(string comand)
        {
            commands.Add(comand);
            return a.Execute(comand);
        }
        public bool ReportStatus()
        {
            return ((string)a.Execute($@"ReportStatus")).Split('=')[1] == "Yes";
        }
    }
}
