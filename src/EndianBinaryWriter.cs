using System;
using System.IO;

namespace IPod {

    internal class EndianBinaryWriter : BinaryWriter {

        private bool isbe;

        public EndianBinaryWriter (Stream stream, bool isbe) : base (stream) {
            this.isbe = isbe;
        }
        
        public override void Write (int val) {
            base.Write (Utility.MaybeSwap (BitConverter.GetBytes (val), isbe));
        }

        public override void Write (short val) {
            base.Write (Utility.MaybeSwap (BitConverter.GetBytes (val), isbe));
        }

        public override void Write (long val) {
            base.Write (Utility.MaybeSwap (BitConverter.GetBytes (val), isbe));
        }

        public override void Write (uint val) {
            Write ((int) val);
        }

        public override void Write (ushort val) {
            Write ((short) val);
        }
    }
}
