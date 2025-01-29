using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace WorkerService1
{
    public class CardWrite : BackgroundService
    {
        public static CardWriteConfig cardWriteConfig;
        public TimeSpan timeout;
        public TimeSpan timestart;
        public TimeSpan deltasleep;

        public readonly ILogger _logger;
        public readonly string db_config;
        public CardWrite(ILogger<CardWrite> logger, WorkerOptions options)
        {
            _logger = logger;
            db_config = options.db_config;
            cardWriteConfig = options.CardWriteConfig;
            var time = options.CardWriteConfig.timeout.Split(':');
            timeout = new TimeSpan(Int32.Parse(time[0]), Int32.Parse(time[1]), Int32.Parse(time[2]));
            time = options.CardWriteConfig.timeout.Split(':');
            timestart = new TimeSpan(Int32.Parse(time[0]), Int32.Parse(time[1]), Int32.Parse(time[2]));
            var now = new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, DateTime.Now.TimeOfDay.Seconds);
            deltasleep = (options.CardWriteConfig.run_now) ? TimeSpan.Zero :
                (timestart >= now) ? timestart - now : timestart - now + new TimeSpan(1, 0, 0, 0);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("start");
            return base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace(@$"time run worker1: {timestart} deltasleep: {deltasleep}");
            await Task.Delay(deltasleep);
            _logger.LogTrace("run worker1");
            while (!stoppingToken.IsCancellationRequested)
            {
                devs = new List<DEV>();
                devListNoIP = new List<DEV>();
                _logger.LogTrace($@"Старт итерации");
                run(_logger, db_config);
                _logger.LogTrace($@"timeout worker1: {timeout}");
                await Task.Delay(timeout);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("stop");
            return base.StopAsync(cancellationToken);
        }
        static List<DEV> devs = new List<DEV>();//список контроллеров, с которыми программа будет работать.
        static List<DEV> devListNoIP = new List<DEV>();//список контроллеров, для которых не указан IP адрес.
        private static void run(ILogger _logger, string db_config)
        {
            /*  
     COM com = new COM();
     com.SetupString("10.25.16.205");
     string command;
     //Console.WriteLine("23 " + com.getVersion);
     command = "reportstatus";
     command = "getDeviceTime";
     command = "writekey door=0, key=\"00203623\", TZ=1, status=0";

     string answer = com.ComandExclude(command);//выполнил команду
     Console.WriteLine("22 " +command +" answer:" + answer);
     return;
   */



            //var path = Path.Combine(Directory.GetCurrentDirectory(), "conf.json");
            //Config config_log = JsonSerializer.Deserialize<Config>(File.ReadAllText(path));


            // if (!config_log.log_console) Console.WriteLine("log false in conf.json");


            _logger.LogTrace($@"26 Подключение к базе данных {db_config}");

            Console.WriteLine(cardWriteConfig.stopList);


            FbConnection con = DB.Connect(db_config, _logger);//config_log.db_config);
            try
            {
                con.Open();

                _logger.LogDebug($@"36 Подключение к базе данных выполнено успешно.");
                string procName = "CARDINDEV_TS3";
                if (DB.checkProc(con, procName))
                {
                    _logger.LogDebug($@"39 Процедура {procName} в базе данных зарегистрирована.");
                }
                else
                {
                    _logger.LogError($@"42 Процедура {procName} в базе данных НЕ зарегистрирована. Программа завершает работу.");
                    // return;
                }

            }

            catch (Exception Ex)
            {
                _logger.LogCritical("48 Не могу подключиться к базе данных " + db_config + ". Проверьте строку подключения. Программа TS3 завершает работу.");
                _logger.LogCritical("82 \n" + Ex.Message.ToString());
                return;
            }
            DateTime startMain = DateTime.Now;
            //Получаю список контроллеров, для которых есть очередь.


            DataTable table = DB.GetDevice(con);
            _logger.LogDebug("55 Имеется " + DB.cardInDevGetList(con).Rows.Count + " записей в очереди для  " + table.Rows.Count + " контроллеров. Время выполнения " + (DateTime.Now - startMain));
            con.Close();//закрыть соединение 
            _logger.LogTrace("Закрыть соединение с бд");

            //теперь проверяю настройки: разделяю список на тех, у кого есть IP адрес. и у кого нет IP адреса.
            foreach (DataRow row in table.Rows)
            {
                if (row["netaddr"].ToString() != "")
                {
                    DEV newdev = new DEV(row);
                    foreach (DEV dev in devs)
                        if (newdev.ip == dev.ip)
                        {
                            _logger.LogError("Повторяется: " + newdev.ip);
                            newdev = null;
                        }
                    if (!(newdev is null)) devs.Add(newdev);
                    //у этих есть IP адрес, и далее буду работать с ними.
                }
                else
                {
                    // devListNoIP.Add(new DEV(row));//тут собраны контроллеры без IP адреса
                }

            }

            //Log.log("72 Нет ip адресов для " + devListNoIP.Count + " контроллеров. А для " +(table.Rows.Count - devListNoIP.Count) + " контроллеров IP адреса имеются.");
            _logger.LogDebug("72 Есть ip адреса для " + devs.Count + " контроллеров. А для " + (table.Rows.Count - devs.Count) + " контроллеров IP адресов нет.");


            if (devs.Count == 0)
            {
                string mess = "54 Нет данных для загрузки/удаления идентификаторов из контроллеров. Время выполнения " + (DateTime.Now - startMain);
                _logger.LogError(mess);
                // Console.WriteLine(mess);

                mess = "58 Программа TS3 завершает работу: нет данных для работы. Время выполнения " + (DateTime.Now - startMain);
                _logger.LogError(mess);
                //Console.WriteLine(mess);
                return;
            }

            _logger.LogDebug("64 Имеются данных для загрузки/удаления идентификаторов в " + devs.Count + " контроллеров. Время выполнения " + (DateTime.Now - startMain), LogLevel.Debug);


            _logger.LogTrace("70 Начинаю основной поток. Время выполнения " + (DateTime.Now - startMain));


            List<Thread> threads = new List<Thread>();
            foreach (DEV dev in devs)
            {
                MainLine(dev,_logger, db_config);
                // Thread thread = new Thread(() => GetVersion(dev));
                // Thread thread = new Thread(() => mainLine(config_log, dev));
                //threads.Add(thread);
                // thread.Start();
            }
            //ждем завершение всех потоков.
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            //  223  132 Start mainLine id_dev: 10 | IP:192.168.8.10 | time:14.01.2025 12:36:51 | DBConnectOk:00:00:00.1343070 | DevConnectNo:00:00:02.1627000 | UpdateIdxCardsNoConnect:00:00:02.1627018 | UpdateCardInDevIncrements:00:00:02.1627020 | conClose:00:00:02.1634080 | Time_execite:00:00:02.1635669                                                                                       223  132 Start mainLine id_dev: 7 | IP:192.168.8.7 | time:14.01.2025 12:36:51 | DBConnectOk:00:00:00.1343071 | DevConnectNo:00:00:02.1628451 | UpdateIdxCardsNoConnect:00:00:02.1628464 | UpdateCardInDevIncrements:00:00:02.1628468 | conClose:00:00:02.1634942 | Time_execite:00:00:02.1636581                                                                                         thread_end
            //223  132 Start mainLine id_dev:7|IP:192.168.8.7|time:14.01.2025 12:37:28|DBConnectOk:00:00:00.0885455|DevConnectNo:00:00:02.1158628|UpdateIdxCardsNoConnect:00:00:02.1158650|UpdateCardInDevIncrements:00:00:02.1158652|conClose:00:00:02.1166158|Time_execite:00:00:02.1167255                                                                                         223  132 Start mainLine id_dev:10|IP:192.168.8.10|time:14.01.2025 12:37:30|DBConnectOk:00:00:00.0002577|DevConnectNo:00:00:02.0020725|UpdateIdxCardsNoConnect:00:00:02.0020758|UpdateCardInDevIncrements:00:00:02.0020762|conClose:00:00:02.0021264|Time_execite:00:00:02.0021378                                                                                       thread_end    
            //ждем завершение всех потоков.
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            _logger.LogTrace($@"thread_end");
            _logger.LogTrace("92 Завершение работы TS3. Время выполнения " + (DateTime.Now - startMain));
            return;
        }

        //остновной цикл обработки очереди 16.12.2024
        static void MainLine(DEV dev, ILogger _logger,string db_config)
        {
            DateTime start = DateTime.Now;
            string lineStat = "132 Start mainLine id_dev:" + dev.id + "|IP:" + dev.ip + "|time:" + start;
            DateTime _start = DateTime.Now;
            COM com = new COM();
            com.SetupString(dev.ip);
            FbConnection con = DB.Connect(db_config, _logger);
            try
            {
                con.Open();
                //lineStat = lineStat + "|conOpen:" + (DateTime.Now - _start);
            }
            catch (Exception Ex)
            {
                _logger.LogError("144 Не могу подключиться к базе данных в потоке mainLine для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start) + ". Завершаю  работу с этим устройством.");
                _logger.LogError("147 id_dev=" + dev.id + " IP " + dev.ip + "mess " + Ex.Message.ToString());
                return;
            }
            if (con.State != ConnectionState.Open)
            {
                Console.WriteLine("179 no connect db " + db_config + ". Завершаю поток для id_dev=" + dev.id + " IP " + dev.ip + " время выполнения " + (DateTime.Now - start), LogLevel.Error);
                return;
            }
            lineStat += "|DBConnectOk:" + (DateTime.Now - _start);
            if (com.ReportStatus())//если связь с контроллером имеется, то продолжаю работу
            {
                lineStat += "|DevConnectOk:" + (DateTime.Now - _start);

                //начинаю формировать список команд для контроллера для последующей обработки
                DataTable table = DB.GetComandForDevice(con, dev.id);


                lineStat += "|GetComandForDevice:" + (DateTime.Now - _start);
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
                lineStat += "|startOneDev:" + (DateTime.Now - _start);

                // выполнение команд для указанного контролллера.
                OneDev(con, _logger,dev, com);

                lineStat += "|stopOneDev:" + (DateTime.Now - _start);


            }
            else // если нет связи, то увеличиваяю количество попыток с указанием, что нет связи
            {
                //нет связи - это происходит на 2,2 сек после старта программы
                lineStat += "|DevConnectNo:" + (DateTime.Now - _start);


                //  DB.UpdateIdxCardsNoConnect(con, dev.id); //зафиксировал no connect

                lineStat += "|UpdateIdxCardsNoConnect:" + (DateTime.Now - _start);
                //  DB.UpdateCardInDevIncrements(con, dev.id);//attempt+1

                lineStat += "|UpdateCardInDevIncrements:" + (DateTime.Now - _start);

            }
            con.Close();//закрыл подключение к БД СКУД
            lineStat += "|conClose:" + (DateTime.Now - _start);

            _logger.LogDebug("223  " + lineStat + "|Time_execite:" + (DateTime.Now - start));
        }
        /*
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
    * работа с указанным контроллером: выборка списка команда, их запись и фиксация результата.
    * 
    */

        private static void OneDev(FbConnection con, ILogger _logger, DEV dev, COM com)
        {

            DateTime start = DateTime.Now;

            //беру список карт для точек прохода указанного контроллера
            DataTable table = DB.GetComandForDevice(con, dev.id);
            Console.WriteLine(@$"281 sql GetComandForDevice_{DateTime.Now - start}");
            _logger.LogDebug(@$"281 sql GetComandForDevice id_dev= {dev.id} time_exec:{DateTime.Now - start}");
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
                string anser = "";
                anser = com.ComandExclude(cmd.command);//выполнил команду
                AfterComand(anser, con, cmd.dataRow,_logger);//зафиксировал результат в базе данных
                string log = $@"288 {dev.id}  | {cmd.dataRow["id_reader"]} | {dev.ip} | {cmd.command} > {anser}";
                _logger.LogTrace(log);//зафиксировал результат в лог-файле
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
        private static void AfterComand(string anser, FbConnection con, DataRow? row,ILogger _logger)
        {
            string log = $@"300 {anser}";
            _logger.LogTrace(log, LogLevel.Trace);//зафиксировал результат в лог-файле
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
                    }
                    else
                    {
                        DB.UpdateCardInDevIncrement(con, row);//attempt+1
                    }
                    break;
            }
        }
    }

}
