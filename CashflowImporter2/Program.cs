using StatementsImporterLib.Controllers;
using StatementsImporterLib.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashflowImporter2
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] _args = new string[4];
            _args[0] = "/dbf";
            _args[1] = @"d:\dbf\test1\bank2.dbf";
            _args[2] = "/comp";
            _args[3] = "za";
            Launcher.Run(args);
        }
    }
        class Launcher
        {
            public static void Run(string[] args)
            {
                // DEFAULT 
                string def_dbf = "bank.DBF";
                const Company def_comp = Company.MS;

                const string key_dbf = "/dbf";
                const string key_def = "/default";
                const string key_comp = "/comp";

                string dbf = "";
                
                dbf = def_dbf;
                Company comp = def_comp;

                bool DefaultParams = args.Length == 0 ? true : false;
                try
                {
                    for (int i = 0; i < args.Length; i += 2)
                    {
                        if (DefaultParams)
                            continue;
                        switch (args[i])
                        {
                            case key_def:
                                DefaultParams = true;
                                break;
                            case key_dbf:
                                dbf = args[i + 1];
                                break;
                            case key_comp:
                                string s = args[i + 1];
                                comp = Helper.GetCompany(s);
                                break;
                            default:
                                Helper.PrintInfo();
                                return;
                        }
                    }
                }
                catch (Exception)
                {
                    Helper.PrintInfo();
                    return;
                }

                if (DefaultParams)
                {
                    dbf = def_dbf;
                    comp = def_comp;
                }
                string host = Properties.Settings.Default.ts_host;
                string db = Properties.Settings.Default.ts_db;
                string user = Properties.Settings.Default.ts_user;
                string psw = Properties.Settings.Default.ts_psw;                

                string connectionstring = "metadata=res://*/ADO.TsXrmDbModel.csdl|res://*/ADO.TsXrmDbModel.ssdl|res://*/ADO.TsXrmDbModel.msl;provider=System.Data.SqlClient;provider connection string='Data Source="
                        + host
                        + ";Initial Catalog="
                        + db
                        + ";Persist Security Info=True;User ID=\""
                        + user
                        + "\";Password="
                        + psw + "'";
                if (!File.Exists(dbf))
                {
                    string s = "Файл не найден: " + dbf;
                    Console.WriteLine(s);
                    Helper.Log(s);
                    return;
                }
                DateTime startDate = Properties.Settings.Default.start_date;
                DateTime finishDate = Properties.Settings.Default.finish_date;

                Connector1C core = new Connector1C(dbf, comp);

                core.RunExport(connectionstring, startDate, finishDate, comp);                
            }
        }
    
}
