﻿
using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text.Json;

partial class Program
{
    static void Main(string[] args)
    {
        if (!File.Exists("conf.json"))
        File.AppendAllText("conf.json",@$"{{
  ""db_config"": ""User = SYSDBA; Password = temp; Database =  C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB; DataSource = 127.0.0.1; Port = 3050; Dialect = 3; Charset = win1251; Role =;Connection lifetime = 15; Pooling = true; MinPoolSize = 0; MaxPoolSize = 50; Packet Size = 8192; ServerType = 0;"",
  ""selct_card"": ""cardindev_getlist(1)""
}}
");
        Config_Log log = JsonSerializer.Deserialize<Config_Log>(File.ReadAllText("conf.json"));
        FbConnection con = DB.Connect(log.db_config);
        try
        {
            con.Open();
        }
        catch {
            Config_Log.log($@"Не могу подключиться к базе данных {log.db_config}");
            return; 
        }   
  
        List<DEV> devs = new List<DEV>();
        DataTable table = DB.GetDevice(con, log.selct_card);
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            if (row["netaddr"].ToString() != "")
                devs.Add(new DEV(row));
        }
       
        foreach (DEV dev in devs)
        {
           // Console.WriteLine(dev.id.ToString());
            
            // сделал экземпляр контроллера
            Comand com = new Comand();
            com.SetupString(dev.ip);
            
            Config_Log.log(dev.id+" | " + dev.ip + " | " + com.ComandExclude($@"ReportStatus"));

            //беру список карт для точек прохода указанного контроллера
            table = DB.GetDor(con, dev.id);
          

            if (com.ReportStatus())
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    CardinDev(con, dev, table.Rows[i],com);
                }

            } else
            {

               
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    DB.UpdateIdxCard(con, table.Rows[i], "no connect");
                    DB.UpdateCardInDevIncrement(con, table.Rows[i]);
                }

            }
         //   List<Thread> threads = new List<Thread>();
          /*  Thread thread = new Thread(() => );
            threads.Add(thread);
            thread.Start();
            foreach (Thread thread in threads) thread.Join*/
        }
        con.Close();
    }
    private static void CardinDev(FbConnection con, DEV dev, DataRow row,Comand com)
    {
        //Проверка связи 
       

        //com.SetupString("192.168.8.18");

            string comand = ComandParser(row, con, com);
            string log = $@"{dev.id}  | {row["id_door"]} | {dev.ip} | {comand}";
            Config_Log.log(log);
       
    }
    private static string ComandParser(DataRow? row, FbConnection con, Comand comand)
    {
        string anser = "", command = "";
        switch ((int)row["operation"])
        {
            case 1:
                command = $@"writekey door={row["id_reader"]}, key=""{row["id_card"]}"", TZ={row["timezones"]}, status={0}";
                anser = comand.ComandExclude(command);
                if (anser.Contains("OK"))
                {
                    DB.UpdateIdxCard(con, row, anser);//заполнить load_result, load_time, id_card_in_dev=null
                    DB.DeleteCardInDev(con, row);//удалить строку
                }
                else
                {
                    DB.UpdateIdxCard(con, row, anser);
                    DB.UpdateCardInDevIncrement(con, row);//atent+1
                }
                break;
            case 2:
                command = $@"deletekey door={0}, key=""{row["id_card"]}""";
                anser = comand.ComandExclude(command);
                DB.DeleteCardInDev(con, row);
                break;
        }
        return command + ">" + anser;

    }
}








/*using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
List<DEV> ips = new List<DEV>() {
"192.168.8.18",
};
string DBuser= "SYSDBA", DBpassword= "temp", DBpath= " C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB", 
DBDataSource= "127.0.0.1", DBCharset= "win1251", DBRoule="";
int DBport= 3050, DBDialect=3, DBConnectionlifetime=15, DBMinPoolSize=0, DBMaxPoolSize=50, DBPacket_Size=8192, DBServerType=0;
bool DBPooling=true;
string connectionString = $@"User = {DBuser}; Password = {DBpassword}; Database = {DBpath}; DataSource = {DBDataSource}; Port = {DBport}; 
            Dialect = {DBDialect}; Charset = {DBCharset}; Role ={DBRoule};Connection lifetime = {DBConnectionlifetime}; Pooling = {DBPooling};
            MinPoolSize = {DBMinPoolSize}; MaxPoolSize = {DBMaxPoolSize}; Packet Size = {DBPacket_Size}; ServerType = {DBServerType};";
FbConnection con = new FbConnection(connectionString);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
con.Open();
FbCommand getip = new FbCommand("select d.id_dev, d.name, d.netaddr, d.config from Device d where d.id_reader is Null", con);
FbCommand getcomand = new FbCommand("select * from cardindev_getlist(1) order by id_cardindev", con);
var reader= getip.ExecuteReader();
DataTable table= new DataTable();
table.Load(reader);
for (int i = 0; i < table.Rows.Count; i++)
{
    var row = table.Rows[i];
    if (row["netaddr"].ToString()!="")
    ips.Add(new DEV);
}
reader = getcomand.ExecuteReader();
table = new DataTable();
table.Load(reader);
for (int i = 0; i < table.Rows.Count; i++)
{
    var row = table.Rows[i];
    Console.WriteLine(row["id_dev"]+" | "+row["operation"]);
}
con.Close();
dynamic a = Activator.CreateInstance(Type.GetTypeFromCLSID(Guid.Parse("EAE30322-9FA6-4466-B3AE-DFB1D58813D3")));//load driver by guid

foreach (object ip in ips)
{
    Console.WriteLine(ip);
    a.SetupString = ip;
    string status = a.Execute($@"ReportStatus");
    Console.WriteLine(status);

    //Console.WriteLine(a.Execute($@"getkeyCount door= 0"));
    //Console.WriteLine(a.Execute($@"GetDeviceTime"));
    string log = ip +" | "+ status;
    File.AppendAllText(@"log.txt", log + Environment.NewLine);
}*/