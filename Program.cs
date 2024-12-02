﻿
using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text.Json;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

partial class Program
{
    static void Main(string[] args)
    {
        Config_Log config_log = JsonSerializer.Deserialize<Config_Log>(File.ReadAllText("conf.json"));
        if (!config_log.log_console) Console.WriteLine("log false in conf.json");
        Config_Log.log($@"Старт программы TS3", config_log);
        if (!File.Exists("conf.json"))
        File.AppendAllText("conf.json",@$"{{
  ""db_config"": ""User = SYSDBA; Password = temp; Database =  C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB; DataSource = 127.0.0.1; Port = 3050; Dialect = 3; Charset = win1251; Role =;Connection lifetime = 15; Pooling = true; MinPoolSize = 0; MaxPoolSize = 50; Packet Size = 8192; ServerType = 0;"",
  ""selct_card"": ""cardindev_getlist(1)""}}");

        ConsoleApp1.Config_Log.log($@"Подключение к базе данных {config_log.db_config}", config_log);

        FbConnection con = DB.Connect(config_log.db_config);
        try
        {
            con.Open();
            Config_Log.log($@"Подключение к базе данных выполнено успешно.", config_log);
        }
        catch {
            Config_Log.log("Не могу подключиться к базе данных "+ config_log.db_config+". Программа завершает работу.", config_log);
            return; 
        }


        List<DEV> devs = new List<DEV>();
        DataTable table = DB.GetDevice(con, config_log.selct_card);
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            if (row["netaddr"].ToString() != "")
                devs.Add(new DEV(row));
        }
       
        if(devs.Count == 0)
        {
            string mess="Нет данных для загрузки/удаления идентификаторов из контроллеров.";
            Config_Log.log(mess, config_log);
           // Console.WriteLine(mess);

            mess="Программа TS3 завершает работу: нет данных для работы.";
            Config_Log.log(mess, config_log);
            //Console.WriteLine(mess);
            return;
        }

        Config_Log.log("Имеются данных для загрузки/удаления идентификаторов в " + devs.Count+ " контроллеров.", config_log);
        con.Close();

        List<Thread> threads = new List<Thread>();
        Console.WriteLine("threads");

        foreach (DEV dev in devs)
        {
            Thread thread = new Thread(() => OneDev(config_log, dev, config_log));
            threads.Add(thread);
            Console.WriteLine("thread_add");
            thread.Start();
            Console.WriteLine("thread_start");
        }
        Console.WriteLine("thread_join_s");
        foreach (Thread thread in threads)
        {
            thread.Join();
            Console.WriteLine("thread_join");
        }

        Config_Log.log($@"Стоп программы TS3", config_log);
    }
    private static void OneDev(Config_Log log_config,DEV dev,Config_Log config_log) 
    {
        FbConnection con = DB.Connect(log_config.db_config);
        con.Open();
        // Console.WriteLine(dev.id.ToString());

        //беру список карт для точек прохода указанного контроллера
        DataTable table = DB.GetDor(con, dev.id, log_config.selct_card);

        // сделал экземпляр контроллера
        Comand com = new Comand();
        com.SetupString(dev.ip);

        Config_Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + com.ComandExclude($@"ReportStatus") + " | count " + table.Rows.Count, config_log);
        if (com.ReportStatus())
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                CardinDev(con, dev, table.Rows[i], com, log_config);
            }

        }
        else
        {


            for (int i = 0; i < table.Rows.Count; i++)
            {
                DB.UpdateIdxCard(con, table.Rows[i], "no connect",false);
                DB.UpdateCardInDevIncrement(con, table.Rows[i]);
            }

        }
        con.Close();

    }
    private static void CardinDev(FbConnection con, DEV dev, DataRow row,Comand com,Config_Log config_log)
    {
        //Проверка связи 
       

        //com.SetupString("192.168.8.18");

            string comand = ComandParser(row, con, com);
            string log = $@"{dev.id}  | {row["id_door"]} | {dev.ip} | {comand}";
            Config_Log.log(log, config_log);
       
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
                    DB.UpdateIdxCard(con, row, anser,true);//заполнить load_result, load_time, id_card_in_dev=null
                    DB.DeleteCardInDev(con, row);//удалить строку
                }
                else
                {
                    DB.UpdateIdxCard(con, row, anser,false);
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