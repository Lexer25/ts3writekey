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
            time = options.CardWriteConfig.timestart.Split(':');
            timestart = new TimeSpan(Int32.Parse(time[0]), Int32.Parse(time[1]), Int32.Parse(time[2]));
            var now = new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, DateTime.Now.TimeOfDay.Seconds);
            deltasleep = (options.CardWriteConfig.run_now) ? TimeSpan.Zero :
                (timestart >= now) ? timestart - now : timestart - now + new TimeSpan(1, 0, 0, 0);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("36 Start CardWrite.");
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
                _logger.LogTrace($@"48 Старт итерации");
                      run(_logger, db_config);//запуск логического процесса.
                _logger.LogTrace($@"50 Стоп итерации");
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
                //MainLine(dev,_logger, db_config);
                // Thread thread = new Thread(() => GetVersion(dev));
                Thread thread = new Thread(() => MainLine(dev, _logger, db_config));
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
            _logger.LogTrace($@"167 Завершаю работу основного потока.");
            _logger.LogTrace("92 Завершение работы TS3. Время выполнения " + (DateTime.Now - startMain));
            return;
        }

        //остновной цикл обработки очереди 16.12.2024
        static void MainLine(DEV dev, ILogger _logger,string db_config)
        {
            DateTime start = DateTime.Now;
            string lineStat = "132 Start thread id_dev:" + dev.id + "|IP:" + dev.ip;
            DateTime _start = DateTime.Now;
           
            
            //создаю подключение к базе данных. Оно потребуется даже если нет связи с контроллером.
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
            //lineStat += "|DBConnectOk:" + (DateTime.Now - _start);

            //создаю экземпляр контроллера
            COM com = new COM();
            com.SetupString(dev.ip);
            if (com.ReportStatus())//если связь с контроллером имеется, то продолжаю работу
            {
                
                // выполнение команд для указанного контролллера.
                OneDev(con, _logger,dev, com);

                           }
            else // если нет связи, то увеличиваяю количество попыток с указанием, что нет связи
            {
                //нет связи - это происходит на 2,2 сек после старта программы
                //lineStat += "|DevConnectNo:" + (DateTime.Now - _start);
                lineStat += "|DevConnectNo:" ;


                //  DB.UpdateIdxCardsNoConnect(con, dev.id); //зафиксировал no connect

                //lineStat += "|UpdateIdxCardsNoConnect:" + (DateTime.Now - _start);
                //  DB.UpdateCardInDevIncrements(con, dev.id);//attempt+1

                //lineStat += "|UpdateCardInDevIncrements:" + (DateTime.Now - _start);

            }
            con.Close();//закрыл подключение к БД СКУД
            //lineStat += "|conClose:" + (DateTime.Now - _start);

            _logger.LogDebug("223  " + lineStat + "|Time_execute:" + (DateTime.Now - start));
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
            //Console.WriteLine(@$"281 sql GetComandForDevice_{DateTime.Now - start}");
            _logger.LogDebug(@$"281 sql GetComandForDevice id_dev= {dev.id} count {table.Rows.Count} time_exec:{DateTime.Now - start}");
            start = DateTime.Now;

            //собираю команды в один список cmds
            List<Command> cmds = new List<Command>();
            foreach (DataRow row in table.Rows)
            {
                string comand = ComandBuilder(row);
                
                cmds.Add(new Command(row, comand));

            }

            _logger.LogDebug(@$"258 Для id_dev= {dev.id} имеется {cmds.Count()} команд записи/удаления");

            //а теперь обрабаываю список команд cmds
            foreach (Command cmd in cmds)
            {
                string answer = "";
               // anser = com.ComandExclude(cmd.command);//выполнил команду
                answer = com.ComandExecute(cmd.command);//выполнил команду
                AfterComand(answer, con, cmd.dataRow,_logger);//зафиксировал результат в базе данных
                string log = $@"288 id_dev={dev.id}  | reader  {cmd.dataRow["id_reader"]} | IP {dev.ip} | {cmd.command} > {answer}";
                _logger.LogTrace(log);//зафиксировал результат в лог-файле
            }
          
        }

        /**формирую тектовую команду
         * @input строка из базы данных
         * @output текстовая строка - команда для драйвера Artonit2.dll
         */
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
            //string log = $@"300 {anser}";
            //_logger.LogTrace(log, LogLevel.Trace);//зафиксировал результат в лог-файле
            switch ((int)row["operation"])
            {
                case 1://1 - добавление карты в контроллер.
                    if (anser.ToUpper().Contains("OK"))
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

                    if (anser.ToUpper().Contains("OK"))
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
