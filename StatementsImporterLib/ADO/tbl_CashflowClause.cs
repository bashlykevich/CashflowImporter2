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
    
    public partial class tbl_CashflowClause
    {
        public tbl_CashflowClause()
        {
            this.tbl_Cashflow = new HashSet<tbl_Cashflow>();
        }
    
        public System.Guid ID { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<System.Guid> CreatedByID { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<System.Guid> ModifiedByID { get; set; }
        public Nullable<System.Guid> GroupID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public Nullable<System.Guid> TypeID { get; set; }
        public Nullable<System.Guid> ExpenseTypeID { get; set; }
        public Nullable<System.Guid> ExpenseDevideType { get; set; }
        public Nullable<int> IsTZP { get; set; }
    
        public virtual ICollection<tbl_Cashflow> tbl_Cashflow { get; set; }
    }
}
