
using System;
using System.IO;
using System.Text;
using System.Collections;
using Mono.Unix;

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
    }

    internal abstract class Record {

        public const int PadLength = 2;

        public string Name;
        public int HeaderOne; // usually the size of this record
        public int HeaderTwo; // usually the size of this record + size of children

        public virtual void Read (DatabaseRecord db, BinaryReader reader) {
            this.Name = Encoding.ASCII.GetString (reader.ReadBytes (4));
            this.HeaderOne = reader.ReadInt32 ();
            this.HeaderTwo = reader.ReadInt32 ();
        }
        
        public abstract void Save (DatabaseRecord db, BinaryWriter writer);

        protected byte[] Concatenate (params byte[][] byteArrayList) {
            int size = 0;

            foreach (byte[] b in byteArrayList) {
                size += b.Length;
            }

            byte[] ret = new byte[size];

            int offset = 0;
            foreach (byte[] b in byteArrayList) {
                Array.Copy (b, 0, ret, offset, b.Length);
                offset += b.Length;
            }

            return ret;
        }
    }

    internal class GenericRecord : Record {

        private byte[] data;
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            data = reader.ReadBytes (this.HeaderTwo - 12);
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {
            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (this.HeaderOne);
            writer.Write (this.HeaderTwo);
            writer.Write (this.data);
        }
    }

    internal class PlaylistItemRecord : Record {

        private int unknownOne = 1;
        private int correlationId = 0;
        private DetailRecord detail;

        public int TrackId;
        public int Timestamp;
        public int Index;

        public int CorrelationId {
            get { return correlationId; }
            set {
                correlationId = value;
                detail.Position = value;
            }
        }

        public PlaylistItemRecord () {
            this.Name = "mhip";
            detail = new DetailRecord ();
            detail.Type = DetailType.Misc;
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            unknownOne = BitConverter.ToInt32 (body, 0);
            correlationId = BitConverter.ToInt32 (body, 4);
            Index = BitConverter.ToInt32 (body, 8);
            TrackId = BitConverter.ToInt32 (body, 12);
            Timestamp = BitConverter.ToInt32 (body, 16);
            
            detail.Read (db, reader);
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {
            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (32 + PadLength);
            writer.Write (32 + PadLength);
            writer.Write (unknownOne);
            writer.Write (correlationId);
            writer.Write (Index);
            writer.Write (TrackId);
            writer.Write (Timestamp);
            writer.Write (new byte[PadLength]);
            detail.Save (db, writer);
        }
    }

    internal class PlaylistRecord : Record {

        private int unknownOne;
        private int unknownTwo;
        private int unknownThree;

        private ArrayList dataObjects = new ArrayList (); //these are DetailRecord objects
        private ArrayList playlistItems = new ArrayList ();

        private DetailRecord nameRecord;
        
        public bool IsHidden;
        public int Timestamp;
        public int Id;

        public string PlaylistName {
            get { return nameRecord.Value; }
            set {
                if (nameRecord == null) {
                    nameRecord = new DetailRecord ();
                    nameRecord.Type = DetailType.Title;
                    dataObjects.Add (nameRecord);
                }
                
                nameRecord.Value = value;
            }
        }

        public PlaylistItemRecord[] Items {
            get {
                return (PlaylistItemRecord[]) playlistItems.ToArray (typeof (PlaylistItemRecord));
            }
        }

        public PlaylistRecord () {
            this.Name = "mhyp";
        }

        public bool RemoveItem (int trackid) {
            foreach (PlaylistItemRecord rec in playlistItems) {
                if (rec.TrackId == trackid) {
                    playlistItems.Remove (rec);
                    return true;
                }
            }

            return false;
        }

        public void AddItem (PlaylistItemRecord rec) {
            InsertItem (-1, rec);
        }
        
        public void InsertItem (int index, PlaylistItemRecord rec) {
            int maxidx = 0;
            foreach (PlaylistItemRecord item in playlistItems) {
                if (item.Index > maxidx)
                    maxidx = item.Index;
            }

            rec.Index = maxidx + 2;

            if (index < 0) {
                playlistItems.Add (rec);
            } else {
                playlistItems.Insert (index, rec);
            }
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numobjs = BitConverter.ToInt32 (body, 0);
            int numitems = BitConverter.ToInt32 (body, 4);
            int hiddenFlag = BitConverter.ToInt32 (body, 8);

            if (hiddenFlag == 1)
                IsHidden = true;

            Timestamp = BitConverter.ToInt32 (body, 12);
            Id = BitConverter.ToInt32 (body, 16);
            unknownOne = BitConverter.ToInt32 (body, 20);
            unknownTwo = BitConverter.ToInt32 (body, 24);
            unknownThree = BitConverter.ToInt32 (body, 28);

            dataObjects.Clear ();
            playlistItems.Clear ();

            for (int i = 0; i < numobjs; i++) {
                if (i == 0) {
                    nameRecord = new DetailRecord ();
                    nameRecord.Read (db, reader);
                    dataObjects.Add (nameRecord);
                } else {
                    GenericRecord rec = new GenericRecord ();
                    rec.Read (db, reader);
                    dataObjects.Add (rec);
                }
            }

            for (int i = 0; i < numitems; i++) {
                PlaylistItemRecord item = new PlaylistItemRecord ();
                item.Read (db, reader);
                playlistItems.Add (item);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new BinaryWriter (stream);

            foreach (Record dataobj in dataObjects) {
                dataobj.Save (db, childWriter);
            }

            foreach (PlaylistItemRecord item in playlistItems) {
                item.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();
            
            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (44 + PadLength);
            writer.Write (44 + PadLength + childDataLength);
            writer.Write (dataObjects.Count);
            writer.Write (playlistItems.Count);
            writer.Write (IsHidden ? 1 : 0);
            writer.Write (Timestamp);
            writer.Write (Id);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (unknownThree);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }
        

    internal class PlaylistListRecord : Record {

        private ArrayList playlists = new ArrayList ();

        public PlaylistRecord[] Playlists {
            get {
                return (PlaylistRecord[]) playlists.ToArray (typeof (PlaylistRecord));
            }
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            int numlists = this.HeaderTwo;

            reader.ReadBytes (this.HeaderOne - 12);

            playlists.Clear ();

            for (int i = 0; i < numlists; i++) {
                PlaylistRecord list = new PlaylistRecord ();
                list.Read (db, reader);
                playlists.Add (list);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {
            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (12 + PadLength);
            writer.Write (playlists.Count);
            writer.Write (new byte[PadLength]);

            foreach (PlaylistRecord rec in playlists) {
                rec.Save (db, writer);
            }
        }

        private int FindNextId () {
            int id = 0;
            foreach (PlaylistRecord record in playlists) {
                if (record.Id > id)
                    id = record.Id;
            }

            return id + 1;
        }

        public void AddPlaylist (PlaylistRecord record) {
            record.Id = FindNextId ();
            playlists.Add (record);
        }

        public void RemovePlaylist (PlaylistRecord record) {
            playlists.Remove (record);
        }
    }

    internal enum DetailType {
        Title = 1,
        Location = 2,
        Album = 3,
        Artist = 4,
        Genre = 5,
        Filetype = 6,
        EQ = 7,
        Comment = 8,
        Composer = 12,
        Grouping = 13,
        PodcastUrl = 15,
        PodcastUrl2 = 16,
        ChapterData = 17,
        PlaylistData = 50,
        PlaylistRules = 51,
        LibraryIndex = 52,
        Misc = 100
    }

    internal class DetailRecord : Record {

        private static UnicodeEncoding encoding = new UnicodeEncoding (false, false);

        private int unknownOne;
        private int unknownTwo;
        private int unknownThree;
        private byte[] chapterData;
        
        public DetailType Type;
        public string Value = String.Empty;
        public int Position = 1;

        public DetailRecord () {
            this.Name = "mhod";
            this.HeaderOne = 24; // this is always the value
        }

        public DetailRecord (DetailType type, string value) : this () {
            this.Type = type;
            this.Value = value;
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            if (Name != "mhod")
                throw new ApplicationException ("Invalid detail header name");

            byte[] body = reader.ReadBytes (this.HeaderTwo - 12);
            
            Type = (DetailType) BitConverter.ToInt32 (body, 0);

            if ((int) Type > 50 && Type != DetailType.Misc)
                throw new ApplicationException ("Unsupported detail type: " + Type);


            unknownOne = BitConverter.ToInt32 (body, 4);
            unknownTwo = BitConverter.ToInt32 (body, 8);
            
            if ((int) Type < 50) {
                if (Type == DetailType.PodcastUrl ||
                    Type == DetailType.PodcastUrl2) {
                    Value = Encoding.UTF8.GetString (body, 12, body.Length - 12);
                } else if (Type == DetailType.ChapterData) {
                    // ugh ugh ugh, just preserve it for now -- no parsing

                    chapterData = new byte[body.Length - 12];
                    Array.Copy (body, 12, chapterData, 0, body.Length - 12);
                } else {
                    
                    Position = BitConverter.ToInt32 (body, 12);

                    int strlen = 0;
                    int strenc = 0;
            
                    if ((int) Type < 50) {
                        // 'string' mhods       
                        strlen = BitConverter.ToInt32 (body, 16);
                        strenc = BitConverter.ToInt32 (body, 20); // 0 == UTF16, 1 == UTF8
                        unknownThree = BitConverter.ToInt32 (body, 24);
                    }
                    
                    // the strenc field is not what it was thought to be
                    // latest DBs have the field set to 1 even when the encoding
                    // is UTF-16. For now I'm just encoding as UTF-16
                    Value = encoding.GetString(body, 28, strlen);
                    if(Value.Length != strlen / 2)
                        Value = Encoding.UTF8.GetString(body, 28, strlen);
                }
            } else {
                Position = BitConverter.ToInt32 (body, 12);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (24);

            byte[] valbytes = null;

            if ((int) Type < 50) {
                if (Type == DetailType.PodcastUrl || Type == DetailType.PodcastUrl2) {
                    valbytes = Encoding.UTF8.GetBytes (Value);
                    writer.Write (24 + valbytes.Length);
                } else if (Type == DetailType.ChapterData) {
                    valbytes = chapterData;
                    writer.Write (24 + valbytes.Length);
                } else {
                    valbytes = encoding.GetBytes (Value);
                    writer.Write (40 + valbytes.Length);
                }
            } else if (Type == DetailType.Misc) {
                writer.Write (44);
            }

            writer.Write ((int) Type);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);

            if ((int) Type < 50) {
                if (Type == DetailType.PodcastUrl || Type == DetailType.PodcastUrl2 ||
                    Type == DetailType.ChapterData) {
                    writer.Write (valbytes);
                } else {
                    writer.Write (Position);
                    writer.Write (valbytes.Length);
                    writer.Write (0);
                    writer.Write (unknownThree);
                    writer.Write (valbytes);
                }
            } else if (Type == DetailType.Misc) {
                writer.Write (Position);
                writer.Write (new byte[16]); // just padding
            }
        }
    }

    internal enum TrackRecordType {
        MP3 = 0x101,
        AAC = 0x0
    }

    internal class TrackRecord : Record {

        private int unknownTwo = 0;
        private short unknownThree = 0;
        private short unknownFour;
        private int unknownFive;
        private int unknownSix = 0x472c4400;
        private int unknownSeven;
        private int unknownEight = 0x0000000c;
        private int unknownNine;
        private int unknownTen;
        private int playCountDup;
        private byte[] remainder = new byte[0];

        private ArrayList details = new ArrayList ();

        public int Id;
        public bool Hidden = false;
        public TrackRecordType Type = TrackRecordType.MP3;
        public byte CompilationFlag = 0;
        public byte Rating;
        public uint Date;
        public int Size;
        public int Length;
        public int TrackNumber = 1;
        public int TotalTracks = 1;
        public int Year;
        public int BitRate;
        public ushort SampleRate;
        public int Volume;
        public int StartTime;
        public int StopTime;
        public int SoundCheck;
        public int PlayCount;
        public uint LastPlayedTime;
        public int DiscNumber;
        public int TotalDiscs;
        public int UserId;
        public uint LastModifiedTime;
        public int BookmarkTime;
        public long DatabaseId;
        public byte Checked;
        public byte ApplicationRating;
        public short BPM;
        public short ArtworkCount;
        public int ArtworkSize;

        public DetailRecord[] Details {
            get { return (DetailRecord[]) details.ToArray (typeof (DetailRecord)); }
        }

        public TrackRecord () {
            this.Name = "mhit";
        }

        public void AddDetail (DetailRecord detail) {
            details.Add (detail);
        }

        public void RemoveDetail (DetailRecord detail) {
            details.Remove (detail);
        }

        public DetailRecord GetDetail (DetailType type) {
            foreach (DetailRecord detail in details) {
                if (detail.Type == type)
                    return detail;
            }

            DetailRecord rec = new DetailRecord ();
            rec.Type = type;
            AddDetail (rec);
            
            return rec;
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {

            base.Read (db, reader);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numDetails = BitConverter.ToInt32 (body, 0);
            Id = BitConverter.ToInt32 (body, 4);
            Hidden = BitConverter.ToInt32 (body, 8) == 1 ? false : true;
            unknownTwo = BitConverter.ToInt32 (body, 12);
            Type = (TrackRecordType) BitConverter.ToInt16 (body, 16);
            CompilationFlag = body[18];
            Rating = body[19];
            Date = BitConverter.ToUInt32 (body, 20);
            Size = BitConverter.ToInt32 (body, 24);
            Length = BitConverter.ToInt32 (body, 28);
            TrackNumber = BitConverter.ToInt32 (body, 32);
            TotalTracks = BitConverter.ToInt32 (body, 36);
            Year = BitConverter.ToInt32 (body, 40);
            BitRate = BitConverter.ToInt32 (body, 44);
            SampleRate = BitConverter.ToUInt16 (body, 48);
            unknownThree = BitConverter.ToInt16 (body, 50);
            Volume = BitConverter.ToInt32 (body, 52);
            StartTime = BitConverter.ToInt32 (body, 56);
            StopTime = BitConverter.ToInt32 (body, 60);
            SoundCheck = BitConverter.ToInt32 (body, 64);
            PlayCount = BitConverter.ToInt32 (body, 68);
            playCountDup = BitConverter.ToInt32 (body, 72);
            LastPlayedTime = BitConverter.ToUInt32 (body, 76);
            DiscNumber = BitConverter.ToInt32 (body, 80);
            TotalDiscs = BitConverter.ToInt32 (body, 84);
            UserId = BitConverter.ToInt32 (body, 88);
            LastModifiedTime = BitConverter.ToUInt32 (body, 92);
            BookmarkTime = BitConverter.ToInt32 (body, 96);
            DatabaseId = BitConverter.ToInt64 (body, 100);
            Checked = body[108];
            ApplicationRating = body[109];
            BPM = BitConverter.ToInt16 (body, 110);
            unknownFour = BitConverter.ToInt16 (body, 112);
            ArtworkCount = BitConverter.ToInt16 (body, 114);
            ArtworkSize = BitConverter.ToInt32 (body, 116);
            unknownFive = BitConverter.ToInt32 (body, 120);
            unknownSix = BitConverter.ToInt32 (body, 124);
            unknownSeven = BitConverter.ToInt32 (body, 128);
            unknownEight = BitConverter.ToInt32 (body, 132);
            unknownNine = BitConverter.ToInt32 (body, 136);
            unknownTen = BitConverter.ToInt32 (body, 140);

            if (body.Length > 144) {
                remainder = new byte[body.Length - 144];
                Array.Copy (body, 144, remainder, 0, body.Length - 144);
            }

            details.Clear ();

            for (int i = 0; i < numDetails; i++) {
                DetailRecord rec = new DetailRecord ();
                rec.Read (db, reader);
                details.Add (rec);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new BinaryWriter (stream);

            foreach (DetailRecord rec in details) {
                rec.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();
            
            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (156 + remainder.Length + PadLength);
            writer.Write (156 + remainder.Length + PadLength + childDataLength);

            writer.Write (details.Count);
            writer.Write (Id);
            writer.Write (Hidden ? 0 : 1);
            writer.Write (unknownTwo);
            writer.Write ((short) Type);
            writer.Write (CompilationFlag);
            writer.Write (Rating);
            writer.Write (Date);
            writer.Write (Size);
            writer.Write (Length);
            writer.Write (TrackNumber);
            writer.Write (TotalTracks);
            writer.Write (Year);
            writer.Write (BitRate);
            writer.Write (SampleRate);
            writer.Write (unknownThree);
            writer.Write (Volume);
            writer.Write (StartTime);
            writer.Write (StopTime);
            writer.Write (SoundCheck);
            writer.Write (PlayCount);
            writer.Write (playCountDup);
            writer.Write (LastPlayedTime);
            writer.Write (DiscNumber);
            writer.Write (TotalDiscs);
            writer.Write (UserId);
            writer.Write (LastModifiedTime);
            writer.Write (BookmarkTime);
            writer.Write (DatabaseId);
            writer.Write (Checked);
            writer.Write (ApplicationRating);
            writer.Write (BPM);
            writer.Write (unknownFour);
            writer.Write (ArtworkCount);
            writer.Write (ArtworkSize);
            writer.Write (unknownFive);
            writer.Write (unknownSix);
            writer.Write (unknownSeven);
            writer.Write (unknownEight);
            writer.Write (unknownNine);
            writer.Write (unknownTen);
            writer.Write (remainder);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal class TrackListRecord : Record {

        private ArrayList tracks;

        public TrackRecord[] Tracks {
            get { return (TrackRecord[]) tracks.ToArray (typeof (TrackRecord)); }
        }

        public void Remove (int id) {
            foreach (TrackRecord track in tracks) {
                if (track.Id == id) {
                    tracks.Remove (track);
                    break;
                }
            }
        }

        public void Add (TrackRecord track) {
            tracks.Add (track);
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {

            base.Read (db, reader);
            
            reader.ReadBytes (this.HeaderOne - 12);

            int trackCount = this.HeaderTwo;

            tracks = new ArrayList ();
            
            for (int i = 0; i < trackCount; i++) {
                TrackRecord rec = new TrackRecord ();
                rec.Read (db, reader);
                tracks.Add (rec);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new BinaryWriter (stream);

            foreach (TrackRecord rec in tracks) {
                rec.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();

            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (12 + PadLength);
            writer.Write (tracks.Count);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal enum DataSetIndex {
        Library = 1,
        Playlist = 2,
        Podcast = 3
    }

    internal class DataSetRecord : Record {

        public DataSetIndex Index;

        public TrackListRecord TrackList;
        public PlaylistListRecord PlaylistList;
        public PlaylistListRecord PodcastList;

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            if (this.Name != "mhsd")
                throw new ApplicationException ("Unknown header name: " + this.Name);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int idx = BitConverter.ToInt32 (body, 0);

            switch (idx) {
            case 1:
                this.TrackList = new TrackListRecord ();
                this.TrackList.Read (db, reader);
                break;
            case 2:
                this.PlaylistList = new PlaylistListRecord ();
                this.PlaylistList.Read (db, reader);
                break;
            case 3:
                this.PodcastList = new PlaylistListRecord ();
                this.PodcastList.Read (db, reader);
                break;
            default:
                throw new ApplicationException ("Can't handle dataset index: " + Index);
            }

            Index = (DataSetIndex) idx;
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            byte[] childData;
            int childDataLength;

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new BinaryWriter (stream);

            switch ((int) Index) {
            case 1:
                TrackList.Save (db, childWriter);
                break;
            case 2:
                PlaylistList.Save (db, childWriter);
                break;
            case 3:
                PodcastList.Save (db, childWriter);
                break;
            default:
                throw new ApplicationException ("Can't handle DataSet record index: " + Index);
            }

            childWriter.Flush ();
            childData = stream.GetBuffer ();
            childDataLength = (int) stream.Length;
            childWriter.Close ();

            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (16 + PadLength);
            writer.Write (16 + PadLength + childDataLength);
            writer.Write ((int) Index);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal class DatabaseRecord : Record {

        private int unknownOne = 1;
        private int unknownTwo = 2;

        private ArrayList datasets;

        public int Version;
        public int ChildrenCount;
        public long Id;

        public DataSetRecord this[DataSetIndex index] {
            get {
                foreach (DataSetRecord rec in datasets) {
                    if (rec.Index == index)
                        return rec;
                }

                return null;
            }
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            
            unknownOne = BitConverter.ToInt32 (body, 0);
            Version = BitConverter.ToInt32 (body, 4);
            ChildrenCount = BitConverter.ToInt32 (body, 8);
            Id = BitConverter.ToInt64 (body, 12);
            unknownTwo = BitConverter.ToInt32 (body, 20);

            if (Version > 13)
                throw new ApplicationException ("Detected unsupported database version: " + Version);
            
            datasets = new ArrayList ();

            for (int i = 0; i < ChildrenCount; i++) {
                DataSetRecord rec = new DataSetRecord ();
                rec.Read (this, reader);
                datasets.Add (rec);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {
            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new BinaryWriter (stream);

            foreach (DataSetRecord rec in datasets) {
                rec.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();

            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (36 + PadLength);
            writer.Write (36 + PadLength + childDataLength);

            writer.Write (unknownOne);
            writer.Write (Version);
            writer.Write (ChildrenCount);
            writer.Write (Id);
            writer.Write (unknownTwo);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }

    public class InsufficientSpaceException : ApplicationException {

        public InsufficientSpaceException (string format, params object[] args) :
            base (String.Format (format, args)) {
        }
    }

    public delegate void StatusHandler (SongDatabase db, Song currentSong,
                                        int completed, int total);

    public class SongDatabase {

        private const string MusicFolder = "ipod-sharp";

        private DatabaseRecord dbrec;

        private ArrayList songs = new ArrayList ();
        private ArrayList songsToAdd = new ArrayList ();
        private ArrayList songsToRemove = new ArrayList ();

        private ArrayList playlists = new ArrayList ();

        private Device device;

        public event EventHandler SaveStarted;
        public event StatusHandler SaveProgressChanged;
        public event EventHandler SaveEnded;

        private string SongDbPath {
            get { return device.MountPoint + "/iPod_Control/iTunes/iTunesDB"; }
        }

        private string MusicPath {
            get { return device.MountPoint + "/iPod_Control/Music/" + MusicFolder; }
        }

        private string PlayCountsPath {
            get { return device.MountPoint + "/iPod_Control/iTunes/Play Counts"; }
        }

        public Song[] Songs {
            get {
                return (Song[]) songs.ToArray (typeof (Song));
            }
        }

        public Playlist[] Playlists {
            get {
                lock (playlists) {
                    return (Playlist[]) playlists.ToArray (typeof (Playlist));
                }
            }
        }

        public Playlist OnTheGoPlaylist {
            get {
                lock (playlists) {
                    foreach (Playlist p in playlists) {
                        if (p.IsOnTheGo)
                            return p;
                    }

                    return null;
                }
            }
        }

        internal SongDatabase (Device device) {
            this.device = device;
            Reload ();
        }
        
        private void Clear () {
            dbrec = null;
            songs.Clear ();
            songsToAdd.Clear ();
            songsToRemove.Clear ();
            playlists.Clear ();
        }

        private void LoadPlayCounts () {
            if (!File.Exists (PlayCountsPath))
                return;
            
            using (BinaryReader reader = new BinaryReader (File.Open (PlayCountsPath, FileMode.Open))) {

                byte[] header = reader.ReadBytes (96);
                int entryLength = BitConverter.ToInt32 (header, 8);
                int numEntries = BitConverter.ToInt32 (header, 12);

                for (int i = 0; i < numEntries; i++) {
                    
                    byte[] entry = reader.ReadBytes (entryLength);
                    
                    (songs[i] as Song).playCount = BitConverter.ToInt32 (entry, 0);

                    uint lastPlayed = BitConverter.ToUInt32 (entry, 4);
                    if (lastPlayed > 0) {
                        (songs[i] as Song).Track.LastPlayedTime = lastPlayed;
                    }

                    // if it has rating info, get it
                    if (entryLength >= 16) {
                        // Why is this one byte in iTunesDB and 4 here?
                        
                        int rating = BitConverter.ToInt32 (entry, 12);
                        (songs[i] as Song).Track.Rating  = (byte) rating;
                    }
                }
            }
        }

        private void LoadOnTheGo () {
            string path = device.MountPoint + "/iPod_Control/iTunes/OTGPlaylistInfo";

            if (!File.Exists (path)) {
                // add a blank one
                playlists.Add (new Playlist (this, new Song[0]));
                return;
            }
            
            ArrayList otgsongs = new ArrayList ();
            
            using (BinaryReader reader = new BinaryReader (File.Open (path, FileMode.Open))) {

                byte[] header = reader.ReadBytes (20);

                int numTracks = BitConverter.ToInt32 (header, 12);

                for (int i = 0; i < numTracks; i++) {
                    int index = reader.ReadInt32 ();

                    otgsongs.Add (songs[index]);
                }
            }

            Playlist otglist = new Playlist (this, (Song[]) otgsongs.ToArray (typeof (Song)));
            playlists.Add (otglist);
        }

        public void Reload () {

            lock (this) {

                using (BinaryReader reader = new BinaryReader (new FileStream (SongDbPath, FileMode.Open))) {

                    Clear ();
                    
                    dbrec = new DatabaseRecord ();
                    dbrec.Read (null, reader);

                    // Load the songs
                    foreach (TrackRecord track in dbrec[DataSetIndex.Library].TrackList.Tracks) {
                        Song song = new Song (this, track);
                        songs.Add (song);
                    }

                    // Load the play counts
                    LoadPlayCounts ();

                    // Load the playlists
                    foreach (PlaylistRecord listrec in dbrec[DataSetIndex.Playlist].PlaylistList.Playlists) {
                        if (listrec.IsHidden)
                            continue;
                        
                        Playlist list = new Playlist (this, listrec);
                        playlists.Add (list);
                    }

                    // Load the On-The-Go playlist
                    LoadOnTheGo ();
                }
            }
        }

        private string FormatSpace (UInt64 bytes) {
            return String.Format ("{0} MB", bytes/1024/1024);
        }

        private void CheckFreeSpace () {

            device.RescanDisk ();

            UInt64 available = device.VolumeAvailable;
            UInt64 required = 0;

            // we're going to free some up, so add that
            foreach (Song song in songsToRemove) {
                available += (UInt64) song.Size;
            }

            foreach (Song song in songsToAdd) {
                required += (UInt64) song.Size;
            }

            if (required >= available)
                throw new InsufficientSpaceException ("Not enough free space on '{0}'.  {1} required, " +
                                                      "but only {2} available.", device.Name,
                                                      FormatSpace (required), FormatSpace (available));
        }

        public void Save () {

            CheckFreeSpace ();

            if (SaveStarted != null)
                SaveStarted (this, new EventArgs ());

            // Back up the current song db
            File.Copy (SongDbPath, SongDbPath + ".bak", true);
            
            try {
                // Save the songs db
                using (BinaryWriter writer = new BinaryWriter (new FileStream (SongDbPath, FileMode.Create))) {
                    dbrec.Save (dbrec, writer);
                }
                
                foreach (Song song in songsToRemove) {
                    if (File.Exists (song.Filename))
                        File.Delete (song.Filename);
                }
                
                if (!Directory.Exists (MusicPath))
                    Directory.CreateDirectory (MusicPath);
                
                int completed = 0;
                
                foreach (Song song in songsToAdd) {
                    if (SaveProgressChanged != null)
                        SaveProgressChanged (this, song, completed++, songsToAdd.Count);
                    
                    string dest = GetFilesystemPath (song.Track.GetDetail (DetailType.Location).Value);

                    File.Copy (song.Filename, dest, true);
                }

                // The play count file is invalid now, so we'll remove it (even though the iPod would anyway)
                if (File.Exists (PlayCountsPath))
                    File.Delete (PlayCountsPath);

            } catch (Exception e) {
                // rollback the song db
                File.Copy (SongDbPath + ".bak", SongDbPath, true);
                throw e;
            } finally {
                if (SaveEnded != null)
                    SaveEnded (this, new EventArgs ());
            }
        }

        internal string GetFilesystemPath (string ipodPath) {
            if (ipodPath == null)
                return null;
            
            return device.MountPoint + ipodPath.Replace (":", "/");
        }

        internal string GetPodPath (string path) {
            if (path == null)
                return null;
            
            return String.Format (":iPod_Control:Music:{0}:{1}", MusicFolder, Path.GetFileName (path));
        }

        private int GetNextSongId () {
            int max = 0;

            foreach (TrackRecord track in dbrec[DataSetIndex.Library].TrackList.Tracks) {
                if (track.Id > max)
                    max = track.Id;
            }

            return max + 1;
        }

        private void AddSong (Song song, bool existing) {
            dbrec[DataSetIndex.Library].TrackList.Add (song.Track);

            PlaylistRecord library = dbrec[DataSetIndex.Playlist].PlaylistList.Playlists[0];

            PlaylistItemRecord item = new PlaylistItemRecord ();
            item.TrackId = song.Track.Id;
 
            library.AddItem (item);

            if (!existing)
                songsToAdd.Add (song);
            else if (songsToRemove.Contains (song))
                songsToRemove.Remove (song);
                
            songs.Add (song);
        }

        public void RemoveSong (Song song) {
            lock (this) {
                if (songs.Contains (song)) {
                    songs.Remove (song);

                    if (songsToAdd.Contains (song))
                        songsToAdd.Remove (song);
                    else
                        songsToRemove.Add (song);

                    // remove from the song db
                    dbrec[DataSetIndex.Library].TrackList.Remove (song.Id);

                    // remove from the playlists
                    foreach (PlaylistRecord list in dbrec[DataSetIndex.Playlist].PlaylistList.Playlists) {
                        list.RemoveItem (song.Id);
                    }
                }
            }
        }

        public Song CreateSong () {
            lock (this) {
                TrackRecord track = new TrackRecord ();
                track.Id = GetNextSongId ();
                track.Date = Utility.DateToMacTime (DateTime.Now);
                track.LastModifiedTime = track.Date;
                track.DatabaseId = (long) new Random ().Next ();
                
                Song song = new Song (this, track);

                AddSong (song, false);
                
                return song;
            }
        }

        public Playlist CreatePlaylist (string name) {
            if (name == null)
                throw new ArgumentException ("name cannot be null");
            
            PlaylistRecord playrec = new PlaylistRecord ();
            playrec.PlaylistName = name;
            
            dbrec[DataSetIndex.Playlist].PlaylistList.AddPlaylist (playrec);

            Playlist list = new Playlist (this, playrec);
            playlists.Add (list);
            return list;
        }

        public void RemovePlaylist (Playlist playlist) {
            lock (playlists) {
                if (playlist.IsOnTheGo)
                    throw new InvalidOperationException ("The On-The-Go playlist cannot be removed.");
                
                dbrec[DataSetIndex.Playlist].PlaylistList.RemovePlaylist (playlist.PlaylistRecord);
                playlists.Remove (playlist);
            }
        }

        public void RevertSong (Song song) {
            lock (this) {
                if (songsToAdd.Contains (song)) {
                    // song was going to be added, so don't do that
                    RemoveSong (song);
                } else if (songsToRemove.Contains (song)) {
                    // song was going to be removed, so don't do that
                    AddSong (song, true);
                }
            }
        }

        internal Song GetSongById (int id) {
            foreach (Song song in songs) {
                if (song.Id == id)
                    return song;
            }

            return null;
        }
    }
}
