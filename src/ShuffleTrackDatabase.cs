using System;
using System.IO;
using System.Text;

namespace IPod {
    
    internal class ShuffleTrackDatabase  {
        
        public static bool Save(Device device) {
            
            if(device.Model != DeviceModel.Shuffle)
                return false;
        
            string sdbFile = device.ControlPath + "/iTunes/iTunesSD";
        
            using(BinaryWriter writer = new BinaryWriter(new FileStream(
                sdbFile, FileMode.Create))) {
                
                WriteHeader(writer, device.TrackDatabase.Tracks.Count);
                
                foreach(Track track in device.TrackDatabase.Tracks) {
                    WriteTrackEntry(writer, device, track);
                }
            }
            
            return true;
        }
        
        private static void WriteHeader(BinaryWriter writer, int trackCount) {
        
            // number of track entries in the file
            writer.Write((byte)0x00);
            writer.Write(Utility.Swap ((short)trackCount));
            
            // unknown (0x010600)
            writer.Write((byte)0x01);
            writer.Write((byte)0x06);
            writer.Write((byte)0x00);
            
            // size of header (0x000012, 18 byte header)
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);
            writer.Write((byte)0x12);
            
            // 9 bytes of 0 padding?
            writer.Write(new byte [9]);
        }
    
        private static void WriteTrackEntry(BinaryWriter writer, Device device, 
            Track track) {
        
            // size of entry 
            writer.Write((byte)0x00);
            writer.Write(Utility.Swap ((short)0x022e));
            
            // unknown (0x5aa501)
            writer.Write((byte)0x5a);
            writer.Write((byte)0xa5);
            writer.Write((byte)0x01);
            
            // start time in 256ms increments (60 seconds = 0xea)
            writer.Write(new byte [3]);
            // unknown (always 0?)
            writer.Write(new byte [3]);
            // unknown, assoc. with start time
            writer.Write(new byte [3]);
    
            // stop time in 256ms increments (60 seconds = 0xea)
            writer.Write(new byte [3]);
            // unknown (always 0?)
            writer.Write(new byte [3]);
            // unknown, assoc. with stop time
            writer.Write(new byte [3]);
            
            // volume (-100 to 0 to 100)
            writer.Write((byte)0x00);
            writer.Write(Utility.Swap ((short)0x0064));
            
            // file type (0x01 = MP3, 0x02 = AAC, 0x04 = WAV)
            writer.Write(new byte [2]);
            switch(track.Record.Type) {
                case TrackRecordType.MP3:
                    writer.Write((byte)0x01);
                    break;
                case TrackRecordType.AAC:
                default:
                    writer.Write((byte)0x02);
                    break;
            }
                    
            // unknown (0x000200)
            writer.Write((byte)0x00);
            writer.Write((byte)0x02);
            writer.Write((byte)0x00);
            
            // file name (UTF-16, record is 522 bytes)
            string file = track.FileName;
            if(file.StartsWith(device.MountPoint)) {
                file = file.Substring(device.MountPoint.Length);
            }
            
            byte [] filebytes = Encoding.Unicode.GetBytes(file);
            writer.Write(filebytes);
            writer.Write(new byte [522 - filebytes.Length]);
            
            // shuffle flag (0x00 to disable playback in shuffle mode)
            writer.Write((byte)0x01);
            
            // bookmark flag (0x00 to disable)
            writer.Write((byte)0x00);
            
            // unknown, always 0?
            writer.Write((byte)0x00);
        }
    }
}

