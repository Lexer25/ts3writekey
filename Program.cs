
using ConsoleApp1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text.Json;

partial class Program
{
    static void Main(string[] args)
    {
        FbConnection con = DB.Connect(System.IO.File.ReadAllText("DBsetting.txt"));
        con.Open();
        List<DEV> devs = new List<DEV>();
        DataTable table = DB.GetDevice(con, "cardindev_getlist(1)");
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            if (row["netaddr"].ToString() != "")
                devs.Add(new DEV(row));
        }
        foreach (DEV dev in devs)
        {
            Console.WriteLine(dev.id.ToString());
            table = DB.GetDor(con, dev.id);
           
                //Обработка таблицы Card in dev
            List<Comand> list = new List<Comand>();
            for (int i = 0; i < table.Rows.Count; i++) list.Add(CardinDev(con, dev, table.Rows[i]));



         //   List<Thread> threads = new List<Thread>();
          /*  Thread thread = new Thread(() => );
            threads.Add(thread);
            thread.Start();
            foreach (Thread thread in threads) thread.Join*/
        }
        con.Close();
    }
    private static Comand CardinDev(FbConnection con, DEV dev, DataRow row)
    {
        //Проверка связи 
        Comand com = new Comand();

        //com.SetupString("192.168.8.18");
        com.SetupString(dev.ip);
        Config_Log.log(dev.ip +" | "+ com.ComandExclude($@"ReportStatus"));
        //Console.WriteLine(row["netaddr"]);
        if (com.ReportStatus())
        {
            string comand = ComandParser(row, con, com);
            string log = $@"{dev.ip} | {comand}";
            Config_Log.log(log);
        }
        else
        {
            DB.UpdateIdxCard(con, row, "no connect");
            DB.UpdateCardInDevIncrement(con, row);
        }
        return com;
    }
    private static string ComandParser(DataRow? row, FbConnection con, Comand comand)
    {
        string anser = "", command = "";
        switch ((int)row["operation"])
        {
            case 1:
                command = $@"writekey door={0}, key=""{row["id_card"]}"", TZ={row["timezones"]}, status={0}";
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