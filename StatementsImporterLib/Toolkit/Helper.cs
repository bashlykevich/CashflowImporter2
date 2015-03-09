using StatementsImporterLib.ADO;
using StatementsImporterLib.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StatementsImporterLib.Toolkit
{
    public enum CashflowType { Income, Expense };
    public struct ds_Company
    {
        public static string MS = "4F38D4CB-1929-47AA-A2D1-3C17C3CFA0A1";
        public static string ZA = "5CAE3EFA-CB47-457B-BE41-06618B83C4D8";
        public static string NE = "18C4B188-B55E-4AFD-A5B2-71A83398CB8A";
        public static string SP = "86D28791-2C52-4E16-8BF4-8A3E1F2EE878";
        public static string IR = "86D28791-2C52-4E16-8BF4-8A3E1F2EE878";
    }
    // ds_Company
    
    public enum Company {MS,ZA,NE,SP,IR};

    public class Helper
    {
        
        public static string GetAccountNameCode(Transfer t)
        {
            string name = "";
            Subconto Account = t.Субконто1;
            if (Account != null)
                name = Account.Наименование + " (" + Account.Код + ")";
            return name;
        }
        
        public static Guid? GetPeriodID(Entities db, DateTime date)
        {
            tbl_Period p = db.tbl_Period.FirstOrDefault(x => x.StartDate <= date && date <= x.DueDate);
            if (p == null)
                return null;
            else
                return p.ID;
        }
        public static string GetCashflowTypeID(CashflowType type)
        {
            if (type == CashflowType.Income)
                return Constants.CashflowTypeIncomeID;
            else
                return Constants.CashflowTypeExpenseID;
        }
        static int cashflowCount = 0;
        public static string GetNextNumber(Entities db)
        {
            string num = "1С" + (db.tbl_Cashflow.Count() + cashflowCount++ + 1).ToString();
            return num;
        }
        public static Guid? GetOwnerID()
        {
            return new Guid(Constants.CashflowOwnerID);
        }

        public static Guid? GetSupervisorID()
        {
            return new Guid("251FB9AC-C17E-4DF7-A0CB-D591FDB97462");
        }
        public static DateTime ParseDate(string date)
        {
            DateTime dt = DateTime.Now;
            DateTime.TryParse(date, out dt);
            return dt;
        }
        public static string getDs_CompanyID(Company c)
        {
            string id = ds_Company.MS;
            switch (c)
            {
                case Company.NE:
                    id = ds_Company.NE;
                    break;
                case Company.SP:
                    id = ds_Company.SP;
                    break;
                case Company.ZA:
                    id = ds_Company.ZA;
                    break;
            }
            return id;
        }
        public static string ParseContractNumber(string ContractName)
        {
            string Number = "";
            int start = ContractName.IndexOf("№");
            if (start < 0)
                return "";
            Number = ContractName.Substring(start + 1);
            int end = Number.IndexOf(" ");
            if (end < 0)
                return "";
            Number = Number.Substring(0, end + 1);
            return Number;
        }
        public static string ParseContractCode(string ContractCode)
        {
            return ContractCode.Replace("№", "");
        }
        public static void PrintInfo()
        {
            Console.WriteLine(" Без параметров  Запуск с параметрами по умолчанию (смотри ниже)");
            Console.WriteLine(" /?              Справка");
            Console.WriteLine(" /def            Запуск с параметрами по умолчанию: /dbf ddMMyy.dbf /server 192.168.5.12 /db TSXRM3 /user Supervisor /psw 123 /comp MS");
            Console.WriteLine(" /dbf %FILE%     Файл .dbf с выписками");
            Console.WriteLine(" /server %SRV%   Имя сервера MS SQL");
            Console.WriteLine(" /db %DBNAME%    Имя БД Terrasoft XRM");
            Console.WriteLine(" /user %DBUSER%  Логин пользователя TS");
            Console.WriteLine(" /psw  %PSW%     Пароль пользователя TS");
            Console.WriteLine(" /comp  %COMP%   Компания (MS/ZA/NE/SP)");
        }
        public static string CalculateMD5Hash(string[] input)
        {
            string h = "";
            foreach (string s in input)
                h += s;
            return CalculateMD5Hash(h);
        }
        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
        public static string GenerateConnectionString(string host, string db, string user, string psw)
        {
                                    // metadata=res://*/ADO.TsDatabase.csdl|res://*/ADO.TsDatabase.ssdl|res://*/ADO.TsDatabase.msl;provider=System.Data.SqlClient;provider connection string='data source=
            string connectionstring = "metadata=res://*/ADO.TsDatabase.csdl|res://*/ADO.TsDatabase.ssdl|res://*/ADO.TsDatabase.msl;provider=System.Data.SqlClient;provider connection string='Data Source="
                        + host
                        // ;initial catalog=
                        + ";Initial Catalog="
                        + db
                        // ;persist security info=True;user id=
                        + ";Persist Security Info=True;User ID=\""
                        + user
                        //   ;password=
                        + "\";Password="
                        + psw
                        // ;MultipleActiveResultSets=True;App=EntityFramework'
                        + ";MultipleActiveResultSets=True;App=EntityFramework'";
            return connectionstring;
        }
        public static void Log(string text)
        {
            string LogFile = "log.txt";
            string str = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss") + " " + text + "\n";
            System.IO.File.AppendAllText(LogFile, str);
        }

        public static Company GetCompany(string comp)
        {
            string str = comp.ToLower();
            switch (str)
            {
                case "ms":
                    return Company.MS;
                case "ir":
                    return Company.IR;
                case "za":
                    return Company.ZA;
                case "ne":
                    return Company.NE;
                case "nl":
                    return Company.NE;
                case "sp":
                    return Company.SP;
                default:
                    return Company.MS;
            }
        }
    }
}
