using StatementsImporterLib.ADO;
using System.Collections.Generic;


namespace StatementsImporterLib.ADO
{
    public class BankStatement
    {
        private double guid;

        public double GUID
        {
            get { return guid; }
            set { guid = value; }
        }

        private string номерДок;

        public string НомерДок
        {
            get { return номерДок; }
            set { номерДок = value; }
        }
        public string Валюта {get;set;}        

        private string name;

        public string Наименование
        {
            get { return name; }
            set { name = value; }
        }

        private string date;

        public string ДатаДок
        {
            get { return date; }
            set { date = value; }
        }

        private List<Transfer> transfers = new List<Transfer>();

        public List<Transfer> Transfers
        {
            get { return transfers; }
        }

        private List<tbl_Cashflow> cashflows = new List<tbl_Cashflow>();

        public List<tbl_Cashflow> Cashflows
        {
            get { return cashflows; }            
        }
    }
}