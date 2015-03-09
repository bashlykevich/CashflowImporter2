using StatementsImporterLib.ADO;
using StatementsImporterLib.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace StatementsImporterLib.Controllers
{
    public class Connector1Cv2
    {
        private Company company;

        public Connector1Cv2(Company c)
        {
            this.company = c;
        }
        public void ImportBansStatements(string sourceXmlFile, string destDbConnectionString, DateTime startDate, DateTime endDate)
        {
            List<BankStatement> statements = ParseXmlFileBs(sourceXmlFile);
            UploadToDatabase(destDbConnectionString, statements, startDate, endDate);
        }
        public void ImportManualCashflows(string sourceXmlFile, string destDbConnectionString, DateTime startDate, DateTime endDate)
        {
            List<ManualCashflow> cashflows = ParseXmlFileMc(sourceXmlFile);

            using (StatementsImporterLib.ADO.Entities db = new Entities(destDbConnectionString))    // поднимаем подключение к БД
            {
                foreach (ManualCashflow mc in cashflows)
                {
                    string parentCashflowNumber = mc.Number.Replace("пп", "").Replace(".", "");
                    tbl_Cashflow parentCashflow = DbHelper.GetParentCashflow(parentCashflowNumber, mc.Date, db);
                    if (parentCashflow == null)
                    {
                        Console.WriteLine("{0} не найден", parentCashflowNumber);

                    }
                    else
                    {
                        tbl_Cashflow childCashflow = mc.toTsCashflow(parentCashflow, db);
                        tbl_CashflowInCashflow cic = new tbl_CashflowInCashflow
                        {
                            CreatedByID = new Guid(Constants.DefaultAdminID),
                            CreatedOn = DateTime.Now,
                            ID = Guid.NewGuid(),
                            ModifiedByID = new Guid(Constants.DefaultAdminID),
                            ModifiedOn = DateTime.Now,
                            ChildID = childCashflow.ID,
                            ParentID = parentCashflow.ID
                        };
                        Console.WriteLine("{0} ({1}): => {2}", parentCashflow.CFNumber, parentCashflow.Obj1cDocNumIn, childCashflow.Amount);
                        int childCount = db.tbl_CashflowInCashflow.Count(x => x.ParentID == parentCashflow.ID);
                        childCashflow.CFNumber += "xx" + (char)('a' + childCount);
                        db.tbl_Cashflow.Add(childCashflow);
                        DbHelper.GrantToManagerAccessToCashflow(childCashflow, db); // раздаём права на новый платёж его менеджеру    
                        db.tbl_CashflowInCashflow.Add(cic);
                        db.SaveChanges();
                    }
                }

            }
        }
        void UploadToDatabase(string connectionString, List<BankStatement> statements, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("UploadToDatabase");
            string companyID = Helper.getDs_CompanyID(company);
            using (Entities db = new Entities(connectionString))    // поднимаем подключение к БД
            {
                DateTime compareDate = startDate;
                do
                {
                    UpdateCashflowForDate(statements, compareDate, companyID, db); // для каждой даты осуществляем сравнение выгруженнных из 1С и существующих в ТС платежей
                    compareDate = compareDate.AddDays(1);
                } while (compareDate <= endDate);       //  проходим по всем датам из указанного в конфигурационном файле диапазона
            }
        }
        void UpdateCashflowForDate(List<BankStatement> statements, DateTime compareDate, string companyID, Entities db)
        {
            
            Console.Write("\n{0} \t", compareDate.ToShortDateString());

            List<Transfer> list1C = getTransfersFrom1c(statements, compareDate);                    // выбираем платежи из выписки 1С на дату
            var uids1C = list1C.Select(x => x.UID1C);

            List<tbl_Cashflow> listTs = getTransfersFromTs(compareDate, companyID, db); // выбираем платежи из ТС на дату
            var uidsTs = listTs.Select(x => x.UID1C);

            // обновим общие идентификаторы 
            var uidsToUpdate = uids1C.Intersect(uidsTs);
            int changeflag = 0;
            foreach(string uidToCompare in uidsToUpdate)
            {                
                tbl_Cashflow c1C = CreateCashflow(db, list1C.FirstOrDefault(x => x.UID1C == uidToCompare));
                tbl_Cashflow cTS = listTs.FirstOrDefault(x => x.UID1C == uidToCompare);
                // check for changes
                bool changed = false;
                // Amount                
                if(c1C.Amount != cTS.Amount)
                {                    
                    cTS.Amount = c1C.Amount;
                    changed = true;
                }
                // PayerID
                if (c1C.PayerID != cTS.PayerID)
                {
                    cTS.PayerID = c1C.PayerID;
                    changed = true;
                }
                // RecipientID
                if (c1C.RecipientID != cTS.RecipientID)
                {
                    cTS.RecipientID = c1C.RecipientID;
                    changed = true;
                }
                // ContractID
                if (c1C.ContractID != cTS.ContractID)
                {
                    cTS.ContractID = c1C.ContractID;
                    changed = true;
                }
                if (changed)
                    changeflag++;
            }
            if(changeflag > 0)
            {
                db.SaveChanges();   
            }
            Console.Write("Updated: {0}/{1} \t", changeflag, uidsToUpdate.Count());

            // удалим левые идентификаторы
            var cashflowsToDelete = listTs.Where(x => !uids1C.Contains(x.UID1C));
            if (cashflowsToDelete.Count() > 0)
            {
                Console.Write("Delete: {0} \t", cashflowsToDelete.Count());            
                db.tbl_Cashflow.RemoveRange(cashflowsToDelete);
                db.SaveChanges();
            }
            
            // добавим новые
            var cashflowsToInsert = list1C.Where(x => !uidsTs.Contains(x.UID1C));
            foreach(Transfer t in cashflowsToInsert)
            {
                tbl_Cashflow newCashflow = CreateCashflow(db, t);                

                db.tbl_Cashflow.Add(newCashflow);
                DbHelper.GrantToManagerAccessToCashflow(newCashflow, db); // раздаём права на новый платёж его менеджеру    
                db.SaveChanges();
            }
            if (cashflowsToInsert.Count() > 0)
            {
                Console.Write("Insert: {0}", cashflowsToInsert.Count());
                db.SaveChanges();
            }                       
        }
        
        
        public tbl_Cashflow CreateCashflow(Entities db, Transfer t)
        {
            tbl_Cashflow c = new tbl_Cashflow();
            
            c.UID1C = t.UID1C;

            //TMP
            c.MailSent = 1;

            c.Obj1cDocNumIn = t.НомерДокВходящий;
            c.ModifiedByID = Helper.GetSupervisorID();
            c.ModifiedOn = DateTime.Now;
            c.CreatedByID = Helper.GetSupervisorID();
            c.CreatedOn = DateTime.Now;

            string cID = Helper.getDs_CompanyID(t.Company);
            c.CompanyID = new Guid(cID);
            // 01 НОМЕР
            c.CFNumber = Helper.GetNextNumber(db);

            // 02 НАЗНАЧЕНИЕ
            c.Subject = t.НазначениеПлатежа;
            if(t.ВидДвижения != null)
            {
                if(t.ВидДвижения.Наименование.Length>0)
                c.Subject += " (" + t.ВидДвижения.Наименование + ")";
            }
            if(c.Subject.Length > 249)
            {
                c.Subject = c.Subject.Substring(0, 246) + "...";
            }

            // 03 ОТ            
            c.DocDate = DateTime.Parse(t.ДатаДок);

            // 04 ТИП
            CashflowType cashflowType;
            if (t.Приход > 0)
                cashflowType = CashflowType.Income;
            else
                cashflowType = CashflowType.Expense;
            c.TypeID = Helper.GetCashflowTypeID(cashflowType);

            // 05 СТАТЬЯ NULL
            c.ClauseID = DbHelper.GetCashflowClauseID(db, t.ВидДвижения, t.Company);

            // 06 КАТЕГОРИЯ NULL
            // 07 ОТВЕТСТВЕННЫЙ
            c.OwnerID = Helper.GetOwnerID();

            // 08 СОСТОЯНИЕ
            c.StatusID = new Guid(Constants.CashflowStateFinishedID);

            // 09 ИНЦИДЕНТ NULL
            // 10 ВОЗДЕЙСТВИЕ NULL
            // 11 PL - (UseAsPandL - P&L) NULL

            // 12 КАССА 
            c.CashAccountID = new Guid(Constants.CashflowKassaID);
            // 13 ПЛАНИРУЕМАЯ ДАТА
            c.EstimatedDate = DateTime.Parse(t.ДатаДок);

            // 14 ФАКТИЧЕСКАЯ ДАТА
            c.ActualDate = DateTime.Parse(t.ДатаДок);

            // 15 ТИП РАСХОДА-ДОХОДА NULL
            // 16 ПЕРИОД
            c.PeriodID = Helper.GetPeriodID(db, c.ActualDate.Value);

            // 00 Контрагент
            string comments = "";
            Guid? AccountID = DbHelper.GetAccountID(db, t);
            if (!AccountID.HasValue)
            {
                comments += "Контрагент не найден: " + Helper.GetAccountNameCode(t) + ".\r\n";
            }
            
            // 19 УЧИТЫВАТЬ ПРИ ВЗАИМОРАСЧЁТАХ NULL
            // 20 ДЕБИТОР-КРЕДИТОР NULL
            // 21 АВТОМАТИЧЕСКИ РАССЧИТЫВАТЬ СУММУ
            c.AutocalcAmount = 1;

            // 22 ВАЛЮТ            
            c.CurrencyID = DbHelper.ConvertToCurrencyID(t.Валюта);

            // 23 СУММА
            if (cashflowType == CashflowType.Income)
                c.Amount = (decimal)t.Приход;
            else
                c.Amount = -1 * (decimal)t.Расход;
            
            // 24 ВНУТРЕННИЙ КУРС
            c.CurrencyRate = (decimal)t.Курс;

            // 25 СУММА В БАЗОВОЙ ВАЛЮТЕ NULL
            //c.BasicAmount = (int)(c.Amount / c.CurrencyRate);
            // 26 КОНТАКТ NULL
            // 27 СЧЁТ NULL
            // 28 ДОГОВОР
            Guid? ContractID = DbHelper.GetContractID(db, t);
            if (ContractID.HasValue)
            {
                c.ContractID = ContractID;
            }
            else
            {
                string ContractName = DbHelper.GetContractName(t);
                if(!String.IsNullOrEmpty(ContractName))
                comments += "Договор не найден: " + ContractName  + ".";
            }            
            // 29 ПРОДАЖА - получаем из договора
            if (ContractID.HasValue)
            {
                c.OpportunityID = DbHelper.GetOpportunityIDFromContract(db, ContractID);
            }

            // 30 МЕНЕДЖЕР - получаем из договора
            if (ContractID.HasValue)
            {
                c.ManagerID = DbHelper.GetManagerIDFromContract(db, ContractID);
            }
            else
                if (AccountID.HasValue)
                {
                    c.ManagerID = DbHelper.GetManagerIDFromAccount(db, AccountID);
                }
                else
                {
                    c.ManagerID = DbHelper.GetDefaultManagerID();
                }

            // 31 если найден договор, но не найден контрагент
            if (ContractID.HasValue && !AccountID.HasValue)
            {
                AccountID = DbHelper.GetAccountIDFromContract(db, ContractID);
            }

            // 17 ПЛАТЕЛЬЩИК
            // 18 ПОЛУЧАТЕЛЬ
            if (cashflowType == CashflowType.Income)
            {
                c.PayerID = AccountID;
                c.RecipientID = DbHelper.GetCompanyID(t.Company);
            }
            else
            {
                c.PayerID = DbHelper.GetCompanyID(t.Company);
                c.RecipientID = AccountID;
            }

            c.DebtorCreditorID = c.PayerID;
            // если назначение содержит номер счёта
            string PayDetails = t.НазначениеПлатежа;
            string InvoiceString = "счет ";
            if (PayDetails.Contains(InvoiceString))
            {
                int NumStart = PayDetails.IndexOf(InvoiceString) + 5;
                string substr1 = PayDetails.Substring(NumStart);
                int NumEnd = substr1.IndexOf(' ');
                string InvoiceNumber = "";
                if (NumEnd >= 0)
                    InvoiceNumber = substr1.Substring(0, NumEnd);
                // найти счёт
                // фильтр по номеру счёта                
                List<tbl_Invoice> invoices = db.tbl_Invoice.Where(i => i.InvoiceNumber == InvoiceNumber).ToList();
                // фильтр по дате счёта (год)?
                invoices = invoices.Where(i => i.InvoiceDate.Year == DateTime.Now.Year).ToList();
                // фильтр по контрагенту
                if (AccountID.HasValue)
                {
                    invoices = invoices.Where(i => i.CustomerID == AccountID).ToList();
                }
                if (invoices.Count > 0)
                {
                    tbl_Invoice i = invoices.First();
                    c.InvoiceID = i.ID;
                }
            }
            if (comments.Length > 249)
            {
                comments = comments.Substring(0, 246) + "...";
            }
            c.Comments = comments;            

            //t.КоррСчёт - пока не использовать
            c.ID = Guid.NewGuid();

            return c;
        }
       
        List<Transfer> getTransfersFrom1c(List<BankStatement> statements, DateTime compareDate)
        {
            List<Transfer> list1C = new List<Transfer>();
            List<BankStatement> bsList = statements.Where(x => Helper.ParseDate(x.ДатаДок) == compareDate).ToList();
            foreach (BankStatement bs in bsList)
            {
                list1C.AddRange(bs.Transfers);
            }
            list1C = list1C.OrderBy(x => x.Приход).ToList();
            return list1C;
        }
        List<tbl_Cashflow> getTransfersFromTs(DateTime compareDate, string companyID, Entities db)
        {
            Guid ds_compID = new Guid(companyID);
            //Console.WriteLine(compareDate.ToShortDateString());
            List<tbl_Cashflow> listTs = db.tbl_Cashflow.Where(x => x.ActualDate == compareDate
                && (x.CFNumber.StartsWith("1С") || x.CFNumber.StartsWith("1C"))
                && x.CompanyID == ds_compID).OrderBy(x => x.Amount).ToList();
            return listTs;
        }
        
        List<BankStatement> ParseXmlFileBs(string filename)
        {
            List<BankStatement> statements = new List<BankStatement>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);

            XmlNode bankNode = xmlDoc.SelectSingleNode("Bank");
            foreach (XmlNode statementNode in bankNode)
            {
                BankStatement bs = new BankStatement();
                bs.ДатаДок = statementNode.Attributes["ДатаДок"].Value;
                bs.НомерДок = statementNode.Attributes["НомерДок"].Value;
                bs.Валюта = statementNode.Attributes["Валюта"].Value;
                Console.WriteLine("{0} {1} {2}", bs.ДатаДок, bs.НомерДок, bs.Валюта);
                List<Transfer> transfers = new List<Transfer>();
                foreach (XmlNode transferNode in statementNode.ChildNodes)
                {
                    Transfer t = new Transfer();

                    t.UID1C = bs.НомерДок + "#" + transferNode.Attributes["Num"].Value;
                    t.ДатаДок = bs.ДатаДок;

                    //t.КоррСчёт = ;
                    t.НазначениеПлатежа = transferNode.Attributes["НазначениеПлатежа"].Value;
                    t.Субконто1 = new Subconto
                    {
                        Код = transferNode.Attributes["КонтрагентИНН"].Value,
                        Наименование = transferNode.Attributes["КонтрагентНаименование"].Value,
                        ТипСубконто = SubcontoType.Контрагент
                    };                    
                    t.Субконто2 = new Subconto
                    {
                        Код = transferNode.Attributes["ДоговорКод"].Value,
                        Наименование = transferNode.Attributes["ДоговорНаименование"].Value,
                        ТипСубконто = SubcontoType.Договор
                    };                    
                    t.Субконто3 = new Subconto
                    {
                        Код = "",
                        Наименование = "",
                        ТипСубконто = SubcontoType.Неопределено
                    };
                    t.Приход = Double.Parse(transferNode.Attributes["Приход"].Value.Replace(".", ","));
                    t.Расход = Double.Parse(transferNode.Attributes["Расход"].Value.Replace(".", ","));
                    t.Валюта = bs.Валюта;

                    t.ВидДвижения = new CashflowClause
                    {
                        Код = transferNode.Attributes["ВидДвиженияКод"].Value,
                        Наименование = transferNode.Attributes["ВидДвиженияНаименование"].Value,
                        РазрезДеятельности = "",
                        ВидДвижения = "",
                        RowNum = transferNode.Attributes["ВидДвиженияКод"].Value
                    };

                    t.Company = this.company;
                    t.НомерДокВходящий = transferNode.Attributes["НомерДокВходящий"].Value;

                    bool IsIncome = t.Приход > t.Расход;
                    //if (t.Валюта == "BYR")
                    //{
                    //    t.Курс = 1;
                    //}
                    //else
                    {
                        t.Курс = Double.Parse(transferNode.Attributes["КурсОплаты"].Value.Replace(".", ","));
                    }
                    bs.Transfers.Add(t);
                }
                statements.Add(bs);
            }
            return statements;
        }

        List<ManualCashflow> ParseXmlFileMc(string filename)
        {
            List<ManualCashflow> cashflows = new List<ManualCashflow>();
            XmlTextReader textReader = new XmlTextReader(filename);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(textReader);
            foreach (XmlNode c in xmlDoc.GetElementsByTagName("Cashflow"))
            {
                foreach (XmlNode t in c.ChildNodes)
                {
                    ManualCashflow mc = new ManualCashflow
                    {
                        Date = c.Attributes["Date"].Value,
                        Amount = t.Attributes["Amount"].Value,
                        Number = c.Attributes["Number"].Value,
                        Contract = t.Attributes["Contract"].Value
                    };
                    cashflows.Add(mc);
                }
            }
            return cashflows;
        }
    }
}
