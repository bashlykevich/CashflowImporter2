using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.IO;
using StatementsImporterLib.DAO;
using StatementsImporterLib.ADO;
using StatementsImporterLib.Toolkit;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;

namespace StatementsImporterLib.Controllers
{
    public class Connector1C
    {
        private List<DataRow> ImportedRows = new List<DataRow>();

        #region public methods

        public Connector1C(string filename, Company company)
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
                                t.ВидДвижения = GetTransferAttributeName("ВидДвижения", transferBaseGUID);
                                t.Company = company;
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
                                //if (bs.ДатаДок == "12.02.14")
                                /*if (t.ВидДвижения.Contains("Возврат от пос"))
                                {
                                   Console.WriteLine(ii++.ToString() + " " 
                                       + bs.ДатаДок
                                       + " " + t.НомерДокВходящий);
                                    Console.WriteLine(" {0}/{1}/{2}/{3}/{4}/{5}", t.ВидДвижения
                                        ,t.Субконто1.Наименование
                                        ,t.НазначениеПлатежа
                                        , String.Format("{0:n0}", t.Приход).Replace(",", " ")
                                        //, String.Format("{0:n0}", t.Расход).Replace(",", " ")
                                        ,t.НомерДокВходящий);
                                    //Console.WriteLine(" {0}/{1}", String.Format("{0:n0}", t.Приход).Replace(",", " ")
                                      //  , String.Format("{0:n0}", t.Расход).Replace(",", " "));
                                    //Console.WriteLine("Назначение Платежа: \t" + t.НазначениеПлатежа);
                                    //Console.WriteLine("Вид Движения: \t" + t.ВидДвижения);
                                    //Console.WriteLine("Субконто1: \t" + t.Субконто1.Наименование);
                                    //Console.WriteLine("Субконто2: \t" + t.Субконто2.Наименование);
                                    //Console.WriteLine("Субконто3: \t" + t.Субконто3.Наименование);
                                    //Console.WriteLine("Приход: \t" + String.Format("{0:n0}", t.Приход).Replace(",", " "));
                                    //Console.WriteLine("Расход: \t" + String.Format("{0:n0}", t.Расход).Replace(",", " "));
                                    //Console.ReadKey();                                 
                                }*/
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
            foreach(tbl_Cashflow c in listTs)
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
            foreach(CashflowComparer cc in hashes)
            {
                if(cc.Action == ImportAction.UPDATE_1CDOCNUMIN)
                {
                    //Console.WriteLine("Update 1C DocNumIn");
                    db.tbl_Cashflow.FirstOrDefault(x => x.ID == cc.objTs.ID).Obj1cDocNumIn = cc.obj1C.Obj1cDocNumIn;
                }
            }
            db.SaveChanges();
        }
        public void RunExport(string connectionString, DateTime startDate, DateTime endDate, Company company)
        {
            string companyID = getDs_CompanyID(company);
            using (Entities db = new Entities(connectionString))
            {
                Helper.Log("Соединение с базой установлено.");
                DateTime compareDate = startDate;
                do
                {
                    //Из выписки 1С за ДАТУ-1 выбирается набор-платежей-1
                    List<Transfer> list1C = getTransfersFrom1c(compareDate);
                    //За эту же ДАТУ-1 выбирается набор-платежей-2                    
                    List<tbl_Cashflow> listTs = getTransfersFromTs(compareDate, companyID, db);

                    // обновить номераДокВходящих для уже импортированных записей    
                    Update1cDocNumIns(db, list1C, listTs, compareDate);
                                        
                    List<CashflowComparer> hashes = new List<CashflowComparer>();
                    foreach (tbl_Cashflow t in listTs)
                    {
                        string h = CashflowComparer.getHash(t);
                        //if (!hashes.Exists(x => x.hash == h))
                        //{
                            CashflowComparer cc = new CashflowComparer()
                            {
                                hash = h,
                                objTs = t
                            };
                            hashes.Add(cc);
                        //}                        
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
                    // актулизировать список 
                    foreach (CashflowComparer cc in hashes)
                    {
                        PrintCompareResultInfo(cc);                        
                        //Если хэш из набора-1с есть в наборе-ТС - всё норм, оставляем как есть                        
                        switch (cc.Action)
                        {
                            case ImportAction.DELETE_FROM_TS:
                                //Если хэш из набора-ТС отсутствует в наборе-1С - удаляем этот платёж из ТС                                                                        
                                db.tbl_Cashflow.Remove(cc.objTs);
                                break;
                            case ImportAction.ADD_TO_TS:
                                //Если хэш из набора-1с отсутствует в наборе-ТС - импортируем этот платёж из 1С в ТС
                                db.tbl_Cashflow.Add(cc.obj1C);
                                // раздаём права на новый платёж
                                // получаем менеджера записи    
                                if(cc.obj1C.ManagerID.HasValue)
                                {
                                    if(db.tbl_CashflowRight.Count(x => x.RecordID == cc.obj1C.ID && x.AdminUnitID == cc.obj1C.ManagerID) == 0)
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
                                break;
                        }                        
                    }
                    db.SaveChanges();
                    compareDate = compareDate.AddDays(1);
                } while (compareDate <= endDate);

                Helper.Log("Сравнение завершёно.\n");
            }
        }
        void PrintCompareResultInfo(CashflowComparer cc)
        {
            string target = "x";
            int subLength = 4;
            DateTime dt = (cc.obj1C != null) ? cc.obj1C.ActualDate.Value : cc.objTs.ActualDate.Value;
            Console.Write(dt.ToShortDateString() + " ");
            if (cc.obj1C == null || cc.objTs == null)
            {
                if (cc.obj1C != null)
                {
                    target = cc.obj1C.Subject;
                    Console.Write("{0,10} {1,5} {2,5} | ", cc.obj1C.Amount.GetValueOrDefault(),
                                            cc.obj1C.PayerID.GetValueOrDefault().ToString().Substring(0, subLength),
                                            cc.obj1C.RecipientID.GetValueOrDefault().ToString().Substring(0, subLength));
                }
                else
                {
                    Console.Write("{0,10} {1,5} {2,5} | ", "x", "x", "x");
                }
                if (cc.objTs != null)
                {
                    target = cc.objTs.Subject;
                    Console.Write("{0,10} {1,5} {2,5} {3,14} {4}", cc.objTs.Amount.GetValueOrDefault(),
                                        cc.objTs.PayerID.GetValueOrDefault().ToString().Substring(0, subLength),
                                        cc.objTs.RecipientID.GetValueOrDefault().ToString().Substring(0, subLength),
                                        cc.Action, target);
                }
                else
                {
                    Console.Write("{0,10} {1,5} {2,5} {3,14} {4}", "x", "x", "x", cc.Action, target);
                }

                Console.Write("\n");
            }
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
            Double.TryParse(subconto[5].ToString(), out res);
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
        private string ParseContractNumber(string ContractName)
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
        #endregion

        private enum CashflowType { Income, Expense };
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
            
            c.ModifiedByID = GetSupervisorID();
            c.ModifiedOn = DateTime.Now;
            c.CreatedByID = GetSupervisorID();
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
            c.OwnerID = GetOwnerID();

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
            c.PeriodID = GetPeriodID(db, c.ActualDate.Value);

            // 00 Контрагент
            string comments = "";
            Guid? AccountID = GetAccountID(db, t);
            if (!AccountID.HasValue)
            {
                comments += "Контрагент не найден: " + GetAccountNameCode(t) + ".\r\n";
            }
            // 19 УЧИТЫВАТЬ ПРИ ВЗАИМОРАСЧЁТАХ NULL
            // 20 ДЕБИТОР-КРЕДИТОР NULL
            // 21 АВТОМАТИЧЕСКИ РАССЧИТЫВАТЬ СУММУ
            c.AutocalcAmount = 1;

            // 22 ВАЛЮТ
            c.CurrencyID = ConvertToCurrencyID(t.Валюта);

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
            Guid? ContractID = GetContractID(db, t);
            if (ContractID.HasValue)
            {
                c.ContractID = ContractID;
            }
            else
            {
                comments += "Договор не найден: " + GetContractName(t) + ".";
            }

            // 29 ПРОДАЖА - получаем из договора
            if (ContractID.HasValue)
            {
                c.OpportunityID = GetOpportunityIDFromContract(db, ContractID);
            }

            // 30 МЕНЕДЖЕР - получаем из договора
            if (ContractID.HasValue)
            {
                c.ManagerID = GetManagerIDFromContract(db, ContractID);
            }
            else
                if (AccountID.HasValue)
                {
                    c.ManagerID = GetManagerIDFromAccount(db, AccountID);
                }
                else
                {
                    c.ManagerID = GetDefaultManagerID();
                }

            // 31 если найден договор, но не найден контрагент
            if (ContractID.HasValue && !AccountID.HasValue)
            {
                AccountID = GetAccountIDFromContract(db, ContractID);
            }

            // 17 ПЛАТЕЛЬЩИК
            // 18 ПОЛУЧАТЕЛЬ
            if (cashflowType == CashflowType.Income)
            {
                c.PayerID = AccountID;
                c.RecipientID = GetCompanyID(t.Company);
            }
            else
            {
                c.PayerID = GetCompanyID(t.Company);
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

            c.Obj1cDocNumIn = t.НомерДокВходящий;
            c.ModifiedByID = GetSupervisorID();
            c.ModifiedOn = DateTime.Now;
            c.CreatedByID = GetSupervisorID();
            c.CreatedOn = DateTime.Now;

            string cID = getDs_CompanyID(t.Company);
            c.CompanyID = new Guid(cID);
            // 01 НОМЕР
            c.CFNumber = GetNextNumber(db);

            // 02 НАЗНАЧЕНИЕ
            c.Subject = t.ВидДвижения + ": " + t.НазначениеПлатежа;

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
            // 06 КАТЕГОРИЯ NULL
            // 07 ОТВЕТСТВЕННЫЙ
            c.OwnerID = GetOwnerID();

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
            c.PeriodID = GetPeriodID(db, c.ActualDate.Value);

            // 00 Контрагент
            string comments = "";
            Guid? AccountID = GetAccountID(db, t);
            if (!AccountID.HasValue)
            {
                comments += "Контрагент не найден: " + GetAccountNameCode(t) + ".\r\n";
            }
            // 19 УЧИТЫВАТЬ ПРИ ВЗАИМОРАСЧЁТАХ NULL
            // 20 ДЕБИТОР-КРЕДИТОР NULL
            // 21 АВТОМАТИЧЕСКИ РАССЧИТЫВАТЬ СУММУ
            c.AutocalcAmount = 1;

            // 22 ВАЛЮТ
            c.CurrencyID = ConvertToCurrencyID(t.Валюта);

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
            Guid? ContractID = GetContractID(db, t);
            if (ContractID.HasValue)
            {
                c.ContractID = ContractID;
            }
            else
            {
                comments += "Договор не найден: " + GetContractName(t) + ".";
            }

            // 29 ПРОДАЖА - получаем из договора
            if (ContractID.HasValue)
            {
                c.OpportunityID = GetOpportunityIDFromContract(db, ContractID);
            }

            // 30 МЕНЕДЖЕР - получаем из договора
            if (ContractID.HasValue)
            {
                c.ManagerID = GetManagerIDFromContract(db, ContractID);
            }
            else
                if (AccountID.HasValue)
                {
                    c.ManagerID = GetManagerIDFromAccount(db, AccountID);
                }
                else
                {
                    c.ManagerID = GetDefaultManagerID();
                }

            // 31 если найден договор, но не найден контрагент
            if (ContractID.HasValue && !AccountID.HasValue)
            {
                AccountID = GetAccountIDFromContract(db, ContractID);
            }

            // 17 ПЛАТЕЛЬЩИК
            // 18 ПОЛУЧАТЕЛЬ
            if (cashflowType == CashflowType.Income)
            {
                c.PayerID = AccountID;
                c.RecipientID = GetCompanyID(t.Company);
            }
            else
            {
                c.PayerID = GetCompanyID(t.Company);
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
        private void CommitTransferToDatabase(Entities db, Transfer t, string docDate)
        {
            tbl_Cashflow c = CreateCashflow(db, t, docDate);
            db.tbl_Cashflow.Add(c);
            db.SaveChanges();
        }

        private Guid? ConvertToCurrencyID(string Code)
        {
            Guid? res = null;
            switch (Code)
            {
                case "USD":
                    res = new Guid(Constants.CurrencyUsdID);
                    break;

                case "EUR"://?
                    res = new Guid(Constants.CurrencyEurID);
                    break;

                case "RUB"://?
                    res = new Guid(Constants.CurrencyRurID);
                    break;

                default:
                    res = new Guid(Constants.CurrencyByrID);
                    break;
            }
            return res;
        }

        private Guid? GetAccountID(Entities db, Transfer t)
        {
            Guid? AccountID = null;
            Subconto Account = GetSubconto(t, SubcontoType.Контрагент);
            if (Account != null)
            {
                string AccountUNN = Account.Код;
                AccountID = GetAccountID(db, AccountUNN);
            }
            return AccountID;
        }

        private Guid? GetAccountID(Entities db, string AccountUNN)
        {
            Guid? AccountID = null;
            tbl_Account a = db.tbl_Account.FirstOrDefault(x => x.Code == AccountUNN);
            if (a != null)
                AccountID = a.ID;
            return AccountID;
        }

        private Guid? GetAccountIDFromContract(Entities db, Guid? ContractID)
        {
            Guid? id = null;
            id = db.tbl_Contract.FirstOrDefault(x => x.ID == ContractID).CustomerID;
            return id;
        }

        private string GetAccountNameCode(Transfer t)
        {
            string name = "";
            Subconto Account = GetSubconto(t, SubcontoType.Контрагент);
            if (Account != null)
                name = Account.Наименование + " (" + Account.Код + ")";
            return name;
        }

        private string GetCashflowTypeID(CashflowType type)
        {
            if (type == CashflowType.Income)
                return Constants.CashflowTypeIncomeID;
            else
                return Constants.CashflowTypeExpenseID;
        }

        private Guid? GetContractID(Entities db, Transfer t)
        {
            Guid? ContractID = null;
            Subconto Contract = GetSubconto(t, SubcontoType.Договор);
            if (Contract != null)
            {
                string ContractNumber = ParseContractNumber(Contract.Наименование);
                ContractID = GetContractID(db, ContractNumber);
            }
            return ContractID;
        }
        public int ExtraFound = 0;
        private Guid? GetContractID(Entities db, string ContractNumber)
        {
            char[] alph_en = { 'E', 'T', 'O', 'P', 'A', 'H', 'K', 'X', 'C', 'B', 'M' };
            char[] alph_ru = { 'Е', 'Т', 'О', 'Р', 'А', 'Н', 'К', 'Х', 'С', 'В', 'М' };

            Guid? ContractID = null;
            tbl_Contract a = db.tbl_Contract.FirstOrDefault(x => x.ContractNumber == ContractNumber);
            if (a != null)
                ContractID = a.ID;
            else
            {
                for (int i = 0; i < alph_en.Length; i++)
                {
                    if (ContractNumber.Contains(alph_en[i]))
                    {
                        string cn = ContractNumber.Replace(alph_en[i], alph_ru[i]);
                        a = db.tbl_Contract.FirstOrDefault(x => x.ContractNumber == cn);
                        if (a != null)
                        {
                            ContractID = a.ID;
                            ExtraFound++;
                            break;
                        }
                    }
                    if (ContractNumber.Contains(alph_ru[i]))
                    {
                        string cn = ContractNumber.Replace(alph_ru[i], alph_en[i]);
                        a = db.tbl_Contract.FirstOrDefault(x => x.ContractNumber == cn);
                        if (a != null)
                        {
                            ContractID = a.ID;
                            ExtraFound++;
                            break;
                        }
                    }
                }
            }
            return ContractID;
        }

        private string GetContractName(Transfer t)
        {
            string name = "";
            Subconto Contract = GetSubconto(t, SubcontoType.Договор);
            if (Contract != null)
                name = Contract.Наименование;
            return name;
        }

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
        private Guid GetDefaultManagerID()
        {
            return new Guid(Constants.DefaultManagerID);
        }
        private Guid? GetManagerIDFromAccount(Entities db, Guid? AccountID)
        {
            Guid? id = null;
            id = db.tbl_Account.FirstOrDefault(x => x.ID == AccountID).OwnerID;
            return id;
        }

        private Guid? GetManagerIDFromContract(Entities db, Guid? ContractID)
        {
            Guid? id = null;
            tbl_Contract contract = db.tbl_Contract.FirstOrDefault(x => x.ID == ContractID);
            id = contract.OwnerID;
            return id;
        }

        private Guid? GetCompanyID(Company company)
        {
            switch (company)
            {
                case Company.MS:
                    return new Guid(Constants.Company_MS);
                case Company.NE:
                    return new Guid(Constants.Company_NE);
                case Company.SP:
                    return new Guid(Constants.Company_SP);
                case Company.ZA:
                    return new Guid(Constants.Company_ZA);
                default:
                    return new Guid(Constants.Company_MS);
            }

        }

        private string GetNextNumber(Entities db)
        {
            string num = "1С" + (db.tbl_Cashflow.Count() + 1).ToString();
            return num;
        }

        private Guid? GetOpportunityIDFromContract(Entities db, Guid? ContractID)
        {
            Guid? id = null;
            id = db.tbl_Contract.FirstOrDefault(x => x.ID == ContractID).OpportunityID;
            return id;
        }

        private Guid? GetOwnerID()
        {
            return new Guid(Constants.CashflowOwnerID);
        }

        private Guid? GetSupervisorID()
        {
            return new Guid("251FB9AC-C17E-4DF7-A0CB-D591FDB97462");
        }

        private Guid? GetPeriodID(Entities db, DateTime date)
        {
            tbl_Period p = db.tbl_Period.FirstOrDefault(x => x.StartDate <= date && date <= x.DueDate);
            if (p == null)
                return null;
            else
                return p.ID;
        }

        #endregion private methods
    }
    enum ImportAction { NO_ACTION, NO_ACTION_FOR_NULLS, UPDATE_ALL, UPDATE_AMOUNT, UPDATE_PAYER, UPDATE_RECEIVER
                        , UPDATE_SUBJECT, ADD_TO_TS, DELETE_FROM_TS, UPDATE_1CDOCNUMIN}
    class CashflowComparer
    {
        public static string getHashWithoutDocNum(tbl_Cashflow c)
        {
            string[] keys = { c.Amount.ToString(), c.PayerID.ToString(), c.RecipientID.ToString() };
            return Helper.CalculateMD5Hash(keys);
        }
        public static string getHash(tbl_Cashflow c)
        {
            string[] keys = { ((long)c.Amount).ToString(), c.PayerID.ToString(), c.RecipientID.ToString(), c.Obj1cDocNumIn };
            return Helper.CalculateMD5Hash(keys);
        }
        public string hash { get; set; }
        public tbl_Cashflow obj1C { get; set; }
        public tbl_Cashflow objTs { get; set; }

        public ImportAction  Action
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
                    } else
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
    }
    public class Constants
    {
        public const string CashflowKassaID = "ADEC1E41-17EE-4B02-B04E-D677DEA48D39"; // основной расчетный счёт
        public const string CashflowOwnerID = "04AC88F1-CB81-4179-AA70-156DEE3AA022"; // Довгалёва
        public const string CashflowStateFinishedID = "FDEA47BE-53FE-4730-BF4F-4F44C3B5D61A";
        public const string CashflowTypeExpenseID = "{484C8429-DABF-482A-BC7B-4C75D1436A1B}";
        public const string CashflowTypeIncomeID = "{358DA0CD-9EA6-43B8-A099-CD77DA3C6114}";

        //public static string CashflowTypeExpenseID = "ct_Charge";
        //public static string CashflowTypeIncomeID = "ct_Income";
        public const string CurrencyByrID = "49744485-ACC1-4729-9BE6-C595E01DA2FF";
        public const string DefaultManagerID = "04AC88F1-CB81-4179-AA70-156DEE3AA022"; //Довгалева

        public const string CurrencyEurID = "D18AAED6-14F9-435C-9606-0E90CAE816F9";
        public const string CurrencyRurID = "CC997518-B672-4F0B-AD9B-0668F06AE404";
        public const string CurrencyUsdID = "D18AAED6-14F9-435C-9606-0E90CAE816F8";

        public const string Company_MS = "486AF303-9532-462C-A0A4-CB41A0C1C837";
        public const string Company_ZA = "C7202761-078E-4AEB-9531-7A755803F9C0";
        public const string Company_NE = "BFF3459D-61D5-4886-9723-8572B07721F2";
        public const string Company_SP = "DB12B73C-E54C-48D7-B51A-626B00E1392C";
    }
}