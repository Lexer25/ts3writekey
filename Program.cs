
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
        Config config_log = JsonSerializer.Deserialize<Config>(File.ReadAllText("conf.json"));
        if (!config_log.log_console) Console.WriteLine("log false in conf.json");
        Log.log($@"Старт программы TS3");
        if (!File.Exists("conf.json"))
        File.AppendAllText("conf.json",@$"{{
  ""db_config"": ""User = SYSDBA; Password = temp; Database =  C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB; DataSource = 127.0.0.1; Port = 3050; Dialect = 3; Charset = win1251; Role =;Connection lifetime = 15; Pooling = true; MinPoolSize = 0; MaxPoolSize = 50; Packet Size = 8192; ServerType = 0;"",
  ""selct_card"": ""cardindev_getlist(1)""}}");

        Log.log($@"Подключение к базе данных {config_log.db_config}");

        FbConnection con = DB.Connect(config_log.db_config);
        try
        {
            con.Open();
            if(con.State != ConnectionState.Open)
            {
                Log.log($@"Подключение к базе не удалось.");
                return;
            }
            Log.log($@"Подключение к базе данных выполнено успешно.");
        }
        catch {
            Log.log("Не могу подключиться к базе данных "+ config_log.db_config+". Программа завершает работу.");
            return; 
        }
        //DataTable table = DB.GetDeviceList(con, config_log.selct_card);
        DataTable table = DB.GetDevice(con, config_log.selct_card);
        foreach(DataRow row in table.Rows)
        {
            if (row["netaddr"].ToString() != "") 
                devs.Add(new DEV(row));  
        }
       
        if(devs.Count == 0)
        {
            string mess="Нет данных для загрузки/удаления идентификаторов из контроллеров.";
            Log.log(mess);
           // Console.WriteLine(mess);

            mess="Программа TS3 завершает работу: нет данных для работы.";
            Log.log(mess);
            //Console.WriteLine(mess);
            return;
        }

        Log.log("Имеются данных для загрузки/удаления идентификаторов в " + devs.Count+ " контроллеров.");
        //con.Close();
        Log.log("Проверка связи с контроллерами старт.");
        //готовим список контроллеров, которые на связи
        //и формируем список указателей на потоки.
        List<Thread> threads = new List<Thread>();
        foreach (DEV dev in devs)
        {
           // Thread thread = new Thread(() => GetDev(dev));
           // Thread thread = new Thread(() => GetVersion(dev));
            Thread thread = new Thread(() => mainLine(config_log, dev));
            threads.Add(thread);
            thread.Start();
        }   
        //ждем завершение всех потоков.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Log.log($@"thread_end");

        Log.log("Проверка связи с контроллерами стоп.");

        //цикл Бухаров 1.
        /*
        Log.log($@"Сбор версий");
        List<Thread> getVersion = new List<Thread>();
        foreach (DEV dev in devs)
        {
            Thread thread = new Thread(() => GetVersion(dev));
            threads.Add(thread);
            thread.Start();
        }
        //ждем завершение всех потоков.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Log.log($@"thread_end");
        */
        return;

        //цикл Бухаров 2.

        Log.log($@"основной цикл работы с контроллером.");
        List<Thread>  getVersion = new List<Thread>();
        foreach (DEV dev in devs)
        {
            Thread thread = new Thread(() => mainLine(config_log, dev));
            threads.Add(thread);
            thread.Start();
        }
        //ждем завершение всех потоков.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Log.log($@"thread_end");



        return;


        foreach (DEV dev in devs)
        {
            con = DB.Connect(config_log.db_config);
            con.Open();
            if (dev.connect)
            {
                OneDev(con, config_log, dev);
            }
            else
            {
                DB.UpdateIdxCards(con, dev.id);
                DB.UpdateCardInDevIncrements(con, dev.id);
                Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect);
                con.Close();
            }
            /*Thread thread = new Thread(() => OneDev(config_log, dev,config_log));
            threads.Add(thread);
            Console.WriteLine("thread_add");
            thread.Start();
            Console.WriteLine("thread_start");*/
        }
        Log.log($@"Стоп программы TS3");
    }
    public static void GetDev(DEV dev)
    {
        COM com = new COM();
        com.SetupString(dev.ip);
        dev.connect = com.ReportStatus();
        //dev.connect = true;

        
    }
    
    //остновной цикл обработки очереди
    public static void mainLine(Config config_log, DEV dev)
    {
        DateTime start = DateTime.Now;
        
        //получил список команд для обработки
        FbConnection con = DB.Connect(config_log.db_config);
        con.Open();
        if (con.State != ConnectionState.Open) {
            Console.WriteLine("168 no connect db " + config_log.db_config);
            return;
        }

        DataTable table = DB.GetDor(con, dev.id, config_log.selct_card);
        Console.WriteLine(@$"sql GetDor_{DateTime.Now - start}");
        start = DateTime.Now;
        Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect + " | count " + table.Rows.Count);
        //Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect);
        List<Command> cmds = new List<Command>();

        
        //подготовка списка команд
        foreach (DataRow row in table.Rows)
        {
            string comand = ComandBuilder(row);
            string log = $@"{dev.id}  | {row["id_door"]} | {dev.ip} | {comand} > добавить операцию в список команд.";
            cmds.Add(new Command(row, comand));
            Log.log(log);
        }
        


        //Thread thread = new Thread(() =>
        //{
        //реализация команд из списка в цикле
            COM com = new COM();
            com.SetupString(dev.ip);
        
        if (com.ReportStatus())
        {
            foreach (Command cmd in cmds)
            {
                string answer = com.ComandExclude(cmd.command);
                AfterComand(answer, con, cmd.dataRow);
                string log = $@"{dev.id}  | {cmd.dataRow["id_door"]} | {dev.ip} | {cmd.command} > {answer}";
                Log.log(log);
            }
        } else
        {
             DB.UpdateIdxCards(con, dev.id); //зафиксировал no connect
             DB.UpdateCardInDevIncrements(con, dev.id);//attempt+1

        }
        con.Close();
        
        //});

        //Console.WriteLine(@$"sql_con_{DateTime.Now - start}");
    }

    public static void GetVersion(DEV dev)
    {
        COM device = new COM();
        device.SetupString(dev.ip);
        string version = device.ComandExclude("getversion");
        Console.WriteLine(version);
      
    }

    private static void OneDev(FbConnection con,Config log_config,DEV dev) 
    {
        //беру список карт для точек прохода указанного контроллера
        DateTime start = DateTime.Now;
        if (con.State == ConnectionState.Open) return;
   
        DataTable table = DB.GetDor(con, dev.id, log_config.selct_card);
        Console.WriteLine(@$"sql GetDor_{DateTime.Now - start}");
        start = DateTime.Now;
        // сделал экземпляр контроллера
        Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect + " | count " + table.Rows.Count);
        List<Command> cmds = new List<Command>();
        foreach (DataRow row in table.Rows)
        {
            string comand = ComandBuilder(row);
            string log = $@"{dev.id}  | {row["id_door"]} | {dev.ip} | {comand} > добавить операцию в список команд.";
            cmds.Add(new Command(row, comand));
            Log.log(log);
            //надо добавить в лог ответ
        }
        
        Thread thread = new Thread(() =>
        {
            COM com = new COM();
            com.SetupString(dev.ip);
            foreach (Command cmd in cmds)
            {
                string anser = com.ComandExclude(cmd.command);
                AfterComand(anser, con, cmd.dataRow);
                string log = $@"{dev.id}  | {cmd.dataRow["id_door"]} | {dev.ip} | {cmd.command} > {anser}";
                Log.log(log);
            }
            con.Close();
        });
        thread.Start();
        
        Console.WriteLine("thread_start");
        Console.WriteLine(@$"sql_con_{DateTime.Now - start}");
    }
    private static string ComandBuilder(DataRow? row)
    {
        string command = "";
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
                        DB.UpdateCardInDevIncrement(con, row);//attempt+1
                    }
                    break;
            case 2:
                DB.DeleteCardInDev(con, row);
                break;
        }
    }
}


