using StatementsImporterLib.ADO;
using StatementsImporterLib.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatementsImporterLib.Toolkit
{
    public class DbHelper
    {
        public static tbl_Cashflow GetParentCashflow(string parentCashflowNumber, string date, Entities db)
        {
            DateTime dt = DateTime.Parse(date);
            if (db.tbl_Cashflow.Count(x => x.Obj1cDocNumIn == parentCashflowNumber && x.DocDate == dt) > 1)
                return null;
            return db.tbl_Cashflow.FirstOrDefault(x => x.Obj1cDocNumIn == parentCashflowNumber && x.DocDate == dt);
        }
        public static void GrantToManagerAccessToCashflow(tbl_Cashflow cashflow, Entities db)
        {
            if (cashflow.ManagerID.HasValue)
            {
                if (db.tbl_CashflowRight.Count(x => x.RecordID == cashflow.ID && x.AdminUnitID == cashflow.ManagerID) == 0)
                {
                    Guid managerAdminUnitID = db.tbl_AdminUnit.FirstOrDefault(x => x.UserContactID == cashflow.ManagerID).ID;
                    tbl_CashflowRight rights = new tbl_CashflowRight
                    {
                        AdminUnitID = managerAdminUnitID,
                        CanChangeAccess = 1,
                        CanDelete = 0,
                        CanRead = 1,
                        CanWrite = 1,
                        ID = Guid.NewGuid(),
                        RecordID = cashflow.ID
                    };
                    db.tbl_CashflowRight.Add(rights);
                }
            }
        }

        static List<tbl_CashflowClause> clauses = new List<tbl_CashflowClause>();
        public static Guid? GetCashflowClauseID(Entities db, CashflowClause clause1c, Company company)
        {            
            string clauseCode = "1С" +  String.Format("{0,5}", clause1c.Код).Replace(" ", "0");
            if (clauses.Count == 0)
            {
                clauses = (from x in db.tbl_CashflowClause select x).ToList();
            }

            if (clauses.Count(x => x.Code == clauseCode) == 0)
            {
                string clauseName = clause1c.Наименование.Trim();
                if(clauseName.Length> 200)
                {
                    clauseName = clauseName.Substring(0, 200);
                }
                string clauseDescr = (clause1c.ВидДвижения + ": " + clause1c.РазрезДеятельности).Trim();
                if (clauseDescr.Length > 200)
                {
                    clauseDescr = clauseDescr.Substring(0, 200);
                }
                clauseCode = clauseCode.Trim();
                if (clauseCode.Length > 200)
                {
                    clauseCode = clauseCode.Substring(0, 200);
                }
                //add clause
                tbl_CashflowClause clauseTs = new tbl_CashflowClause
                {
                    ID = Guid.NewGuid(),
                    Name = clauseName,
                    Code = clauseCode,
                    CreatedByID = new Guid(Constants.DefaultAdminID),
                    CreatedOn = DateTime.Now,
                    Description = clauseDescr,
                    ExpenseDevideType = null,
                    ExpenseTypeID = null,
                    GroupID = null,
                    IsTZP = null,
                    ModifiedByID = new Guid(Constants.DefaultAdminID),
                    ModifiedOn = DateTime.Now,
                    TypeID = null
                };
                db.tbl_CashflowClause.Add(clauseTs);
                db.SaveChanges();
                clauses.Add(clauseTs);

                return clauseTs.ID;
            }
            return clauses.FirstOrDefault(x => x.Code == clauseCode).ID;
        }

        public static Guid? GetManagerIDFromContract(Entities db, Guid? ContractID)
        {
            Guid? id = null;
            tbl_Contract contract = db.tbl_Contract.FirstOrDefault(x => x.ID == ContractID);
            id = contract.OwnerID;
            return id;
        }
        public static Guid GetDefaultManagerID()
        {
            return new Guid(Constants.DefaultManagerID);
        }
        public static Guid? GetAccountIDFromContract(Entities db, Guid? ContractID)
        {
            Guid? id = null;
            id = db.tbl_Contract.FirstOrDefault(x => x.ID == ContractID).CustomerID;
            return id;
        }
        public static Guid? GetManagerIDFromAccount(Entities db, Guid? AccountID)
        {
            Guid? id = null;
            id = db.tbl_Account.FirstOrDefault(x => x.ID == AccountID).OwnerID;
            return id;
        }
        public static Guid? GetCompanyID(Company company)
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
        public static Guid? GetOpportunityIDFromContract(Entities db, Guid? ContractID)
        {
            Guid? id = null;
            id = db.tbl_Contract.FirstOrDefault(x => x.ID == ContractID).OpportunityID;
            return id;
        }
        public static string GetContractName(Transfer t)
        {
            string name = "";
            Subconto Contract = t.Субконто2;
            if (Contract != null)
                name = Contract.Наименование;
            return name;
        }
        public static Guid? GetContractID(Entities db, string ContractNumber)
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
                            ///delete ExtraFound++;
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
                            ///delete ExtraFound++;
                            break;
                        }
                    }
                }
            }
            return ContractID;
        }
        public static Guid? GetContractID(Entities db, Transfer t)
        {
            Guid? ContractID = null;
            Subconto Contract = t.Субконто2;
            if (Contract != null)
            {
                string ContractNumber = Helper.ParseContractCode(Contract.Код);
                ContractID = GetContractID(db, ContractNumber);
                if (ContractID == null)
                {
                    ContractNumber = Helper.ParseContractNumber(Contract.Наименование);
                    ContractID = GetContractID(db, ContractNumber);
                }
            }            
            return ContractID;
        }
        public static Guid? GetAccountID(Entities db, Transfer t)
        {
            Guid? AccountID = null;
            Subconto Account = t.Субконто1;
            if (Account != null)
            {
                string AccountUNN = Account.Код;
                AccountUNN = AccountUNN.ToLower().Replace('o', '0').Replace('о', '0'); // заменяем русскую и латинскую букву О на НОЛЬ
                AccountID = DbHelper.GetAccountID(db, AccountUNN);
            }
            return AccountID;
        }
        public static Guid? ConvertToCurrencyID(string Code)
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

                case "RUВ"://?
                    res = new Guid(Constants.CurrencyRurID);
                    break;

                default:
                    res = new Guid(Constants.CurrencyByrID);
                    break;
            }
            return res;
        }
        public static Guid? GetAccountID(Entities db, string AccountUNN)
        {
            Guid? AccountID = null;
            tbl_Account a = db.tbl_Account.FirstOrDefault(x => x.Code == AccountUNN);
            if (a != null)
                AccountID = a.ID;
            return AccountID;
        }
    }
    
}
