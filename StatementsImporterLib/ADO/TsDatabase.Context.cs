﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Entities : DbContext
    {
        public Entities(string connectionString)
            : base(connectionString)
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<tbl_Account> tbl_Account { get; set; }
        public virtual DbSet<tbl_AdminUnit> tbl_AdminUnit { get; set; }
        public virtual DbSet<tbl_CashflowRight> tbl_CashflowRight { get; set; }
        public virtual DbSet<tbl_Contact> tbl_Contact { get; set; }
        public virtual DbSet<tbl_Invoice> tbl_Invoice { get; set; }
        public virtual DbSet<tbl_Period> tbl_Period { get; set; }
        public virtual DbSet<tbl_Contract> tbl_Contract { get; set; }
        public virtual DbSet<tbl_CashflowClause> tbl_CashflowClause { get; set; }
        public virtual DbSet<tbl_CashflowInCashflow> tbl_CashflowInCashflow { get; set; }
        public virtual DbSet<tbl_Cashflow> tbl_Cashflow { get; set; }
    }
}
