using System;
using System.Globalization;

namespace IPod
{
    public abstract class ProductionInfo
    {
        private string serial_number;
        private string factory_id;
        private int number;
        private int week;
        private int year;
        
        protected ProductionInfo ()
        {
        }

        public string SerialNumber {
            get { return serial_number; }
            protected set { serial_number = value; }
        }
        
        public string FactoryId {
            get { return factory_id; }
            protected set { factory_id = value; }
        }
        
        public int Number {
            get { return number; }
            protected set { number = value; }
        }
        
        public int Week {
            get { return week; }
            protected set { week = value; }
        }
        
        public int Year {
            get { return year; }
            protected set { year = value; }
        }
        
        public DateTime Date {
            get { return CultureInfo.CurrentCulture.Calendar.AddWeeks(new DateTime(Year, 1, 1), Week); }
        }
        
        public string MonthName {
            get { return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Date.Month); }
        }
        
        public string DisplayDate {
            get { return String.Format("{0}, {1}", MonthName, Date.Year); }
        }
        
        public void Dump ()
        {
            Console.WriteLine ("  Serial Number:    {0}", SerialNumber);
            Console.WriteLine ("  Production Date:  {0}", DisplayDate);
            Console.WriteLine ("  Production Index: {0}", Number);
        }
    }
}
