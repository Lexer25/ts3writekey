﻿
using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

partial class Program
{
    static List<DEV> devs = new List<DEV>();
    static void Main(string[] args)
    {
        Config_Log config_log = JsonSerializer.Deserialize<Config_Log>(File.ReadAllText("conf.json"));
        if (!config_log.log_console) Console.WriteLine("log false in conf.json");
        Config_Log.log($@"Старт программы TS3");
        if (!File.Exists("conf.json"))
        File.AppendAllText("conf.json",@$"{{
  ""db_config"": ""User = SYSDBA; Password = temp; Database =  C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB; DataSource = 127.0.0.1; Port = 3050; Dialect = 3; Charset = win1251; Role =;Connection lifetime = 15; Pooling = true; MinPoolSize = 0; MaxPoolSize = 50; Packet Size = 8192; ServerType = 0;"",
  ""selct_card"": ""cardindev_getlist(1)""}}");

        ConsoleApp1.Config_Log.log($@"Подключение к базе данных {config_log.db_config}");

        FbConnection con = DB.Connect(config_log.db_config);
        try
        {
            con.Open();
            Config_Log.log($@"Подключение к базе данных выполнено успешно.");
        }
        catch {
            Config_Log.log("Не могу подключиться к базе данных "+ config_log.db_config+". Программа завершает работу.");
            return; 
        }
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
            Config_Log.log(mess);
           // Console.WriteLine(mess);

            mess="Программа TS3 завершает работу: нет данных для работы.";
            Config_Log.log(mess);
            //Console.WriteLine(mess);
            return;
        }

        Config_Log.log("Имеются данных для загрузки/удаления идентификаторов в " + devs.Count+ " контроллеров.");
        con.Close();

        //готовим список контроллеров, которые на связи
        //и формируем список указателей на потоки.
        List<Thread> threads = new List<Thread>();
        for (int i = 0;i < devs.Count-1;i++) 
        {
            Thread thread = new Thread(() => GetDev(i));
            threads.Add(thread);
            thread.Start();
        }
        
        
        //ждем завершение всех потоков.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Config_Log.log($@"thread_end");


        foreach (DEV dev in devs)
        {
            OneDev(config_log, dev);
            /*Thread thread = new Thread(() => OneDev(config_log, dev,config_log));
            threads.Add(thread);
            Console.WriteLine("thread_add");
            thread.Start();
            Console.WriteLine("thread_start");*/
        }

        Config_Log.log($@"Стоп программы TS3");
    }
    public static void GetDev(int i)
    {
        COM com = new COM();
        com.SetupString(devs[i].ip);
        devs[i].connect = com.ReportStatus();
    }
    
    //
    private static void OneDev(Config_Log log_config,DEV dev) 
    {
        FbConnection con = DB.Connect(log_config.db_config);
        con.Open();

        //беру список карт для точек прохода указанного контроллера
        DateTime start = DateTime.Now;
        DataTable table = DB.GetDor(con, dev.id, log_config.selct_card);
        Console.WriteLine(@$"sql GetDor_{DateTime.Now-start}");
        start = DateTime.Now;
        // сделал экземпляр контроллера

        Config_Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect + " | count " + table.Rows.Count);
        if (dev.connect)
        {
            COM com = new COM();
            com.SetupString(dev.ip);
            List<Command> cmds = new List<Command>();
            foreach (DataRow row in table.Rows)
            {

                //Console.WriteLine(i);
                string comand = ComandBuilder(row, con, com);
                string log = $@"{dev.id}  | {row["id_door"]} | {dev.ip} | {comand} > start";
                cmds.Add(new Command(row, comand));
                Config_Log.log(log);
                //надо доабавить в лог ответ
            }
            Thread thread = new Thread(() =>
            {
                foreach (Command cmd in cmds)
                {
                    string anser = com.ComandExclude(cmd.command);
                    AfterComand(anser,con, cmd.dataRow);
                    string log = $@"{dev.id}  | {cmd.dataRow["id_door"]} | {dev.ip} | {cmd.command} > {anser}";
                    Config_Log.log(log);
                }
                con.Close();
            });
            thread.Start();
            Console.WriteLine(@$"sql_con_{DateTime.Now - start}");
        }
        else
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                DB.UpdateIdxCard(con, table.Rows[i], "no connect",false);
                DB.UpdateCardInDevIncrement(con, table.Rows[i]);
            }
            Console.WriteLine(@$"sql_no_con_{DateTime.Now - start}");
            con.Close();
        }
    }
    private static string ComandBuilder(DataRow? row, FbConnection con, COM comand)
    {
        string anser = "", command = "";
        switch ((int)row["operation"])
        {
            case 1:
                command = $@"writekey door={row["id_reader"]}, key=""{row["id_card"]}"", TZ={row["timezones"]}, status={0}";
                break;
            case 2:
                command = $@"deletekey door={0}, key=""{row["id_card"]}""";
                break;
        }
        return command;
    }
    private static void AfterComand(string anser, FbConnection con, DataRow? row)
    {
        switch ((int)row["operation"])
        {
            case 1:
                    if (anser.Contains("OK"))
                    {
                        DB.UpdateIdxCard(con, row, anser, true);//заполнить load_result, load_time, id_card_in_dev=null
                        DB.DeleteCardInDev(con, row);//удалить строку
                    }
                    else
                    {
                        DB.UpdateIdxCard(con, row, anser, false);
                        DB.UpdateCardInDevIncrement(con, row);//atent+1
                    }
                    break;
            case 2:
                DB.DeleteCardInDev(con, row);
                break;
        }
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