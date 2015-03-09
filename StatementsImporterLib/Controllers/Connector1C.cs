using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.IO;
using StatementsImporterLib.ADO;
using StatementsImporterLib.Toolkit;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Xml;

namespace StatementsImporterLib.Controllers
{
    public class Connector1C
    {
        private List<DataRow> ImportedRows = new List<DataRow>();

        #region public methods
        private bool DEBUG;
        private Company company;
        public Connector1C(Company c, bool enableDebug = false)
        {
            string logFileName = DateTime.Now.ToString().Replace(":", "-");
            importLogFile = new StreamWriter(logFileName + ".txt");
            this.DEBUG = enableDebug;
            this.company = c;
        }
        public void LoadFromDbf(string filename)
        {
            Helper.Log("Старт");
            OdbcConnection conn = new OdbcConnection();
            conn.ConnectionString = "Driver={Microsoft dBase Driver (*.dbf)};SourceType=DBF;SourceDB=" + filename + ";Exclusive=No; NULL=NO;DELETED=NO;BACKGROUNDFETCH=NO;";
            conn.Open();
            if (conn.State == System.Data.ConnectionState.Open)
            {
                OdbcCommand oCmd = conn.CreateCommand();
                oCmd.CommandText = "SELECT * FROM " + filename;
                DataTable ImportedTable = new DataTable();
                ImportedTable.Load(oCmd.ExecuteReader());
                ImportedRows.AddRange(ImportedTable.Select());

                DataRow[] statementRows = ImportedTable.Select("REKVTYPE = 'Документ.Выписка' AND REKVIZIT <> 'Субконто3'");

                for (int i = 0; i < statementRows.Length; i++)
                {
                    DataRow row = statementRows[i];

                    double StatementStartRowIndex = (double)row[0];
                    double StatementEndRowIndex = GetStatementEndIndex(StatementStartRowIndex);

                    BankStatement bs = new BankStatement();
                    bs.GUID = (double)row[0];
                    try
                    {
                        // ШАПКА
                        double BaseGUID = bs.GUID - 1;
                        bs.Наименование = row["REKVIZIT"].ToString();

                        Helper.Log(bs.Наименование);

                        DataRow rowID = ImportedRows[(int)(BaseGUID + 2)];
                        bs.НомерДок = rowID[5].ToString();
                        DataRow rowDate = ImportedRows[(int)(BaseGUID + 3)];
                        bs.ДатаДок = rowDate[5].ToString();
                        // ТАБЛИЧНАЯ ЧАСТЬ
                        List<Transfer> transfers = new List<Transfer>();
                        List<DataRow> TransferRows = new List<DataRow>();
                        string cond = "PARENTGUID = " + bs.GUID
                                        + " AND REKVIZIT = 'ВидДвижения'"
                                        + " AND REKVTYPE = 'Справочник.ДвиженияДенежныхСредств'";
                        TransferRows.AddRange(ImportedTable.Select(cond));
                        Console.WriteLine(bs.ДатаДок);

                        string Currency = GetStatementCurrency(StatementStartRowIndex, StatementEndRowIndex);

                        for (int j = 0; j < TransferRows.Count; j++)
                        {
                            DataRow transferBaseRow = TransferRows[j];

                            double TransferStartRowIndex = (double)transferBaseRow[0];
                            double TransferEndRowIndex = GetTransferEndIndex(TransferStartRowIndex);

                            int transferBaseGUID = (int)(double)transferBaseRow[0] - 1;
                            try
                            {
                                Transfer t = new Transfer();

                                t.КоррСчёт = ImportedRows[transferBaseGUID + 2][5].ToString();
                                t.НазначениеПлатежа = ImportedRows[transferBaseGUID + 1][5].ToString();
                                t.Субконто1 = GetSubconto(1, transferBaseGUID);
                                t.Субконто2 = GetSubconto(2, transferBaseGUID);
                                t.Субконто3 = GetSubconto(3, transferBaseGUID);
                                t.Приход = GetTransferAttributeDouble("Приход", transferBaseGUID);
                                t.Расход = GetTransferAttributeDouble("Расход", transferBaseGUID);
                                t.Валюта = Currency;

                                t.ВидДвижения = GetTransferCashflowAccount(transferBaseGUID);

                                t.Company = this.company;
                                t.НомерДокВходящий = GetTransferAttribute("НомерДокВходящий", transferBaseGUID);
                                if (bs.Наименование.Contains('т') && bs.Наименование.Contains('д'))
                                {
                                }
                                bool IsIncome = t.Приход > t.Расход;
                                if (t.Валюта == "BYR")
                                {
                                    t.Курс = 1;
                                }
                                else
                                {
                                    t.Курс = GetTransferRate(IsIncome, StatementStartRowIndex, StatementEndRowIndex, TransferStartRowIndex, TransferEndRowIndex);
                                }                               
                                bs.Transfers.Add(t);
                            }
                            catch (Exception e)
                            {
                                Helper.Log("ID=" + transferBaseGUID.ToString() + ": " + e.Message);
                            }
                        }
                        statements.Add(bs);
                        Helper.Log("Платежей в выписке: " + bs.Transfers.Count);
                    }
                    catch (Exception e)
                    {
                        Helper.Log(e.Message);
                    }
                }
                conn.Close();
                Helper.Log("Импорт из DBF завершен.");
                if (this.DEBUG)
                {
                    Console.WriteLine("Found statements: {0}\n", this.statements.Count);
                }
            }
        }
        public void LoadFromXml(string filename, string connectionString, DateTime startDate, DateTime endDate)
        {
            this.ParseXmlFile(filename);
            this.UploadXmlDataToDatabase(connectionString, startDate, endDate);
            
        }
        private void UploadXmlDataToDatabase(string connectionString, DateTime startDate, DateTime endDate)
        {
            string companyID = getDs_CompanyID(company);
            using (Entities db = new Entities(connectionString))    // поднимаем подключение к БД
            {
                DateTime compareDate = startDate;
                do
                {
                    UpdateCashflowForDate(compareDate, companyID, db); // для каждой даты осуществляем сравнение выгруженнных из 1С и существующих в ТС платежей
                    compareDate = compareDate.AddDays(1);
                } while (compareDate <= endDate);       //  проходим по всем датам из указанного в конфигурационном файле диапазона
            }
        }
        public void ParseXmlFile(string filename)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);

            XmlNode bankNode = xmlDoc.SelectSingleNode("Bank");
            foreach(XmlNode statementNode in bankNode)
            {
                BankStatement bs = new BankStatement();
                bs.ДатаДок = statementNode.Attributes["ДатаДок"].Value;
                bs.НомерДок = statementNode.Attributes["НомерДок"].Value;
                bs.Валюта = statementNode.Attributes["Валюта"].Value;
                Console.WriteLine("{0} {1} {2}", bs.ДатаДок, bs.НомерДок,bs.Валюта);
                List<Transfer> transfers = new List<Transfer>();
                foreach(XmlNode transferNode in statementNode.ChildNodes)
                {
                    Transfer t = new Transfer();
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
                    t.Приход = Double.Parse(transferNode.Attributes["Приход"].Value.Replace(".",","));
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
                    if (t.Валюта == "BYR")
                    {
                        t.Курс = 1;
                    }
                    else
                    {
                        t.Курс = Double.Parse(transferNode.Attributes["КурсОплаты"].Value.Replace(".", ","));
                    }
                    bs.Transfers.Add(t);
                }
                statements.Add(bs);
            }            
        }

        public List<BankStatement> BankStatements
        {
            get { return statements; }
        }
        string getDs_CompanyID(Company c)
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
        List<Transfer> getTransfersFrom1c(DateTime compareDate)
        {
            List<Transfer> list1C = new List<Transfer>();
            List<BankStatement> bsList = BankStatements.Where(x => GetDateTime(x.ДатаДок) == compareDate).ToList();
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
        void Update1cDocNumIns(Entities db, List<Transfer> list1C, List<tbl_Cashflow> lts, DateTime compareDate)
        {
            List<CashflowComparer> hashes = new List<CashflowComparer>();
            List<tbl_Cashflow> listTs = lts.Where(x => String.IsNullOrEmpty(x.Obj1cDocNumIn)).ToList();
            foreach (tbl_Cashflow c in listTs)
            {
                string h = CashflowComparer.getHashWithoutDocNum(c);
                if (!hashes.Exists(x => x.hash == h))
                {
                    CashflowComparer cc = new CashflowComparer()
                    {
                        hash = h,
                        objTs = c
                    };
                    hashes.Add(cc);
                }
            }
            foreach (Transfer t in list1C)
            {
                tbl_Cashflow c = CreateCashflow(db, t, compareDate);
                string h = CashflowComparer.getHashWithoutDocNum(c);
                if (hashes.Exists(x => x.hash == h))
                {
                    hashes.FirstOrDefault(x => x.hash == h).obj1C = c;
                }
                else
                {
                    CashflowComparer cc = new CashflowComparer()
                    {
                        hash = h,
                        obj1C = c
                    };
                    hashes.Add(cc);
                }
            }
            foreach (CashflowComparer cc in hashes)
            {
                if (cc.Action == ImportAction.UPDATE_1CDOCNUMIN)
                {
                    //Console.WriteLine("Update 1C DocNumIn");
                    db.tbl_Cashflow.FirstOrDefault(x => x.ID == cc.objTs.ID).Obj1cDocNumIn = cc.obj1C.Obj1cDocNumIn;
                }
            }
            db.SaveChanges();
        }
        StreamWriter importLogFile;

        public void RunExport(string connectionString, DateTime startDate, DateTime endDate, Company company)
        {
            string companyID = getDs_CompanyID(company);
            using (Entities db = new Entities(connectionString))    // поднимаем подключение к БД
            {
                DateTime compareDate = startDate;
                do
                {                    
                    UpdateCashflowForDate(compareDate, companyID, db); // для каждой даты осуществляем сравнение выгруженнных из 1С и существующих в ТС платежей
                    compareDate = compareDate.AddDays(1);
                } while (compareDate <= endDate);       //  проходим по всем датам из указанного в конфигурационном файле диапазона
            }
        }
        List<CashflowComparer> FormHashesList(List<tbl_Cashflow> listTs, List<Transfer> list1C, DateTime compareDate, Entities db)
        {
            List<CashflowComparer> hashes = new List<CashflowComparer>();
            foreach (tbl_Cashflow t in listTs)
            {
                string h = CashflowComparer.getHash(t);
                CashflowComparer cc = new CashflowComparer()
                {
                    hash = h,
                    objTs = t
                };
                hashes.Add(cc);
            }
            foreach (Transfer t in list1C)
            {
                tbl_Cashflow c = CreateCashflow(db, t, compareDate);
                string h = CashflowComparer.getHash(c);
                if (hashes.Exists(x => x.hash == h))
                {
                    hashes.FirstOrDefault(x => x.hash == h).obj1C = c;
                }
                else
                {
                    CashflowComparer cc = new CashflowComparer()
                    {
                        hash = h,
                        obj1C = c
                    };
                    hashes.Add(cc);
                }
            }
            return hashes;
        }
        void WriteHashesDiffToDb(List<CashflowComparer> hashes, Entities db)
        {
            foreach (CashflowComparer cc in hashes)
            {
                switch (cc.Action)
                {
                    case ImportAction.DELETE_FROM_TS:   //Если хэш из набора-ТС отсутствует в наборе-1С - удаляем этот платёж из ТС                                                                                                    
                        db.tbl_Cashflow.Remove(cc.objTs);
                        cc.PrintCompareResultInfo();
                        db.SaveChanges();
                        break;
                    case ImportAction.ADD_TO_TS:        //Если хэш из набора-1с отсутствует в наборе-ТС - импортируем этот платёж из 1С в ТС                            
                        db.tbl_Cashflow.Add(cc.obj1C);
                        GrantToManagerAccessToCashflow(cc, db); // раздаём права на новый платёж его менеджеру    
                        cc.PrintCompareResultInfo();
                        db.SaveChanges();
                        break;                    
                }
            }
        }
        void GrantToManagerAccessToCashflow(CashflowComparer cc, Entities db)
        {
            if (cc.obj1C.ManagerID.HasValue) 
            {
                if (db.tbl_CashflowRight.Count(x => x.RecordID == cc.obj1C.ID && x.AdminUnitID == cc.obj1C.ManagerID) == 0)
                {
                    Guid managerAdminUnitID = db.tbl_AdminUnit.FirstOrDefault(x => x.UserContactID == cc.obj1C.ManagerID).ID;
                    tbl_CashflowRight rights = new tbl_CashflowRight
                    {
                        AdminUnitID = managerAdminUnitID,
                        CanChangeAccess = 1,
                        CanDelete = 0,
                        CanRead = 1,
                        CanWrite = 1,
                        ID = Guid.NewGuid(),
                        RecordID = cc.obj1C.ID
                    };
                    db.tbl_CashflowRight.Add(rights);
                }
            }
        }
        void UpdateCashflowForDate(DateTime compareDate, string companyID, Entities db)
        {
            Console.WriteLine(compareDate.ToShortDateString());

            List<Transfer> list1C = getTransfersFrom1c(compareDate);                    // выбираем платежи из выписки 1С на дату
            List<tbl_Cashflow> listTs = getTransfersFromTs(compareDate, companyID, db); // выбираем платежи из ТС на дату
            Update1cDocNumIns(db, list1C, listTs, compareDate);                         // обновить номераДокВходящих для уже импортированных записей // НЕ ПОМНЮ, ЗАЧЕМ ЭТО 
            List<CashflowComparer> hashes = FormHashesList(listTs, list1C, compareDate, db);    // формируем список хэшей - сравнений платежей из 1С и ТС
            WriteHashesDiffToDb(hashes, db);        // актулизировать список по хэшам            
        }
        
        public void ExportToServer(string connectionstring)
        {

            using (Entities db = new Entities(connectionstring))
            {
                Helper.Log("Соединение с базой установлено.");
                foreach (BankStatement statement in BankStatements)
                {
                    foreach (Transfer transfer in statement.Transfers)
                    {
                        CommitTransferToDatabase(db, transfer, statement.ДатаДок);
                    }
                }
                Helper.Log("Экспорт в БД Terrasoft завершён.\n");
            }
        }
        public void ExportToLocalList(string connectionstring)
        {

            using (Entities db = new Entities(connectionstring))
            {
                Helper.Log("Соединение с базой установлено.");
                foreach (BankStatement statement in BankStatements)
                {
                    foreach (Transfer transfer in statement.Transfers)
                    {
                        CommitTransferToLocalList(db, transfer, statement);
                    }
                }
                Helper.Log("Экспорт в БД Terrasoft завершён.\n");
            }
        }
        public void ExportToLocalListV2(string connectionstring)
        {

            using (Entities db = new Entities(connectionstring))
            {
                Helper.Log("Соединение с базой установлено.");
                foreach (BankStatement statement in BankStatements)
                {
                    foreach (Transfer transfer in statement.Transfers)
                    {
                        CommitTransferToLocalList(db, transfer, statement);
                    }
                }
                Helper.Log("Экспорт в БД Terrasoft завершён.\n");
            }
        }
        #endregion public methods

        #region IMPORT
        double GetStatementEndIndex(double StatementStartRowIndex)
        {
            double StatementEndRowIndex = 0;
            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > StatementStartRowIndex).ToList();
            DataRow NextRow = rows.FirstOrDefault(r => r[3].ToString() == "Документ.Выписка");
            if (NextRow != null)
            {
                StatementEndRowIndex = (double)NextRow[0] - 1;
            }
            return StatementEndRowIndex;
        }
        double GetTransferEndIndex(double TransferStartRowIndex)
        {
            double TransferEndRowIndex = 0;
            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > TransferStartRowIndex).ToList();
            DataRow NextStatement = rows.FirstOrDefault(r => r[3].ToString() == "Документ.Выписка");
            DataRow NextTransfer = rows.FirstOrDefault(r => r[3].ToString() == "Справочник.ДвиженияДенежныхСредств"
                                                                && r[2].ToString() == "ВидДвижения");
            if (NextTransfer != null && NextStatement != null)
            {
                TransferEndRowIndex = ((double)NextTransfer[0] < (double)NextStatement[0] ? (double)NextTransfer[0] : (double)NextStatement[0]) - 1;
            }
            else
                if (NextTransfer != null && NextStatement == null)
                {
                    TransferEndRowIndex = (double)NextTransfer[0] - 1;
                }
                else
                    if (NextTransfer == null && NextStatement != null)
                    {
                        TransferEndRowIndex = (double)NextStatement[0] - 1;
                    }
            return TransferEndRowIndex;
        }
        private Subconto GetSubconto(int subcontoIndex, double transferBaseGUID)
        {
            Subconto res = new Subconto();

            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > transferBaseGUID).ToList();
            DataRow subconto = rows.FirstOrDefault(r => r[2].ToString() == "Субконто" + subcontoIndex.ToString());
            double s1Guid = 0;
            Double.TryParse(subconto[5].ToString(), out s1Guid);
            if (s1Guid == 0)
                return res;

            string s1Type = subconto[3].ToString();
            switch (s1Type)
            {
                case "Документ.Выписка":
                    res.ТипСубконто = SubcontoType.Выписка;
                    res.Код = ImportedRows.FirstOrDefault(r => (double)r[1] == s1Guid && r[2].ToString() == "НомерДок")[5].ToString();
                    res.Наименование = ImportedRows.FirstOrDefault(r => (double)r[0] == s1Guid)[2].ToString();
                    break;

                case "Справочник.Договоры":
                    res.ТипСубконто = SubcontoType.Договор;
                    res.Код = ImportedRows.FirstOrDefault(r => (double)r[1] == s1Guid && r[2].ToString() == "Код")[5].ToString();
                    res.Наименование = ImportedRows.FirstOrDefault(r => (double)r[1] == s1Guid && r[2].ToString() == "Наименование")[5].ToString();
                    break;

                case "Справочник.Контрагенты":
                    res.ТипСубконто = SubcontoType.Контрагент;
                    res.Код = ImportedRows.FirstOrDefault(r => (double)r[1] == s1Guid && r[2].ToString() == "Код")[5].ToString();
                    res.Наименование = ImportedRows.FirstOrDefault(r => (double)r[1] == s1Guid && r[2].ToString() == "Наименование")[5].ToString();
                    break;
            }
            return res;
        }

        private Subconto GetSubconto(Transfer t, SubcontoType type)
        {
            if (t.Субконто1.ТипСубконто == type)
                return t.Субконто1;
            if (t.Субконто2.ТипСубконто == type)
                return t.Субконто2;
            if (t.Субконто3.ТипСубконто == type)
                return t.Субконто3;
            return null;
        }

        private Double GetTransferAttributeDouble(string AttributeName, double transferBaseGUID)
        {
            Double res = 0;
            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > transferBaseGUID).ToList();
            DataRow subconto = rows.FirstOrDefault(r => r[2].ToString() == AttributeName);
            string amount = subconto[5].ToString().Replace(".",",");
            Double.TryParse(amount, out res);
            return res;
        }
        private double GetTransferRate(bool IsIncome, double StatementStartRowIndex, double StatementEndRowIndex, double TransferStartRowIndex, double TransferEndRowIndex)
        {
            double rate = 1;

            string KPP = "КурсПокупкиПродажи";
            string KO = "КурсОплаты";

            if (IsIncome)
            {
                // если приход - ищем в текущем Transfer КурсПокупкиПодажи
                List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > TransferStartRowIndex
                                                            && (double)r[0] < TransferEndRowIndex).ToList();
                if (rows.Count > 0)
                {
                    List<DataRow> rows1 = rows.Where(r => r[2].ToString() == KPP && (Double.TryParse((r[5].ToString()), out rate))).ToList();
                    if (rows1.Count > 0)
                    {
                        DataRow raterow = rows1.FirstOrDefault(r => double.Parse(r[5].ToString()) > 0);
                        if (raterow != null)
                            Double.TryParse(raterow[5].ToString(), out rate);
                    }
                }
                if (rate == 0)
                {
                    // если не найдено - ищем КурсПокупкиПодажи с начала текушего Statement
                    rows = ImportedRows.Where(r => (double)r[0] > StatementStartRowIndex && (double)r[0] < StatementEndRowIndex).ToList();
                    if (rows.Count > 0)
                    {
                        List<DataRow> rows1 = rows.Where(r => r[2].ToString() == KPP && (Double.TryParse((r[5].ToString()), out rate))).ToList();
                        if (rows1.Count > 0)
                        {
                            DataRow raterow = rows1.FirstOrDefault(r => double.Parse(r[5].ToString()) > 0);
                            if (raterow != null)
                                Double.TryParse(raterow[5].ToString(), out rate);
                        }
                    }
                }
            }
            else
            {
                // если расход - ищем в текущем Transfer КурсОплаты
                List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > TransferStartRowIndex
                                                            && (double)r[0] < TransferEndRowIndex).ToList();
                if (rows.Count > 0)
                {
                    List<DataRow> rows1 = rows.Where(r => r[2].ToString() == KO && (Double.TryParse((r[5].ToString()), out rate))).ToList();
                    if (rows1.Count > 0)
                    {
                        DataRow raterow = rows1.FirstOrDefault(r => double.Parse(r[5].ToString()) > 0);
                        if (raterow != null)
                            Double.TryParse(raterow[5].ToString(), out rate);
                    }
                }
                if (rate == 0)
                {
                    // если не найдено - ищем КурсОплаты с начала текушего Statement
                    rows = ImportedRows.Where(r => (double)r[0] > StatementStartRowIndex && (double)r[0] < StatementEndRowIndex).ToList();
                    if (rows.Count > 0)
                    {
                        List<DataRow> rows1 = rows.Where(r => r[2].ToString() == KO && (Double.TryParse((r[5].ToString()), out rate))).ToList();
                        if (rows1.Count > 0)
                        {
                            DataRow raterow = rows1.FirstOrDefault(r => double.Parse(r[5].ToString()) > 0);
                            if (raterow != null)
                                Double.TryParse(raterow[5].ToString(), out rate);
                        }
                    }
                    if (rate == 0)
                    {
                        // если не найдено - ищем КурсПокупкиПодажи с начала текушего Statement
                        rows = ImportedRows.Where(r => (double)r[0] > StatementStartRowIndex && (double)r[0] < StatementEndRowIndex).ToList();
                        if (rows.Count > 0)
                        {
                            List<DataRow> rows1 = rows.Where(r => r[2].ToString() == KPP && (Double.TryParse((r[5].ToString()), out rate))).ToList();
                            if (rows1.Count > 0)
                            {
                                DataRow raterow = rows1.FirstOrDefault(r => double.Parse(r[5].ToString()) > 0);
                                if (raterow != null)
                                    Double.TryParse(raterow[5].ToString(), out rate);
                            }
                        }
                    }
                }
            }
            return rate;
        }

        private string GetStatementCurrency(double StatementStartRowIndex, double StatementEndRowIndex)
        {
            string AttributeName = "Валюта";
            string res = "BYR";
            Double AttributeGuid = 0;
            List<DataRow> rows;
            if (StatementEndRowIndex > 0)
            {
                rows = ImportedRows.Where(r => (double)r[0] > StatementStartRowIndex && (double)r[0] < StatementEndRowIndex).ToList();
            }
            else
            {
                rows = ImportedRows.Where(r => (double)r[0] > StatementStartRowIndex).ToList();
            }
            DataRow AttributeField = rows.FirstOrDefault(r => r[2].ToString() == AttributeName);
            if (AttributeField == null)
                return res;
            Double.TryParse(AttributeField[5].ToString(), out AttributeGuid);
            res = ImportedRows.FirstOrDefault(r => (double)r[0] == AttributeGuid)[2].ToString();
            return res;
        }
        //НомерДокВходящий
        private string GetTransferAttribute(string AttributeName, double transferBaseGUID)
        {
            string res = "";
            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > transferBaseGUID).ToList();
            DataRow AttributeField = rows.FirstOrDefault(r => r[2].ToString() == AttributeName);
            if (AttributeField == null)
                return res;
            res = AttributeField[5].ToString();
            return res;
        }
        List<CashflowClause> clausesInDbf;

        private List<CashflowClause> GetCashflowClause1cList()
        {
            this.clausesInDbf = new List<CashflowClause>();
            List<CashflowClause> cashflows = new List<CashflowClause>();
            string cashflowClauseAttributeName = "Справочник.ДвиженияДенежныхСредств";

            List<DataRow> cashflowHeaderRows = this.ImportedRows.Where(r => r[3].ToString() == cashflowClauseAttributeName && r[1].ToString() == "0").ToList();
            foreach (DataRow cashflowHeaderRow in cashflowHeaderRows)
            {
                Double cashflowClauseCode = 0;
                Double.TryParse(cashflowHeaderRow[0].ToString(), out cashflowClauseCode);

                CashflowClause clause = GetCashflowClauseDetails(cashflowClauseCode);
                this.clausesInDbf.Add(clause);
            }

            return cashflows;
        }

        private CashflowClause GetTransferCashflowAccount(double transferBaseGUID)
        {
            if (this.clausesInDbf == null)
            {
                this.GetCashflowClause1cList();
            }

            string cashflowClauseAttributeName = "ВидДвижения";

            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > transferBaseGUID).ToList();
            DataRow AttributeField = rows.FirstOrDefault(r => r[2].ToString() == cashflowClauseAttributeName);
            if (AttributeField == null)
                return null;
            string cashflowClauseCode = AttributeField[5].ToString();

            return this.clausesInDbf.FirstOrDefault(x => x.RowNum == cashflowClauseCode);
        }
        private CashflowClause GetCashflowClauseDetails(double cashflowClauseCode)
        {
            CashflowClause res = new CashflowClause();

            res.RowNum = cashflowClauseCode.ToString();
            res.Код = ImportedRows.FirstOrDefault(r => (double)r[1] == cashflowClauseCode && r[2].ToString() == "Код")[5].ToString().Trim();
            res.Наименование = ImportedRows.FirstOrDefault(r => (double)r[1] == cashflowClauseCode && r[2].ToString() == "Наименование")[5].ToString().Trim();
            res.ВидДвижения = ImportedRows.FirstOrDefault(r => (double)r[1] == cashflowClauseCode && r[2].ToString() == "ВидДвижения")[5].ToString().Trim();
            res.РазрезДеятельности = ImportedRows.FirstOrDefault(r => (double)r[1] == cashflowClauseCode && r[2].ToString() == "РазрезДеятельности")[5].ToString().Trim();

            return res;
        }
        private string GetTransferAttributeName(string AttributeName, double transferBaseGUID)
        {
            string res = "";
            Double AttributeGuid = 0;
            List<DataRow> rows = ImportedRows.Where(r => (double)r[0] > transferBaseGUID).ToList();
            DataRow AttributeField = rows.FirstOrDefault(r => r[2].ToString() == AttributeName);
            if (AttributeField == null)
                return res;
            Double.TryParse(AttributeField[5].ToString(), out AttributeGuid);
            res = ImportedRows.FirstOrDefault(r => (double)r[0] == AttributeGuid)[2].ToString();
            return res;
        }
        
        #endregion

        
        private List<BankStatement> statements = new List<BankStatement>();

        #region private methods

        private void CommitTransferToLocalList(Entities db, Transfer t, BankStatement statement)
        {
            tbl_Cashflow c = CreateCashflow(db, t, statement.ДатаДок);
            statement.Cashflows.Add(c);
        }
        public tbl_Cashflow CreateCashflow(Entities db, Transfer t, string ДатаДок)
        {
            tbl_Cashflow c = new tbl_Cashflow();

            c.ModifiedByID = Helper.GetSupervisorID();
            c.ModifiedOn = DateTime.Now;
            c.CreatedByID = Helper.GetSupervisorID();
            c.CreatedOn = DateTime.Now;

            string cID = getDs_CompanyID(t.Company);
            c.CompanyID = new Guid(cID);
            // 01 НОМЕР
            c.CFNumber = GetNextNumber(db);

            // 02 НАЗНАЧЕНИЕ
            c.Subject = t.ВидДвижения + ": " + t.НазначениеПлатежа;

            // 03 ОТ
            DateTime DocDate = GetDateTime(ДатаДок);
            c.DocDate = DocDate;

            // 04 ТИП
            CashflowType cashflowType;
            if (t.Приход > 0)
                cashflowType = CashflowType.Income;
            else
                cashflowType = CashflowType.Expense;
            c.TypeID = GetCashflowTypeID(cashflowType);

            // 05 СТАТЬЯ NULL
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
            c.EstimatedDate = GetDateTime(ДатаДок);

            // 14 ФАКТИЧЕСКАЯ ДАТА
            c.ActualDate = GetDateTime(ДатаДок);

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
                c.Amount = (decimal)t.Расход;

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
                comments += "Договор не найден: " + DbHelper.GetContractName(t) + ".";
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
            c.Comments = comments;

            //t.КоррСчёт - пока не использовать
            c.ID = Guid.NewGuid();

            return c;
        }

        public tbl_Cashflow CreateCashflow(Entities db, Transfer t, DateTime docDate)
        {
            tbl_Cashflow c = new tbl_Cashflow();

            //TMP
            c.MailSent = 1;

            c.Obj1cDocNumIn = t.НомерДокВходящий;
            c.ModifiedByID = Helper.GetSupervisorID();
            c.ModifiedOn = DateTime.Now;
            c.CreatedByID = Helper.GetSupervisorID();
            c.CreatedOn = DateTime.Now;

            string cID = getDs_CompanyID(t.Company);
            c.CompanyID = new Guid(cID);
            // 01 НОМЕР
            c.CFNumber = GetNextNumber(db);

            // 02 НАЗНАЧЕНИЕ
            c.Subject = t.ВидДвижения.Наименование + ": " + t.НазначениеПлатежа;

            // 03 ОТ            
            c.DocDate = docDate;

            // 04 ТИП
            CashflowType cashflowType;
            if (t.Приход > 0)
                cashflowType = CashflowType.Income;
            else
                cashflowType = CashflowType.Expense;
            c.TypeID = GetCashflowTypeID(cashflowType);

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
            c.EstimatedDate = docDate;

            // 14 ФАКТИЧЕСКАЯ ДАТА
            c.ActualDate = docDate;

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
                comments += "Договор не найден: " + DbHelper.GetContractName(t) + ".";
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
            c.Comments = comments;

            //t.КоррСчёт - пока не использовать
            c.ID = Guid.NewGuid();

            return c;
        }
        private void CommitTransferToDatabase(Entities db, Transfer t, string docDate)
        {
            tbl_Cashflow c = CreateCashflow(db, t, docDate);
            db.tbl_Cashflow.Add(c);
            db.SaveChanges();
        }

        

        
        private string GetCashflowTypeID(CashflowType type)
        {
            if (type == CashflowType.Income)
                return Constants.CashflowTypeIncomeID;
            else
                return Constants.CashflowTypeExpenseID;
        }
        
        
        
        ///delete public int ExtraFound = 0;
        

       

        private DateTime GetDateTime(string date)
        {
            DateTime dt = DateTime.Now;
            DateTime.TryParse(date, out dt);
            return dt;
        }

        private Guid? GetFinishedCashflowState()
        {
            return new Guid(Constants.CashflowStateFinishedID);
        }
        

        
        int cashflowCount = 0;
        private string GetNextNumber(Entities db)
        {
            string num = "1С" + (db.tbl_Cashflow.Count() + this.cashflowCount++ + 1).ToString();
            return num;
        }

        

        

        

        #endregion private methods
    }
    enum ImportAction
    {
        NO_ACTION, NO_ACTION_FOR_NULLS, UPDATE_ALL, UPDATE_AMOUNT, UPDATE_PAYER,
        UPDATE_RECEIVER
            , UPDATE_SUBJECT, ADD_TO_TS, DELETE_FROM_TS, UPDATE_1CDOCNUMIN
    }
    class CashflowComparer
    {
        public static string getHashWithoutDocNum(tbl_Cashflow c)
        {
            string[] keys = { c.Amount.ToString(), c.PayerID.ToString(), c.RecipientID.ToString() };
            return Helper.CalculateMD5Hash(keys);
        }
        public static string getHash(tbl_Cashflow c)
        {
            string[] keys = { ((long)c.Amount).ToString(), c.PayerID.ToString(), c.RecipientID.ToString() /*, c.Obj1cDocNumIn*/ };
            return Helper.CalculateMD5Hash(keys);
        }
        public string hash { get; set; }
        public tbl_Cashflow obj1C { get; set; }
        public tbl_Cashflow objTs { get; set; }

        public ImportAction Action
        {
            get
            {
                if (obj1C == null && objTs == null)
                    return ImportAction.NO_ACTION;
                if (obj1C == null && objTs != null)
                    return ImportAction.DELETE_FROM_TS;
                if (obj1C != null && objTs == null)
                    return ImportAction.ADD_TO_TS;
                else
                {
                    if (obj1C.Amount == objTs.Amount
                        && obj1C.PayerID == objTs.PayerID
                        && obj1C.RecipientID == objTs.RecipientID
                        && obj1C.Subject == objTs.Subject
                        && obj1C.Obj1cDocNumIn == objTs.Obj1cDocNumIn)
                    {
                        return ImportAction.NO_ACTION;
                    }
                    else
                        if (obj1C.Amount == objTs.Amount
                            && obj1C.PayerID == objTs.PayerID
                            && obj1C.RecipientID == objTs.RecipientID
                            && obj1C.Subject == objTs.Subject
                            && obj1C.Obj1cDocNumIn != objTs.Obj1cDocNumIn)
                        {
                            return ImportAction.UPDATE_1CDOCNUMIN;
                        }
                        else
                            if (obj1C.Amount != objTs.Amount
                            && obj1C.PayerID == objTs.PayerID
                            && obj1C.RecipientID == objTs.RecipientID
                            && obj1C.Subject == objTs.Subject)
                            {
                                return ImportAction.UPDATE_AMOUNT;
                            }
                            else
                                if (obj1C.Amount == objTs.Amount
                                && obj1C.PayerID != objTs.PayerID
                                && obj1C.RecipientID == objTs.RecipientID
                                && obj1C.Subject == objTs.Subject)
                                {
                                    return ImportAction.UPDATE_PAYER;
                                }
                                else
                                    if (obj1C.Amount == objTs.Amount
                                    && obj1C.PayerID == objTs.PayerID
                                    && obj1C.RecipientID != objTs.RecipientID
                                    && obj1C.Subject == objTs.Subject)
                                    {
                                        return ImportAction.UPDATE_RECEIVER;
                                    }
                                    else
                                        if (obj1C.Amount == objTs.Amount
                                        && obj1C.PayerID == objTs.PayerID
                                        && obj1C.RecipientID == objTs.RecipientID
                                        && obj1C.Subject != objTs.Subject)
                                        {
                                            return ImportAction.UPDATE_SUBJECT;
                                        }
                    return ImportAction.NO_ACTION;

                }
            }
        }

        public void PrintCompareResultInfo()
        {            
            string target = "x";
            int subLength = 4;
            string stringToWrite = "";

            if (this.obj1C == null || this.objTs == null)
            {
                DateTime dt = (this.obj1C != null) ? this.obj1C.ActualDate.Value : this.objTs.ActualDate.Value;
                stringToWrite = dt.ToShortDateString() + " ";

                if (this.obj1C != null)
                {

                    target = this.obj1C.Subject;
                    stringToWrite += String.Format("{0,10} {1,5} {2,5} | ", this.obj1C.Amount.GetValueOrDefault(),
                                            this.obj1C.PayerID.GetValueOrDefault().ToString().Substring(0, subLength),
                                            this.obj1C.RecipientID.GetValueOrDefault().ToString().Substring(0, subLength));
                }
                else
                {
                    stringToWrite += String.Format("{0,10} {1,5} {2,5} | ", "x", "x", "x");
                }
                if (this.objTs != null)
                {
                    target = this.objTs.Subject;
                    stringToWrite += String.Format("{0,10} {1,5} {2,5} {3,14} {4}", this.objTs.Amount.GetValueOrDefault(),
                                        this.objTs.PayerID.GetValueOrDefault().ToString().Substring(0, subLength),
                                        this.objTs.RecipientID.GetValueOrDefault().ToString().Substring(0, subLength),
                                        this.Action, target);
                }
                else
                {
                    stringToWrite += String.Format("{0,10} {1,5} {2,5} {3,14} {4}", "x", "x", "x", this.Action, target);
                }

                stringToWrite += "\n";
                Console.Write(stringToWrite);                
            }
        }
    }    
}