using System;

namespace IPod {

    internal class Utility {

        private static DateTime startDate = DateTime.Parse ("1/1/1904");

        public static uint DateToMacTime (DateTime date) {
            TimeSpan span = date - startDate;
            return (uint) span.TotalSeconds;
        }

        public static DateTime MacTimeToDate (uint time) {
            return startDate + TimeSpan.FromSeconds (time);
        }
        
        public static short ReverseByteOrder(short bytes) {
            return System.Net.IPAddress.NetworkToHostOrder (bytes);
        }
    }
}
