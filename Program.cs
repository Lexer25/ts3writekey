
using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

partial class Program
{
    static List<DEV> devs = new List<DEV>();//список контроллеров, с которыми программа будет работать.
    static List<DEV> devListNoIP = new List<DEV>();//список контроллеров, для которых не указан IP адрес.
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
<<<<<<< main
            Log.log($@"Подключение к базе данных выполнено успешно.");
        }
        catch {
            Log.log("Не могу подключиться к базе данных "+ config_log.db_config+". Программа завершает работу.");
            return; 
        }
=======
            if (con.State != ConnectionState.Open)
            {
                Log.log($@"33 Подключение к базе не удалось. Программа TS3 завершает работу.");
                return;
            } else {
                Log.log($@"36 Подключение к базе данных выполнено успешно.");
                string procName = "CARDINDEV_TS3";
                if (DB.checkProc(con, procName)) {
                    Log.log($@"39 Процедура {procName} в базе данных зарегистрирована.");
                } else
                {
                    Log.log($@"42 Процедура {procName} в базе данных НЕ зарегистрирована. Программа завершает работу.");
                    return;
                }
            }
        }
        catch {
            Log.log("48 Не могу подключиться к базе данных "+ config_log.db_config+". Проверьте строку подключения. Программа TS3 завершает работу.");
            return; 
        }


        //Получаю список контроллеров, для которых есть очередь.
      
>>>>>>> local
        DataTable table = DB.GetDevice(con, config_log.selct_card);
        Log.log("55 Имеется " + DB.cardInDevGetList(con, config_log.selct_card).Rows.Count + " записей в очереди для  " + table.Rows.Count + " контроллеров.");

        foreach (DataRow row in table.Rows)
        {
            if (row["netaddr"].ToString() != "")
            {
                devs.Add(new DEV(row));
            } else
            {
               devListNoIP.Add(new DEV(row));//тут собраны контроллеры без IP адреса

            }

        }

        Log.log("72 Нет ip адресов для " + devListNoIP.Count + " контроллеров. А для " +(table.Rows.Count - devListNoIP.Count) + " контроллеров IP адреса имеются.");


        if (devs.Count == 0)
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
        con.Close();

        //готовим список контроллеров, которые на связи
        //и формируем список указателей на потоки.
        List<Thread> threads = new List<Thread>();
        foreach (DEV dev in devs)
        {
            Thread thread = new Thread(() => GetDev(dev));
            threads.Add(thread);
            thread.Start();
        }   
        //ждем завершение всех потоков.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Log.log($@"thread_end");


        //цикл Бухаров.
        
<<<<<<< main
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

        return;
=======
    }

    //остновной цикл обработки очереди
    public static void mainLine(Config config_log, DEV dev)
    {
        //Log.log("168 Старт потока для id_dev=" + dev.id + " IP " + dev.ip);
        string lineStat = "Start id_dev:" + dev.id;
        DateTime start = DateTime.Now;
        DateTime _start = DateTime.Now;
        COM com = new COM();
        com.SetupString(dev.ip);
        //время созадания экземпляра класса примерно 50-60 мс
        //Log.log("172 Создал экземпляр объекта для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));
        
        FbConnection con = DB.Connect(config_log.db_config);
        try
        {
            con.Open();
            lineStat = lineStat + "|conOpen:" + (DateTime.Now - _start);
        }
        catch (Exception Ex)
        {
            Log.log("144 Не могу подключиться к базе данных в потоке mainLine для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start) + ". Завершаю  работу с этим устройством.");
           
            Log.log("147 \n" + Ex.Message.ToString());
            return;


        }
          if (con.State != ConnectionState.Open)
            {
                Console.WriteLine("179 no connect db " + config_log.db_config + ". Завершаю поток для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));
                return;
            }
        //Это происходит на 0,07 сек с начала работы программы.
        //Log.log("177 Установил подключение к базе данных для объекта для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));
        lineStat = lineStat + "|checkConnectionStateDB:" + (DateTime.Now - _start);

        //test

        DataTable id_doors = DB.GetChildId(con, 1);

        foreach(DataRow id in id_doors)
        {

            Console.WriteLine(id[0]);

        }

        if (com.ReportStatus())
        {
            //Log.log("129 Есть связь test " + com.ReportStatus()); return;

            lineStat = lineStat + "|checkConnectionStateDevOK:" + (DateTime.Now - _start);

            
            //получаю список команд для обработки
            //Log.log("189 Есть связь с id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));



            DataTable table = DB.GetDor(con, dev.id, config_log.selct_card);
            Console.WriteLine(@$"sql GetDor_{DateTime.Now - start}");

            lineStat = lineStat + "|getDoor:" + (DateTime.Now - _start);
            start = DateTime.Now;
            Log.log("136 "+ dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | reportStatus OK | count " + table.Rows.Count);
             List<Command> cmds = new List<Command>();
>>>>>>> local


        foreach (DEV dev in devs)
        {
            con = DB.Connect(config_log.db_config);
            con.Open();
            if (dev.connect)
            {
                OneDev(con, config_log, dev);
            }
<<<<<<< main
            else
=======

            lineStat = lineStat + "|makeCommandList:" + (DateTime.Now - _start);

            //Thread thread = new Thread(() =>
            //{
            //реализация команд из списка в цикле

            foreach (Command cmd in cmds)
>>>>>>> local
            {
                DB.UpdateIdxCards(con, dev.id);
                DB.UpdateCardInDevIncrements(con, dev.id);
                Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect);
                con.Close();
            }
<<<<<<< main
            /*Thread thread = new Thread(() => OneDev(config_log, dev,config_log));
            threads.Add(thread);
            Console.WriteLine("thread_add");
            thread.Start();
            Console.WriteLine("thread_start");*/
        }
        Log.log($@"Стоп программы TS3");
=======
            lineStat = lineStat + "|makeComandCicle:" + (DateTime.Now - _start);
        }
        else // если нет связи, то увеличиваяю количество попыток с указанием, что нет связи
        {
            //нет связи - это происходит на 2,2 сек после старта программы
            lineStat = lineStat + "|checkConnectionStateDevNO:" + (DateTime.Now - _start);


            //Log.log("213 Нет связи с id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));
            DB.UpdateIdxCardsNoConnect(con, dev.id); //зафиксировал no connect
            //Log.log("215 Обновил cardIdx not connect id_dev=" + dev.id + " IP " + dev.ip + ",  время выполнения " + (DateTime.Now - start));

            DB.UpdateCardInDevIncrements(con, dev.id);//attempt+1
            //Log.log("215 Обновил cardIndev not connect id_dev=" + dev.id + " IP " + dev.ip + "  время выполнения " + (DateTime.Now - start));

            lineStat = lineStat + "|updateDbForDevNoConnect:" + (DateTime.Now - _start);

        }
        con.Close();
        lineStat = lineStat + "|conClose:" + (DateTime.Now - _start);
        //Console.WriteLine(@$"sql_con_{DateTime.Now - start}");
        Log.log("223  " + lineStat);
        Log.log("224 Стоп потока для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));
>>>>>>> local
    }
    public static void GetDev(DEV dev)
    {
        COM com = new COM();
        com.SetupString(dev.ip);
        dev.connect = com.ReportStatus();
        
    }
    
    public static void GetVersion(DEV dev)
    {
        COM device = new COM();
        device.SetupString(dev.ip);
   
        Console.WriteLine(device.GetVersion());
       // Console.WriteLine(device.ReportStatus());
        //string ver = device.GetVersion();
        //dev.connect = com.ReportStatus();
       // Log.log($@"Версия контроллера "+ ver);
    }

    private static void OneDev(FbConnection con,Config log_config,DEV dev) 
    {
        //беру список карт для точек прохода указанного контроллера
        DateTime start = DateTime.Now;
        DataTable table = DB.GetDor(con, dev.id, log_config.selct_card);
        Console.WriteLine(@$"sql GetDor_{DateTime.Now - start}");
        start = DateTime.Now;
        // сделал экземпляр контроллера
        Log.log(dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | " + dev.connect + " | count " + table.Rows.Count);
        List<Command> cmds = new List<Command>();
        foreach (DataRow row in table.Rows)
        {
            string comand = ComandBuilder(row, con);
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
    private static string ComandBuilder(DataRow? row, FbConnection con)
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


