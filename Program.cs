
using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.ComponentModel.Design;
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
       
        
        DateTime startMain = DateTime.Now;
        Config config_log = JsonSerializer.Deserialize<Config>(File.ReadAllText("conf.json"));
        if (!config_log.log_console) Console.WriteLine("log false in conf.json");
        Log.log($@"Старт программы TS3. Версия 4.");
        if (!File.Exists("conf.json"))
        File.AppendAllText("conf.json",@$"{{
  ""db_config"": ""User = SYSDBA; Password = temp; Database =  C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB; DataSource = 127.0.0.1; Port = 3050; Dialect = 3; Charset = win1251; Role =;Connection lifetime = 15; Pooling = true; MinPoolSize = 0; MaxPoolSize = 50; Packet Size = 8192; ServerType = 0;"",
  ""selct_card"": ""cardindev_getlist(1)""}}");

        Log.log($@"26 Подключение к базе данных {config_log.db_config}");

        FbConnection con = DB.Connect(config_log.db_config);
        try
        {
            con.Open();
         
                Log.log($@"36 Подключение к базе данных выполнено успешно.");
                string procName = "CARDINDEV_TS3";
                if (DB.checkProc(con, procName))
                {
                    Log.log($@"39 Процедура {procName} в базе данных зарегистрирована.");
                }
                else
                {
                    Log.log($@"42 Процедура {procName} в базе данных НЕ зарегистрирована. Программа завершает работу.");
                    return;
                }
         
        }

        catch (Exception Ex)
        {
            Log.log("48 Не могу подключиться к базе данных " + config_log.db_config + ". Проверьте строку подключения. Программа TS3 завершает работу.");
            Log.log("82 \n" + Ex.Message.ToString());
            return;
        }


        //Получаю список контроллеров, для которых есть очередь.
      

        DataTable table = DB.GetDevice(con, config_log.selct_card);
        Log.log("55 Имеется " + DB.cardInDevGetList(con, config_log.selct_card).Rows.Count + " записей в очереди для  " + table.Rows.Count + " контроллеров. Время выполнения " + (DateTime.Now- startMain));
       
        
        //теперь проверяю настройки: разделяю список на тех, у кого есть IP адрес. и у кого нет IP адреса.
        foreach (DataRow row in table.Rows)
        {
            if (row["netaddr"].ToString() != "")
            {
                devs.Add(new DEV(row)); //у этих есть IP адрес, и далее буду работать с ними.
            } else
            {
              // devListNoIP.Add(new DEV(row));//тут собраны контроллеры без IP адреса

            }

        }

        //Log.log("72 Нет ip адресов для " + devListNoIP.Count + " контроллеров. А для " +(table.Rows.Count - devListNoIP.Count) + " контроллеров IP адреса имеются.");
        Log.log("72 Есть ip адреса для " + devs.Count + " контроллеров. А для " +(table.Rows.Count - devs.Count) + " контроллеров IP адресов нет.");


        if (devs.Count == 0)
        {
            string mess= "54 Нет данных для загрузки/удаления идентификаторов из контроллеров. Время выполнения " + (DateTime.Now - startMain);
            Log.log(mess);
            // Console.WriteLine(mess);

            mess = "58 Программа TS3 завершает работу: нет данных для работы. Время выполнения " + (DateTime.Now - startMain);
            Log.log(mess);
            //Console.WriteLine(mess);
            return;
        }

        Log.log("64 Имеются данных для загрузки/удаления идентификаторов в " + devs.Count+ " контроллеров. Время выполнения " + (DateTime.Now - startMain));

      
        Log.log("70 Начинаю основной поток. Время выполнения " + (DateTime.Now - startMain));
       
        List <Thread> threads = new List<Thread>();
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
        


        //ждем завершение всех потоков.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        Log.log($@"thread_end");
        Log.log("92 Завершение работы TS3. Время выполнения " + (DateTime.Now - startMain));

        return;
    }

    //остновной цикл обработки очереди 16.12.2024
    public static void mainLine(Config config_log, DEV dev)
    {
       
        DateTime start = DateTime.Now;
        string lineStat = "132 Start mainLine id_dev:" + dev.id + "|time:"+start;
        DateTime _start = DateTime.Now;
        COM com = new COM();
        com.SetupString(dev.ip);
          
        FbConnection con = DB.Connect(config_log.db_config);
        try
        {
            con.Open();
            //lineStat = lineStat + "|conOpen:" + (DateTime.Now - _start);
        }
        catch (Exception Ex)
        {
            Log.log("144 Не могу подключиться к базе данных в потоке mainLine для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start) + ". Завершаю  работу с этим устройством.");
           
            Log.log("147 id_dev=" + dev.id + " IP " + dev.ip + "mess " + Ex.Message.ToString());
            return;


        }


          if (con.State != ConnectionState.Open)
            {
                Console.WriteLine("179 no connect db " + config_log.db_config + ". Завершаю поток для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start));
                return;
            }
          lineStat = lineStat + "|DBConnectOk:" + (DateTime.Now - _start);

      
        if (com.ReportStatus())//если связь с контроллером имеется, то продолжаю работу
        {
            lineStat = lineStat + "|DevConnectOk:" + (DateTime.Now - _start);
            
           //начинаю формировать список команд для контроллера для последующей обработки
            DataTable table = DB.GetComandForDevice(con, dev.id, config_log.selct_card);
            

            lineStat = lineStat + "|GetComandForDevice:" + (DateTime.Now - _start);
            start = DateTime.Now;
            // Log.log("136 "+ dev.id + " | " + dev.id + " | " + dev.controllerName + " | " + dev.ip + " | reportStatus OK | count " + table.Rows.Count);


            //формирую лист команд.
            /*  List<Command> cmds = new List<Command>();
              foreach (DataRow row in table.Rows)
              {
                  string comand = ComandBuilder(row);
                  //string log = $@"{dev.id}  | {row["id_reader"]} | {dev.ip} | {comand} > добавить операцию в поток";
                  cmds.Add(new Command(row, comand));
                  //Log.log(log);
                  //надо доабавить в лог ответ
              }

            */
            lineStat = lineStat + "|startOneDev:" + (DateTime.Now - _start);
            
            // выполнение команд для указанного контролллера.
            OneDev(con, config_log, dev, com);
            
            lineStat = lineStat + "|stopOneDev:" + (DateTime.Now - _start);

           
        }
        else // если нет связи, то увеличиваяю количество попыток с указанием, что нет связи
        {
            //нет связи - это происходит на 2,2 сек после старта программы
                lineStat = lineStat + "|DevConnectNo:" + (DateTime.Now - _start);


            DB.UpdateIdxCardsNoConnect(con, dev.id); //зафиксировал no connect
            
                lineStat = lineStat + "|UpdateIdxCardsNoConnect:" + (DateTime.Now - _start);
            DB.UpdateCardInDevIncrements(con, dev.id);//attempt+1
            
                lineStat = lineStat + "|UpdateCardInDevIncrements:" + (DateTime.Now - _start);

        }
        con.Close();//закрыл подключение к БД СКУД
            lineStat = lineStat + "|conClose:" + (DateTime.Now - _start);
        
        Log.log("223  " + lineStat + "|Time_execite:" + (DateTime.Now - start));
        
    }
    public static void GetDev(DEV dev)
    {
        COM com = new COM();
        com.SetupString(dev.ip);
        dev.connect = com.ReportStatus();
        //dev.connect = true;

        
    }

  

    public static void GetVersion(DEV dev)
    {
        COM device = new COM();
        device.SetupString(dev.ip);
        string version = device.ComandExclude("getversion");
        Console.WriteLine(version);
      
    }

    /**
     * работа с указанным контроллером: выборка списка команда, из запись и фиксация результата.
     * 
     */
    private static void OneDev(FbConnection con,Config log_config,DEV dev, COM com) 
    {
       
        DateTime start = DateTime.Now;
        
        //беру список карт для точек прохода указанного контроллера
        DataTable table = DB.GetComandForDevice(con, dev.id, log_config.selct_card);
            Console.WriteLine(@$"281 sql GetComandForDevice_{DateTime.Now - start}");
            Log.log(@$"281 sql GetComandForDevice id_dev= {dev.id} time_exec:{DateTime.Now - start}");
        start = DateTime.Now;
       
        List<Command> cmds = new List<Command>();
        foreach (DataRow row in table.Rows)
        {
            string comand = ComandBuilder(row);
            //string log = $@"{dev.id}  | {row["id_reader"]} | {dev.ip} | {comand} > добавить операцию в список команд.";
            cmds.Add(new Command(row, comand));
            
        }
        

            foreach (Command cmd in cmds)
            {
                string anser = com.ComandExclude(cmd.command);//выполнил команду
                AfterComand(anser, con, cmd.dataRow);//зафиксировал результат в базе данных
                string log = $@"288 {dev.id}  | {cmd.dataRow["id_reader"]} | {dev.ip} | {cmd.command} > {anser}";
                Log.log(log);//зафиксировал результат в лог-файле
            }
           // con.Close();
 
        
        //Console.WriteLine("thread_start");
        //Console.WriteLine(@$"sql_con_{DateTime.Now - start}");
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
            case 1://1 - добавление карты в контроллер.
                    if (anser.Contains("OK"))
                    {
                        DB.UpdateIdxCard(con, row, anser, true);//заполнить load_result, load_time, id_card_in_dev=null
                        DB.DeleteCardInDev(con, row);//удалить строку из cardindev
                    }
                    else
                    {
                        DB.UpdateIdxCard(con, row, anser, false);
                        DB.UpdateCardInDevIncrement(con, row);//attempt+1 cardindev
                    }
                    break;
            case 2://удаление карты из контроллера

                if (anser.Contains("OK"))
                {
                    DB.DeleteCardInDev(con, row);
                } else
                {
                    DB.UpdateCardInDevIncrement(con, row);//attempt+1

                }
                break;
        }
    }
}


