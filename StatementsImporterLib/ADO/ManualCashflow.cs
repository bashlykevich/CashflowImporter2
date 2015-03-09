using StatementsImporterLib.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatementsImporterLib.ADO
{
    public class ManualCashflow
    {
        public string Date;
        public string Number;
        public string Amount;
        public string Contract;

        public tbl_Cashflow toTsCashflow(tbl_Cashflow p, Entities db)
        {
            tbl_Cashflow c = new tbl_Cashflow();

            string comments = "";
            c.Amount = decimal.Parse(this.Amount);
            c.Obj1cDocNumIn = this.Number;

            c.Subject = "Детализация: " + this.Contract;
            c.OpportunityID = p.OpportunityID;
            c.ManagerID = p.ManagerID;
            c.OwnerID = p.OwnerID;

            c.ActualDate = p.ActualDate;
            c.AutocalcAmount = p.AutocalcAmount;
            c.CampaignID = p.CampaignID;
            c.CashAccountID = p.CashAccountID;
            c.CategoryID = p.CategoryID;
            c.CFNumber = p.CFNumber;
            c.ClauseID = p.ClauseID;
            c.CompanyID = p.CompanyID;
            c.ContactID = p.ContactID;
            c.CreatedByID = p.CreatedByID;
            c.CreatedOn = DateTime.Now;
            c.CurrencyID = p.CurrencyID;
            c.CurrencyRate = p.CurrencyRate;
            c.DebtorCreditorID = p.DebtorCreditorID;
            c.Description = p.Description;
            c.DocDate = p.DocDate;
            c.EstimatedDate = p.EstimatedDate;
            c.ExpactablePayDate = p.ExpactablePayDate;
            c.ExpenseTypeID = p.ExpenseTypeID;
            c.ID = Guid.NewGuid();
            c.IncidentID = p.IncidentID;
            //c.LinkID = p.LinkID;
            c.MailSent = p.MailSent;
            c.ModifiedByID = p.ModifiedByID;
            c.ModifiedOn = DateTime.Now;
            c.PayerID = p.PayerID;
            c.PeriodID = p.PeriodID;
            c.ProjectID = p.ProjectID;
            c.RecipientID = p.RecipientID;
            c.ServiceAgreementID = p.ServiceAgreementID;
            c.StatusID = p.StatusID;
            c.TypeID = p.TypeID;
            c.UseAsCashflow = p.UseAsCashflow;
            c.UseAsPandL = p.UseAsPandL;

            //c.ContractID
            //c.InvoiceID
            string contractNumber = Helper.ParseContractNumber(this.Contract);
            Guid? ContractID = DbHelper.GetContractID(db, contractNumber);
            if (ContractID.HasValue)
            {
                c.ContractID = ContractID;
            }
            else
            {
                comments += "Договор не найден: " + this.Contract + ".";
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
                if (c.PayerID.HasValue)
                {
                    c.ManagerID = DbHelper.GetManagerIDFromAccount(db, c.PayerID);
                }
                else
                {
                    c.ManagerID = DbHelper.GetDefaultManagerID();
                }
            string InvoiceString = "счет ";
            string PayDetails = this.Contract;
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
                if (c.PayerID.HasValue)
                {
                    invoices = invoices.Where(i => i.CustomerID == c.PayerID).ToList();
                }
                if (invoices.Count > 0)
                {
                    tbl_Invoice i = invoices.First();
                    c.InvoiceID = i.ID;
                }
            }
            c.Comments = comments;
            c.UID1C = c.CFNumber;
            // todo
            //c.BasicAmount = p.BasicAmount;            
            return c;
        }
    }
}
