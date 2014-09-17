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
    
    public partial class tbl_AdminUnit
    {
        public tbl_AdminUnit()
        {
            this.tbl_AdminUnit1 = new HashSet<tbl_AdminUnit>();
            this.tbl_CashflowRight = new HashSet<tbl_CashflowRight>();
        }
    
        public System.Guid ID { get; set; }
        public string Name { get; set; }
        public Nullable<System.Guid> GroupParentID { get; set; }
        public Nullable<int> IsGroup { get; set; }
        public Nullable<int> GroupPasswordChangePeriodType { get; set; }
        public Nullable<int> UserIsAdmin { get; set; }
        public Nullable<System.Guid> UserContactID { get; set; }
        public Nullable<int> UserPasswordNeverExpired { get; set; }
        public Nullable<System.DateTime> UserPasswordChangedOn { get; set; }
        public Nullable<int> UserIsEnabled { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<System.Guid> CreatedByID { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<System.Guid> ModifiedByID { get; set; }
        public Nullable<int> UserIsSysAdmin { get; set; }
        public string SQLObjectName { get; set; }
        public Nullable<int> IsDomainUnit { get; set; }
    
        public virtual ICollection<tbl_AdminUnit> tbl_AdminUnit1 { get; set; }
        public virtual tbl_AdminUnit tbl_AdminUnit2 { get; set; }
        public virtual ICollection<tbl_CashflowRight> tbl_CashflowRight { get; set; }
    }
}
