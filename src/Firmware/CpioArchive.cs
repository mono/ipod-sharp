//
// CpioArchive.cs: Reads/Extracts files from an ASCII CPIO Archive
//
// Authors:
//   Aaron Bockover (aaron@abock.org)
//
// (C) 2006 Aaron Bockover
//
//  NOTE: Currently, attributes like mode, uid, gid, and
//  mtime are ignored when extracting entries.
//
//
//  CPIO ASCII File Entry Header
//
//  Field Width  Field Name  Meaning
//  --------------------------------------------------------------
//  6            magic       magic number "070707"
//  6            dev         device where file resides
//  6            ino         I-number of file
//  6            mode        file mode
//  6            uid         owner user ID
//  6            gid         owner group ID
//  6            nlink       number of links to file
//  6            rdev        device major/minor for special file
//  11           mtime       modify time of file
//  6            namesize    length of file name
//  11           filesize    length of file to follow
//

using System;
using System.IO;
using System.Text;
using System.Collections;

namespace IPod.Firmware
{
    public struct CpioFileEntry
    {
        internal short dev;
        internal short ino;
        internal short mode;
        internal short uid;
        internal short gid;
        internal short nlink;
        internal short rdev;
        internal int mtime;
        internal short namesize;
        internal int filesize;
        internal string filename;
        internal long cpio_offset;
        
        public short Device { get { return dev; } }
        public short Inumber { get { return ino; } }
        public short Uid { get { return uid; } }
        public short Gid { get { return gid; } }
        public short Nlinks { get { return nlink; } }
        public int Mtime { get { return mtime; } }
        public int FileSize { get { return filesize; } }
        public string FileName { get { return filename; } }
    }
    
    public class CpioArchive : IEnumerable
    {
        private Stream stream;
        private BinaryReader reader;
        private long total_size = 0;
        private ArrayList entries = new ArrayList(); 
        
        public CpioArchive(string cpioFile) : this(new FileStream(cpioFile, FileMode.Open))
        {
        }
        
        public CpioArchive(Stream cpioStream)
        {
            stream = cpioStream;
            reader = new BinaryReader(stream);
            
            ReadCpioEntries();
        }
        
        public void ExtractEntry(CpioFileEntry entry, string outFile)
        {
            ExtractEntry(entry, new FileStream(outFile, FileMode.Create));
        }
        
        public void ExtractEntry(CpioFileEntry entry, Stream outStream)
        {
            stream.Seek(entry.cpio_offset, SeekOrigin.Begin);
            
            byte [] block = new byte[4096];
            int blocks = entry.filesize / block.Length;
            int remainder = entry.filesize % block.Length;
            
            for(int i = 0; i < blocks; i++) {
                stream.Read(block, 0, block.Length);
                outStream.Write(block, 0, block.Length);
            }
            
            if(remainder > 0) {
                stream.Read(block, 0, remainder);
                outStream.Write(block, 0, remainder);
            }
        }
        
        public IEnumerator GetEnumerator()
        {
            return entries.GetEnumerator();
        }
        
        private void ReadCpioEntries()
        {
            stream.Seek(0, SeekOrigin.Begin);
            
            while(true) {
                CpioFileEntry entry = ReadFileEntry();
                
                if(entry.filesize == 0 && entry.filename == "TRAILER!!!") {
                    break;
                }

                entries.Add(entry);
                total_size += entry.filesize;
                
                stream.Seek(entry.filesize, SeekOrigin.Current);
            }
            
            stream.Seek(0, SeekOrigin.Begin);
        }
        
        private static int FromOctal(string number)
        {
            int result = 0;
            
            foreach(char digit in number) {
                if('0' <= digit && digit <= '7') {
                    result = 8 * result + (digit - '0');
                } else {
                    throw new FormatException();
                }
            }
            
            return result;
        }
        
        private short ReadFileEntryFieldShort()
        {
            string value = Encoding.ASCII.GetString(reader.ReadBytes(6));
            return (short)FromOctal(value);
        }
        
        private int ReadFileEntryFieldInt()
        {
            string value = Encoding.ASCII.GetString(reader.ReadBytes(11));
            return FromOctal(value);
        }
        
        private CpioFileEntry ReadFileEntry()
        {
            if(Encoding.ASCII.GetString(reader.ReadBytes(6)) != "070707") {
                throw new IOException("Invalid CPIO file header. Expected magic number field 070707.");
            }
        
            CpioFileEntry entry = new CpioFileEntry();
            
            entry.dev = ReadFileEntryFieldShort();
            entry.ino = ReadFileEntryFieldShort();
            entry.mode = ReadFileEntryFieldShort();
            entry.uid = ReadFileEntryFieldShort();
            entry.gid = ReadFileEntryFieldShort();
            entry.nlink = ReadFileEntryFieldShort();
            entry.rdev = ReadFileEntryFieldShort();
            entry.mtime = ReadFileEntryFieldInt();
            entry.namesize = ReadFileEntryFieldShort();
            entry.filesize = ReadFileEntryFieldInt();
            
            entry.filename = Encoding.UTF8.GetString(reader.ReadBytes(entry.namesize - 1));
            reader.ReadByte();
            
            entry.cpio_offset = stream.Position;
            
            return entry;
        }
        
        public long TotalSize {
            get { return total_size; }
        }
        
        public int FileCount {
            get { return entries.Count; }
        }
    }
}
