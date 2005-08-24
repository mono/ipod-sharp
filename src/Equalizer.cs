using System;
using System.IO;
using System.Collections;
using System.Text;

namespace IPod {

    public class Equalizer {

        private EqualizerRecord record;

        internal EqualizerRecord EqualizerRecord {
            get { return record; }
            set { record = value; }
        }

        public string Name {
            get { return record.PresetName; }
            set { record.PresetName = value; }
        }

        public int PreAmp {
            get { return record.PreAmp; }
            set { record.PreAmp = value; }
        }

        public int BandCount {
            get { return record.BandCount; }
        }

        // Some kind of better API would be nicer here.
        // Perhaps percentage values?
        public int[] BandValues {
            get { return record.BandValues; }
            set {
                if (value.Length != BandCount)
                    throw new InvalidOperationException ("array length must be equal to BandCount");

                // FIXME: maybe we should just normalize it to -1200/1200?
                foreach (int band in value) {
                    if (band < -1200 || band > 1200)
                        throw new InvalidOperationException ("band values must be between -1200 and 1200");
                }
                
                record.BandValues = value;
            }
        }
        
        internal Equalizer (EqualizerRecord record) {
            this.record = record;
        }
    }

    internal class EqualizerRecord {

        private static UnicodeEncoding encoding = new UnicodeEncoding (false, false);

        public string PresetName;
        public int PreAmp;
        public int BandCount = 5;
        public int[] BandValues;

        private int largeBandCount = 10;
        private byte[] largeBandData = new byte[40]; // ten 4 byte integers
        private string headerName = "pqed";
        private UInt16 unknownOne;

        private const int PresetNameLength = 510;
        
        public void Read (byte[] data) {
            headerName = Encoding.ASCII.GetString (data, 0, 4);

            // this is allegedly the length of the preset name
            // but that doesn't seem to be the case.  It contains values
            // anywhere between 8 and 14 that I've seen, and the preset name
            // is always 510 bytes.
            unknownOne = BitConverter.ToUInt16 (data, 4);
            PresetName = encoding.GetString (data, 6, PresetNameLength).Trim ((char) 0);
            PreAmp = BitConverter.ToInt32 (data, 6 + PresetNameLength);
            largeBandCount = BitConverter.ToInt32 (data, 10 + PresetNameLength);
            largeBandData = new byte[4 * largeBandCount];
            Array.Copy (data, 14 + PresetNameLength, largeBandData, 0, 4 * largeBandCount);

            int offset = 14 + PresetNameLength + (4 * largeBandCount);
            BandCount = BitConverter.ToInt32 (data, offset);
            BandValues = new int[BandCount];

            offset += 4;
            for (int i = 0; i < BandCount; i++) {
                BandValues[i] = BitConverter.ToInt32 (data, offset);
                offset += 4;
            }
            
        }

        public void Save (BinaryWriter writer) {
            writer.Write (Encoding.ASCII.GetBytes (headerName));
            writer.Write (unknownOne);

            byte[] nameBytes = encoding.GetBytes (PresetName);
            writer.Write (nameBytes, 0,
                          nameBytes.Length > PresetNameLength ? PresetNameLength : nameBytes.Length);

            // pad the name
            if (PresetNameLength > nameBytes.Length) {
                writer.Write (new byte[PresetNameLength - nameBytes.Length]);
            }

            writer.Write (PreAmp);
            writer.Write (largeBandCount);
            writer.Write (largeBandData);
            writer.Write (BandCount);

            for (int i = 0; i < BandCount; i++) {
                writer.Write (BandValues[i]);
            }
        }
    }

    internal class EqualizerContainerRecord {

        private int headerId;
        private int unknownOne;
        private int unknownTwo;
        private int eqSize;

        private ArrayList equalizers = new ArrayList ();

        public EqualizerRecord[] EqualizerRecords {
            get {
                return (EqualizerRecord[]) equalizers.ToArray (typeof (EqualizerRecord));
            }
        }

        public void Remove (EqualizerRecord record) {
            equalizers.Remove (record);
        }
        
        public void Add (EqualizerRecord record) {
            equalizers.Add (record);
        }
        
        public void Read (BinaryReader reader) {
            byte[] header = reader.ReadBytes (8);

            headerId = BitConverter.ToInt32 (header, 0);
            int headerSize = BitConverter.ToInt32 (header, 4);

            byte[] remainder = reader.ReadBytes (headerSize - 8);
            unknownOne = BitConverter.ToInt32 (remainder, 0);
            unknownTwo = BitConverter.ToInt32 (remainder, 4);
            int numEq = BitConverter.ToInt32 (remainder, 8);
            eqSize = BitConverter.ToInt32 (remainder, 12);

            for (int i = 0; i < numEq; i++) {
                EqualizerRecord eqrec = new EqualizerRecord ();
                eqrec.Read (reader.ReadBytes (eqSize));

                Add (eqrec);
            }
        }

        public void Save (BinaryWriter writer) {
            writer.Write (headerId);
            writer.Write (24 + Record.PadLength);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (equalizers.Count);
            writer.Write (eqSize);
            writer.Write (new byte[Record.PadLength]);

            foreach (EqualizerRecord eq in equalizers) {
                eq.Save (writer);
            }
        }
    }
}
