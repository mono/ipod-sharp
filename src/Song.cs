
using System;
using System.IO;

namespace IPod {

    public enum SongRating {
        Zero = 0,
        One = 20,
        Two = 40,
        Three = 60,
        Four = 80,
        Five = 100
    }
    
    public class Song {

        private TrackRecord record;
        private SongDatabase db;

        internal int playCount;
        internal DateTime lastPlayed;

        private string filename;
        
        public int Id {
            get { return record.Id; }
        }

        public string Filename {
            get {
                if (filename != null) {
                    return filename;
                } else {
                    DetailRecord detail = record.GetDetail (DetailType.Location);
                    return db.GetFilesystemPath (detail.Value);
                }
            } set {
                if (value == null)
                    throw new ArgumentException ("filename cannot be null");
                
                DetailRecord detail = record.GetDetail (DetailType.Location);
                detail.Value = db.GetPodPath (SanitizeFilename (value));

                FileInfo info = new FileInfo (value);
                record.Size = (int) info.Length;

                filename = value;

                if (filename.ToLower ().EndsWith ("mp3"))
                    record.Type = TrackRecordType.MP3;
                else
                    record.Type = TrackRecordType.AAC;
            }
        }

        public int Size {
            get { return record.Size; }
        }

        public int Length {
            get { return record.Length; }
            set { record.Length = value; }
        }

        public int TrackNumber {
            get { return record.TrackNumber; }
            set { record.TrackNumber = value; }
        }
        
        public int Year {
            get { return record.Year; }
            set { record.Year = value; }
        }
        
        public int BitRate {
            get { return record.BitRate; }
            set { record.BitRate = value; }
        }
        
        public ushort SampleRate {
            get { return record.SampleRate; }
            set { record.SampleRate = value; }
        }

        public string Title {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Title);
                return detail.Value;
            } set {
                DetailRecord detail = record.GetDetail (DetailType.Title);
                detail.Value = value;
            }
        }
        
        public string Artist {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Artist);
                return detail.Value;
            } set {
                DetailRecord detail = record.GetDetail (DetailType.Artist);
                detail.Value = value;
            }
        }
        
        public string Album {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Album);
                return detail.Value;
            } set {
                DetailRecord detail = record.GetDetail (DetailType.Album);
                detail.Value = value;
            }
        }
        
        public string Genre {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Genre);
                return detail.Value;
            } set {
                DetailRecord detail = record.GetDetail (DetailType.Genre);
                detail.Value = value;
            }
        }
        
        public string Comment {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Comment);
                return detail.Value;
            } set {
                DetailRecord detail = record.GetDetail (DetailType.Comment);
                detail.Value = value;
            }
        }

        public int PlayCount {
            get { return record.PlayCount; }
            set { record.PlayCount = value; }
        }

        public DateTime LastPlayed {
            get {
                if (record.LastPlayedTime > 0) {
                    return Utility.MacTimeToDate (record.LastPlayedTime);
                } else {
                    return DateTime.MinValue;
                }
            } set {
                record.LastPlayedTime = Utility.DateToMacTime (value);
            }
        }

        public SongRating Rating {
            get { return (SongRating) record.Rating; }
            set { record.Rating = (byte) value; }
        }

        internal SongDatabase Database {
            get { return db; }
        }

        internal TrackRecord Track {
            get { return record; }
        }

        internal Song (SongDatabase db, TrackRecord record) {
            this.db = db;
            this.record = record;
        }
        
        public override string ToString () {
            return String.Format ("({0}) {1} - {2} - {3} ({4})", Id, Artist, Album, Title, Filename);
        }

        public override bool Equals (object a) {
            Song song = a as Song;

            if (song == null)
                return false;

            return this.Id == song.Id;
        }

        public override int GetHashCode () {
            return Id.GetHashCode ();
        }

        private string SanitizeFilename (string path) {
            return path.Replace ('?', '_');
        }
    }
}
