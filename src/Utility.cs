using System;
using System.Collections.Generic;

namespace IPod {

    internal class Utility {

        private static DateTime startDate = new DateTime(1904, 1, 1);

        public static uint DateToMacTime (DateTime date) {
            TimeSpan span = date - startDate;
            return (uint) span.TotalSeconds;
        }

        public static DateTime MacTimeToDate (uint time) {
            return startDate + TimeSpan.FromSeconds (time);
        }

        public static short Swap (short number) {
            return (short) ( ((number >> 8) & 0xFF) + ((number << 8) & 0xFF00) );
        }

        public static int Swap (int number) {
            byte b0 = (byte) ((number >> 24) & 0xFF);
            byte b1 = (byte) ((number >> 16) & 0xFF);
            byte b2 = (byte) ((number >> 8) & 0xFF);
            byte b3 = (byte) (number & 0xFF);
            return b0 + (b1 << 8) + (b2 << 16) + (b3 << 24);
        }

        public static long Swap (long number) {
            byte b0 = (byte) ((number >> 56) & 0xFF);
            byte b1 = (byte) ((number >> 48) & 0xFF);
            byte b2 = (byte) ((number >> 40) & 0xFF);
            byte b3 = (byte) ((number >> 32) & 0xFF);
            byte b4 = (byte) ((number >> 24) & 0xFF);
            byte b5 = (byte) ((number >> 16) & 0xFF);
            byte b6 = (byte) ((number >> 8) & 0xFF);
            byte b7 = (byte) (number & 0xFF);
            return (long) b0 + ((long) b1 << 8) + ((long) b2 << 16) + ((long) b3 << 24) + ((long) b4 << 32) + ((long) b5 << 40) + ((long) b6 << 48) + ((long) b7 << 56);
        }

        public static byte[] Swap (byte[] bytes) {
            byte[] val = new byte[bytes.Length];

            for (int i = 0; i < bytes.Length; i++) {
                val[i] = bytes[bytes.Length - i - 1];
            }

            return val;
        }

        public static short MaybeSwap (short val, bool isbe) {
            if ((BitConverter.IsLittleEndian && isbe) ||
                (!BitConverter.IsLittleEndian && !isbe)) {
                return Utility.Swap (val);
            } else {
                return val;
            }
        }

        public static int MaybeSwap (int val, bool isbe) {
            if ((BitConverter.IsLittleEndian && isbe) ||
                (!BitConverter.IsLittleEndian && !isbe)) {
                return Utility.Swap (val);
            } else {
                return val;
            }
        }

        public static long MaybeSwap (long val, bool isbe) {
            if ((BitConverter.IsLittleEndian && isbe) ||
                (!BitConverter.IsLittleEndian && !isbe)) {
                return Utility.Swap (val);
            } else {
                return val;
            }
        }

        public static byte[] MaybeSwap (byte[] val, bool isbe) {
            if ((BitConverter.IsLittleEndian && isbe) ||
                (!BitConverter.IsLittleEndian && !isbe)) {
                return Utility.Swap (val);
            } else {
                return val;
            }
        }
    }
}
