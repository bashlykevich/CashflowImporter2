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
    
    public partial class tbl_Account
    {
        public tbl_Account()
        {
            this.tbl_Contract = new HashSet<tbl_Contract>();
            this.tbl_Contract1 = new HashSet<tbl_Contract>();
            this.tbl_Invoice = new HashSet<tbl_Invoice>();
            this.tbl_Invoice1 = new HashSet<tbl_Invoice>();
            this.tbl_Cashflow = new HashSet<tbl_Cashflow>();
            this.tbl_Cashflow1 = new HashSet<tbl_Cashflow>();
            this.tbl_Cashflow2 = new HashSet<tbl_Cashflow>();
        }
    
        public System.Guid ID { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public string Name { get; set; }
        public Nullable<System.Guid> CreatedByID { get; set; }
        public Nullable<System.Guid> ModifiedByID { get; set; }
        public string OfficialAccountName { get; set; }
        public Nullable<System.Guid> PrimaryContactID { get; set; }
        public Nullable<System.Guid> TerritoryID { get; set; }
        public Nullable<int> AnnualRevenue { get; set; }
        public Nullable<int> EmployeesNumber { get; set; }
        public Nullable<System.Guid> OwnerID { get; set; }
        public Nullable<System.Guid> CampaignID { get; set; }
        public Nullable<System.Guid> AddressTypeID { get; set; }
        public string Address { get; set; }
        public Nullable<System.Guid> CityID { get; set; }
        public Nullable<System.Guid> StateID { get; set; }
        public string ZIP { get; set; }
        public Nullable<System.Guid> CountryID { get; set; }
        public Nullable<System.Guid> ActivityID { get; set; }
        public Nullable<System.Guid> FieldID { get; set; }
        public string Communication1 { get; set; }
        public string Communication2 { get; set; }
        public string Communication3 { get; set; }
        public string Communication4 { get; set; }
        public string Communication5 { get; set; }
        public Nullable<System.Guid> Communication1TypeID { get; set; }
        public Nullable<System.Guid> Communication2TypeID { get; set; }
        public Nullable<System.Guid> Communication3TypeID { get; set; }
        public Nullable<System.Guid> Communication4TypeID { get; set; }
        public Nullable<System.Guid> Communication5TypeID { get; set; }
        public byte[] Description { get; set; }
        public Nullable<System.Guid> AccountTypeID { get; set; }
        public string Code { get; set; }
        public string TaxRegistrationCode { get; set; }
        public Nullable<decimal> SettledCredit { get; set; }
        public Nullable<int> PostponementPayment { get; set; }
        public Nullable<int> IsForeign { get; set; }
        public Nullable<System.Guid> ActivityID2 { get; set; }
        public Nullable<System.Guid> ActivityID3 { get; set; }
        public string Object1C { get; set; }
        public string Code1C { get; set; }
        public Nullable<System.Guid> UID1C { get; set; }
        public Nullable<int> UNNIsUnknown { get; set; }
        public Nullable<int> IsOurCompany { get; set; }
    
        public virtual ICollection<tbl_Contract> tbl_Contract { get; set; }
        public virtual ICollection<tbl_Contract> tbl_Contract1 { get; set; }
        public virtual ICollection<tbl_Invoice> tbl_Invoice { get; set; }
        public virtual ICollection<tbl_Invoice> tbl_Invoice1 { get; set; }
        public virtual ICollection<tbl_Cashflow> tbl_Cashflow { get; set; }
        public virtual ICollection<tbl_Cashflow> tbl_Cashflow1 { get; set; }
        public virtual ICollection<tbl_Cashflow> tbl_Cashflow2 { get; set; }
    }
}