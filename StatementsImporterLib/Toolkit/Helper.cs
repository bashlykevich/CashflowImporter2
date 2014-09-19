using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StatementsImporterLib.Toolkit
{
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
