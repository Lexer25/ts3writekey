using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class DB 
    {
        public static FbConnection Connect(string connectionString)
        {        
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
        bool DBPooling = true;
            if (connectionString == "") 
            connectionString = $@"User = {DBuser}; Password = {DBpassword}; Database = {DBpath}; DataSource = {DBDataSource}; Port = {DBport}; 
            Dialect = {DBDialect}; Charset = {DBCharset}; Role ={DBRoule};Connection lifetime = {DBConnectionlifetime}; Pooling = {DBPooling};
            MinPoolSize = {DBMinPoolSize}; MaxPoolSize = {DBMaxPoolSize}; Packet Size = {DBPacket_Size}; ServerType = {DBServerType};";
            FbConnection con = new FbConnection(connectionString);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return con;
        }
        public static DataTable Test1(FbConnection con)
        {
            string sql = "select count(*) from cards";
            FbCommand getip = new FbCommand(sql, con);
            var reader = getip.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return table;
        }
        
        public static DataTable GetDevice(FbConnection con, string procdb)
        {
            List<DEV> devs = new List<DEV>();
            FbCommand getip = new FbCommand(@$"select distinct d2.id_dev as id_controller, d2.name as controllerName, d2.netaddr from {procdb} 
                    cg join device d on d.id_dev=cg.id_dev 
                    join device d2 on d2.id_ctrl=d.id_ctrl and d2.id_reader is null", con);
            var reader = getip.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return table;
        }

        public static DataTable GetDeviceList(FbConnection con, string procdb)
        {
            List<DEV> devs = new List<DEV>();
            FbCommand getip = new FbCommand(@$"select first 5 d.id_dev as id_controller, d.name as controllerName, d.netaddr from device d
where d.id_reader is null", con);
            var reader = getip.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return table;
        }




        /**Получаю список точек прохода, в которые надо записать/удалить карты
         * input id - id контроллера
         */
        public static DataTable GetDor(FbConnection con, int id, string procdb)
        {
            string sql = $@"select distinct  cg.id_dev as id_door, d.netaddr, d.id_reader, cg.id_card, cg.timezones, cg.operation, cg.id_cardindev from {procdb} cg
                join device d on d.id_dev=cg.id_dev
                join device d2 on d2.id_ctrl=d.id_ctrl and  d2.id_reader is null
                where d2.id_dev={id}
                order by cg.id_cardindev";
           // Console.WriteLine("sql "+ sql);
            
            FbCommand getcomand = new FbCommand(sql, con);
 
            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
           return table;
        }
        public static bool UpdateIdxCard(FbConnection con, DataRow row,string result,bool s_e)
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
        public static bool UpdateIdxCardsNoConnect(FbConnection con, int id)
        {
            string sql = $@"update cardidx cdx
                set cdx.load_time='now',
                cdx.load_result='no connect'
                where  cdx.id_cardindev is not null
                and cdx.id_dev in (
                select d.id_dev from device d
                join device d2 on d2.id_ctrl=d.id_ctrl and d2.id_reader is null
                where d.id_reader in (0,1)
                and  d2.id_dev={id})";
            //Log.log("142 UpdateIdxCardsNoConnect " + sql);
            FbCommand getcomand = new FbCommand(sql, con);
            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return true;
        }
        public static bool UpdateCardInDevIncrements(FbConnection con, int id)
        {
            string sql = $@"update cardindev cd
                set cd.attempts=cd.attempts+1
                where cd.id_dev in (
                select d.id_dev from device d
                join device d2 on d2.id_ctrl=d.id_ctrl and d2.id_reader is null
                where d.id_reader in (0,1)
                and d2.id_dev={id})";
            FbCommand getcomand = new FbCommand(sql, con);
            //cdx.id_cardindev=null
            var reader = getcomand.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            return true;
        }
    }
}
