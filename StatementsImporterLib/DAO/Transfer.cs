using StatementsImporterLib.Toolkit;

namespace StatementsImporterLib.DAO
{
    public class Transfer
    {
        private Company company;

        public Company Company
        {
            get { return company; }
            set { company = value; }
        }
        private string движениеДенежныхСредств;

        public string ВидДвижения
        {
            get { return движениеДенежныхСредств; }
            set { движениеДенежныхСредств = value; }
        }

        private string назначениеПлатежа;

        public string НазначениеПлатежа
        {
            get { return назначениеПлатежа; }
            set { назначениеПлатежа = value; }
        }

        private string коррСчёт;

        public string КоррСчёт
        {
            get { return коррСчёт; }
            set { коррСчёт = value; }
        }

        private Subconto субконто1 = new Subconto();

        public Subconto Субконто1
        {
            get { return субконто1; }
            set { субконто1 = value; }
        }

        private Subconto субконто2 = new Subconto();

        public Subconto Субконто2
        {
            get { return субконто2; }
            set { субконто2 = value; }
        }

        private Subconto субконто3 = new Subconto();

        public Subconto Субконто3
        {
            get { return субконто3; }
            set { субконто3 = value; }
        }

        private double приход;

        public double Приход
        {
            get { return приход; }
            set { приход = value; }
        }

        private double расход;

        public double Расход
        {
            get { return расход; }
            set { расход = value; }
        }

        private string валюта;

        public string Валюта
        {
            get { return валюта; }
            set { валюта = value; }
        }
        private double курс;

        public double Курс
        {
            get { return курс; }
            set { курс = value; }
        }
        public string НомерДокВходящий
        {
            get;
            set;
        }
    }
}