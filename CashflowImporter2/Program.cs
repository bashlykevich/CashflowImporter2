using CashflowImporter2;
using CommandLine;
using CommandLine.Text;
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
    class Options
    {
        // source-bs 
        [Option('b', "source-bs",
          HelpText = "Имя xml-файла с банковскими выписками 1С.")]
        public string SourceBsFile { get; set; }

        // source-mc
        [Option('m', "source-mc",
          HelpText = "Имя xml-файла с ручными операциями 1С.")]
        public string SourceMcFile { get; set; }

        // date-start
        [Option('s', "date-start", Required = true,
          HelpText = "Начала импортируемого периода.")]
        public DateTime DateStart { get; set; }

        // date-end
        [Option('e', "date-end", Required = true,
          HelpText = "Конец импортируемого периода.")]
        public DateTime DateEnd { get; set; }
        
        // ts-host
        [Option('h', "host", Required = true,
          HelpText = "Адрес сервера Террасофт.")]
        public string TsHost { get; set; }

        // ts-db
        [Option('d', "db", Required = true,
          HelpText = "Имя базы на сервере Террасофт.")]
        public string TsDatabase { get; set; }

        // ts-user
        [Option('u', "user", Required = true,
          HelpText = "Имя пользователя сервера Террасофт.")]
        public string TsUser { get; set; }
        
        // ts-psw
        [Option('p', "psw", Required = true,
          HelpText = "Пароль пользователя сервера Террасофт.")]
        public string TsPsw { get; set; }

        // company
        [Option('c', "company", Required = true,
         HelpText = "Код компании, база которой загружается (ms, za, sp, ir, nl")]
        public string Company { get; set; }

        [Option('v', "verbose", DefaultValue = true,
         HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                if (String.IsNullOrEmpty(options.SourceBsFile) && String.IsNullOrEmpty(options.SourceMcFile))
                {
                    Console.WriteLine("Должен быть указан хотя бы один файл для импорта.");
                    return;
                }
                //if( options.DateEnd == null)
                //{
                    options.DateEnd = DateTime.Now;
                //}

                Company companyToUpdate = Helper.GetCompany(options.Company);
                string connectionString = Helper.GenerateConnectionString(options.TsHost, options.TsDatabase, options.TsUser, options.TsPsw);

                Connector1Cv2 core = new Connector1Cv2(companyToUpdate);
                
                if(!String.IsNullOrEmpty(options.SourceBsFile))
                {
                    core.ImportBansStatements(options.SourceBsFile, connectionString, options.DateStart, options.DateEnd);
                }
                if(!String.IsNullOrEmpty(options.SourceMcFile))
                {
                    core.ImportManualCashflows(options.SourceBsFile, connectionString, options.DateStart, options.DateEnd);
                }
            }            
        }
    }
}
