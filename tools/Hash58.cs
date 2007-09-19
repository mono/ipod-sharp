using System;
using System.IO;
using System.Text;

namespace IPod
{
    public static class Hash58Test
    {
        public static void Main(string [] args)
        {
            if(args.Length != 2) {
                Console.WriteLine("Usage:");
                Console.WriteLine("  mono hash58.exe iTunesDBPath Firewire-ID");
                return;
            }

            string path = args[0];
            string firewire_id = args[1];

            byte [] hash = null;
            byte [] original = new byte[20];

            using(BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open))) {
                byte [] contents = new byte[reader.BaseStream.Length];
                reader.Read(contents, 0, contents.Length);
                
                Array.Copy(contents, 0x58, original, 0, 20);

                Zero(contents, 0x18, 8);
                Zero(contents, 0x32, 20);
                Zero(contents, 0x58, 20);

                hash = Hash58.GenerateHash(firewire_id, contents);
            }

            Console.WriteLine("Original Hash:  {0}", HashToString(original));
            Console.WriteLine("Generated Hash: {0}", HashToString(hash));
        }

        private static string HashToString(byte [] buffer)
        {
            StringBuilder builder = new StringBuilder();
            foreach(byte b in buffer) {
                builder.AppendFormat("{0:X2}", b);
            }
            return builder.ToString();
        }

        private static void Zero(byte [] buffer, int index, int length)
        {
            for(int i = index; i < index + length; i++) {
                buffer[i] = 0;
            }
        }
    }
}

