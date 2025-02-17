﻿using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WorkerService1
{
    class DB
    {
        public static ILogger _logger;
        public static FbConnection Connect(string connectionString, ILogger logger)
        {
            _logger = logger;
            string DBuser = "SYSDBA",
            DBpassword = "temp",
            DBpath = " C:\\db\\ShieldPro_rest.GDB",
            DBDataSource = "127.0.0.1",
            DBCharset = "win1251",
            DBRoule = "";
            int DBport = 3050,
            DBDialect = 3,
            DBConnectionlifetime = 15,
            DBMinPoolSize = 0,
            DBMaxPoolSize = 50,
            DBPacket_Size = 8192,
            DBServerType = 0;
            bool DBPooling = false;
            if (connectionString == "")
                connectionString = $@"User = {DBuser}; Password = {DBpassword}; Database = {DBpath}; DataSource = {DBDataSource}; Port = {DBport}; 
            Dialect = {DBDialect}; Charset = {DBCharset}; Role ={DBRoule};Connection lifetime = {DBConnectionlifetime}; Pooling = {DBPooling};
            MinPoolSize = {DBMinPoolSize}; MaxPoolSize = {DBMaxPoolSize}; Packet Size = {DBPacket_Size}; ServerType = {DBServerType};";
            FbConnection con = new FbConnection(connectionString);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return con;
        }

        /** Список всех записей из таблицы cardindev
         * 
         * 
         */
        public static DataTable cardInDevGetList(FbConnection con)
        {
            //string sql = @$"select * from {procd}";
            string sql = @$"select * from cardindev";
            FbCommand getip = new FbCommand(sql, con);
            var reader = getip.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return table;
        }
        /** Список контроллеров, куда надо будет загружать карты
         * 
         */
        public static DataTable GetDevice(FbConnection con)
        {
            List<DEV> devs = new List<DEV>();


            string sql = @$"select distinct d2.id_dev as id_controller, d2.name as controllerName, d2.netaddr
     from CardInDev c
     join Device d  on (c.id_dev=d.id_dev) and (c.id_db=d.id_db)
    left join card cc on cc.id_card=c.id_card
    join device d2 on d2.id_ctrl=d.id_ctrl and (d2.id_devtype in (1,2, 4, 6)) and d2.id_reader is null
    where (c.id_db=1)
    and ( 0 <> (select IS_ACTIVE from DEVICE_CHECKACTIVE(d.id_dev)) )";

            if (CardWrite.cardWriteConfig.stopList != null) sql = sql + " and d2.id_dev not in " + CardWrite.cardWriteConfig.stopList;


            if (CardWrite.cardWriteConfig.SqlGetDevice != null) sql = CardWrite.cardWriteConfig.SqlGetDevice;// если в файле конфигурации указан SQL запрос, то использовать его


            //_logger.LogDebug($@"75 " + sql);
            FbCommand getip = new FbCommand(sql, con);
            var reader = getip.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return table;
        }


        /** Список всех контроллеров.
         * 
         */



        /**Получаю список команд загрузки для указанного контроллера.
         * input id - контроллер!!!
         */
        public static DataTable GetComandForDevice(FbConnection con, int id)
        {
            string sql = $@"select distinct  cg.id_dev, cg.id_reader, cg.id_card, cg.timezones, cg.operation, cg.id_cardindev from cardindev_ts3(1, {id}) cg
             order by cg.id_cardindev";

            //_logger.LogDebug($@"121 " + sql);

            FbCommand getcomand = new FbCommand(sql, con);

            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return table;
        }


        /**20.12.2024 Обновление таблицы cardidx по результатам записи/удаления карты.
         * при успешной записи/удалении карты фиксируется дата, время, ответ (ОК), id_cardindev ставится null
         * при неуспешной попытке записи/удалении фиксируется дата, время, ответ (err). id_cardindev остается без изменений, т.к. попытки записи будут продолжаться.
         * 
         */
        public static bool UpdateIdxCard(FbConnection con, DataRow row, string result, bool s_e)
        {
            string id_cardindev = "";
            if (s_e)
            {
                id_cardindev = ",cdx.id_cardindev=null";
            }
            string sql = $@"update cardidx cdx
                set cdx.load_time='now',
                cdx.load_result='{result.Substring(0, Math.Min(result.Length, 100))}'
                {id_cardindev}
                where cdx.id_cardindev={row["id_cardindev"]}";
            FbCommand getcomand = new FbCommand(sql, con);
            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return true;
        }
        public static bool DeleteCardInDev(FbConnection con, DataRow row)
        {
            FbCommand getcomand = new FbCommand($@"delete from cardindev cd
            where cd.id_cardindev ={row["id_cardindev"]}", con);
            //cdx.id_cardindev=null
            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return true;
        }
        public static bool UpdateCardInDevIncrement(FbConnection con, DataRow row)
        {
            FbCommand getcomand = new FbCommand($@"update cardindev cd
            set cd.attempts=cd.attempts+1
            where cd.id_cardindev={row["id_cardindev"]}", con);
            //cdx.id_cardindev=null
            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return true;
        }


        /** 20.12.2024 Обновление таблицы cardidx при отсутсвии связи с устройством.
         * в таблицу записывается дата, время и сообщение, что нет связи с устройством.
         * Особенность: для ускорения работы необходимо указывать id точек прохода.
         * 
         */

        /** 20.12.2024 Обновление таблицы cardindev при отсутсвии связи с устройством.
         * выполняется инкремент поля attempt.
         * Особенность: для ускорения работы необходимо указывать id точек прохода.
         * 
         */



        public static bool checkProc(FbConnection con, string procName)
        {
            string sql = $@"SELECT * FROM RDB$PROCEDURES WHERE RDB$PROCEDURE_NAME ='{procName}'";
            FbCommand getcomand = new FbCommand(sql, con);
            //cdx.id_cardindev=null
            var reader = getcomand.ExecuteReader();
            return reader.HasRows;
        }


        /**Получаю список "дочек" - точек прохода
         * 
         */
      


    }
}
