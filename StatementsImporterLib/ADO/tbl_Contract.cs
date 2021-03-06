//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace StatementsImporterLib.ADO
{
    using System;
    using System.Collections.Generic;
    
    public partial class tbl_Contract
    {
        public tbl_Contract()
        {
            this.tbl_Contract1 = new HashSet<tbl_Contract>();
            this.tbl_Invoice = new HashSet<tbl_Invoice>();
            this.tbl_Cashflow = new HashSet<tbl_Cashflow>();
        }
    
        public System.Guid ID { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<System.Guid> CreatedByID { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<System.Guid> ModifiedByID { get; set; }
        public string Title { get; set; }
        public string ContractNumber { get; set; }
        public Nullable<System.Guid> ContractTypeID { get; set; }
        public Nullable<System.Guid> ParentContractID { get; set; }
        public Nullable<System.Guid> OpportunityID { get; set; }
        public Nullable<System.Guid> ContractStatusID { get; set; }
        public Nullable<System.Guid> OwnerID { get; set; }
        public Nullable<System.DateTime> StartDate { get; set; }
        public Nullable<System.DateTime> DueDate { get; set; }
        public Nullable<System.Guid> CustomerID { get; set; }
        public Nullable<System.Guid> BillInfoID { get; set; }
        public string BillingFrequencyID { get; set; }
        public Nullable<System.Guid> CurrencyID { get; set; }
        public Nullable<decimal> CurrencyRate { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public Nullable<decimal> BasicAmount { get; set; }
        public byte[] Description { get; set; }
        public Nullable<System.Guid> ContactID { get; set; }
        public Nullable<System.Guid> CampaignID { get; set; }
        public Nullable<System.Guid> IncidentID { get; set; }
        public Nullable<System.Guid> WorkflowItemID { get; set; }
        public Nullable<System.Guid> ProjectID { get; set; }
        public Nullable<System.Guid> ManagerOBUKID { get; set; }
        public Nullable<System.Guid> LogisticManagerID { get; set; }
        public Nullable<decimal> AdvanceAmount { get; set; }
        public Nullable<decimal> BasicAdvanceAmount { get; set; }
        public Nullable<System.DateTime> SigneOwnDate { get; set; }
        public Nullable<System.DateTime> RejectionDate { get; set; }
        public Nullable<System.DateTime> SigneClienttDate { get; set; }
        public Nullable<System.DateTime> PlannedAdvanceDate { get; set; }
        public Nullable<System.DateTime> PlannedFullPayDate { get; set; }
        public Nullable<System.DateTime> SupplyStartDate { get; set; }
        public Nullable<System.DateTime> PlannedSupplyFinishDate { get; set; }
        public Nullable<System.DateTime> WorkStartDate { get; set; }
        public Nullable<System.DateTime> PlannedWorkFinishDate { get; set; }
        public Nullable<System.DateTime> AdvancePayDate { get; set; }
        public Nullable<decimal> PlannedProfit { get; set; }
        public Nullable<decimal> PayedAmount { get; set; }
        public Nullable<System.DateTime> FactSupplyFinishDate { get; set; }
        public Nullable<System.DateTime> FactFullPayDate { get; set; }
        public Nullable<System.DateTime> ActClientSigneDate { get; set; }
        public Nullable<System.DateTime> ActOurSigneDate { get; set; }
        public Nullable<int> WorkRating { get; set; }
        public Nullable<int> SupplyRating { get; set; }
        public Nullable<decimal> FactProfit { get; set; }
        public Nullable<int> ClientSatisfaction { get; set; }
        public string ResultComments { get; set; }
        public Nullable<System.DateTime> FactWorkFinishDate { get; set; }
        public Nullable<int> AdvanceWithin { get; set; }
        public Nullable<int> Closed { get; set; }
        public Nullable<int> ContractType1 { get; set; }
        public Nullable<int> ContractType2 { get; set; }
        public Nullable<int> ContractType3 { get; set; }
        public Nullable<int> ContractType4 { get; set; }
        public Nullable<int> ContractType5 { get; set; }
        public Nullable<int> ContractType6 { get; set; }
        public Nullable<int> ContractType7 { get; set; }
        public Nullable<int> mailSent3 { get; set; }
        public Nullable<int> ContractType8 { get; set; }
        public Nullable<decimal> PayedAmountBasic { get; set; }
        public Nullable<int> isFrame { get; set; }
        public Nullable<int> IsInvoice { get; set; }
        public Nullable<System.DateTime> ContractCreationDate { get; set; }
        public string NumberTitle { get; set; }
        public Nullable<decimal> AmountOfOfferings { get; set; }
        public Nullable<decimal> AmountOfServices { get; set; }
        public Nullable<decimal> AmountOfOfferingsBasic { get; set; }
        public Nullable<decimal> AmountOfServicesBasic { get; set; }
        public Nullable<decimal> ProfitPlannedBasic { get; set; }
        public Nullable<decimal> ProfitActualBasic { get; set; }
        public string MonthID { get; set; }
        public int MailOnCreate { get; set; }
        public Nullable<System.Guid> TempCompanyFields { get; set; }
        public Nullable<System.Guid> ContractCompanyID { get; set; }
        public Nullable<int> WasTaskCreated { get; set; }
        public Nullable<int> TermsOfObligations { get; set; }
        public Nullable<int> Maturity { get; set; }
        public Nullable<int> Prepayment { get; set; }
        public Nullable<System.Guid> DocumentID { get; set; }
        public int MailOnProcess { get; set; }
        public Nullable<int> IsAutoCalculated { get; set; }
        public string ContractDirectionID { get; set; }
        public Nullable<int> MailOnSigned { get; set; }
        public string Object1C { get; set; }
        public string Code1C { get; set; }
        public Nullable<int> ContractType9 { get; set; }
        public Nullable<int> ContractType10 { get; set; }
        public Nullable<int> ContractType11 { get; set; }
        public Nullable<int> ContractType12 { get; set; }
        public Nullable<int> ContractType13 { get; set; }
    
        public virtual tbl_Account tbl_Account { get; set; }
        public virtual tbl_Account tbl_Account1 { get; set; }
        public virtual tbl_Contact tbl_Contact { get; set; }
        public virtual tbl_Contact tbl_Contact1 { get; set; }
        public virtual tbl_Contact tbl_Contact2 { get; set; }
        public virtual tbl_Contact tbl_Contact3 { get; set; }
        public virtual ICollection<tbl_Contract> tbl_Contract1 { get; set; }
        public virtual tbl_Contract tbl_Contract2 { get; set; }
        public virtual ICollection<tbl_Invoice> tbl_Invoice { get; set; }
        public virtual ICollection<tbl_Cashflow> tbl_Cashflow { get; set; }
    }
}
