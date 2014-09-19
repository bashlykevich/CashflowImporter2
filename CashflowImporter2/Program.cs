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
            Launcher.Run(args);
        }
    }
        class Launcher
        {
            public static void Run(string[] args)
            {                
                string sourceFile = "";
                string companyCode = "";
                bool finishDateIsNow = false;
                try
                {
                    // первый параметр - имя файла
                    sourceFile = args[0];
                    // второй параметр - имя компании
                    companyCode = args[1];
                    // 3 параметр - опциональный ключ импорта по текущую дату
                    if (args.Count() > 2)
                        if (args[2] == "-to-date")
                            finishDateIsNow = true;
                }
                catch (Exception)
                {
                    Helper.PrintInfo();
                    return;
                }
                if (!File.Exists(sourceFile))
                {
                    string s = "Файл не найден: " + sourceFile;
                    Console.WriteLine(s);
                    Helper.Log(s);
                    return;
                }
                Company company = Helper.GetCompany(companyCode);
                
                string host = Properties.Settings.Default.ts_host;
                string db = Properties.Settings.Default.ts_db;
                string user = Properties.Settings.Default.ts_user;
                string psw = Properties.Settings.Default.ts_psw;
                string connectionString = Helper.GenerateConnectionString(host, db, user, psw);
                               
                DateTime startDate = Properties.Settings.Default.start_date;
                DateTime finishDate = DateTime.Now;
                if (!finishDateIsNow)
                {
                    finishDate = Properties.Settings.Default.finish_date;
                }
                Connector1C core = new Connector1C(sourceFile, company);

                core.RunExport(connectionString, startDate, finishDate, company);                
            }
        }
    
}
