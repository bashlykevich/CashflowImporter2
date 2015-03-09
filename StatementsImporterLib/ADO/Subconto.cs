namespace StatementsImporterLib.ADO
{
    internal enum SubcontoType { Контрагент, Договор, Выписка, Неопределено };

    public class Subconto
    {
        private string код = "";

        public string Код
        {
            get { return код; }
            set { код = value; }
        }

        private string наименование = "";

        public string Наименование
        {
            get { return наименование; }
            set { наименование = value; }
        }

        private SubcontoType типСубконто = SubcontoType.Неопределено;

        internal SubcontoType ТипСубконто
        {
            get { return типСубконто; }
            set { типСубконто = value; }
        }
    }
}