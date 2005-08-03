
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

        public const int PadLength = 8;

        public string Name;
        public int HeaderOne; // usually the size of this record
        public int HeaderTwo; // usually the size of this record + size of children

        protected void Read (DatabaseRecord db, BinaryReader reader, string expectedName) {
            this.Name = Encoding.ASCII.GetString (reader.ReadBytes (4));

            if (expectedName != null && this.Name != expectedName) {
                throw new DatabaseReadException ("Expected record name of '{0}', got '{1}'", expectedName, this.Name);
            }
            
            this.HeaderOne = reader.ReadInt32 ();
            this.HeaderTwo = reader.ReadInt32 ();
        }
        
        public virtual void Read (DatabaseRecord db, BinaryReader reader) {
            Read (db, reader, null);
        }
        
        public abstract void Save (DatabaseRecord db, BinaryWriter writer);

        protected void SaveChild (DatabaseRecord db, Record record, out byte[] data, out int length) {
            MemoryStream stream = new MemoryStream ();
            BinaryWriter writer = new BinaryWriter (stream);
            record.Save (db, writer);
            writer.Flush ();
            length = (int) stream.Length;
            data = stream.GetBuffer ();
            writer.Close ();
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

        private int unknownOne = 0;
        private int unknownTwo = 0;
        private ArrayList details = new ArrayList ();

        public int TrackId;
        public int Timestamp;

        public PlaylistItemRecord () {
            this.Name = "mhip";
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader, this.Name);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numDataObjects = BitConverter.ToInt32 (body, 0);
            unknownOne = BitConverter.ToInt32 (body, 4);
            unknownTwo = BitConverter.ToInt32 (body, 8);
            TrackId = BitConverter.ToInt32 (body, 12);
            Timestamp = BitConverter.ToInt32 (body, 16);

            for (int i = 0; i < numDataObjects; i++) {
                DetailRecord detail = new DetailRecord ();
                detail.Read (db, reader);
                details.Add (detail);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            // we need to create this for new playlist items
            if (details.Count == 0) {
                DetailRecord detail = new DetailRecord ();
                detail.Type = DetailType.Misc;
                details.Add (detail);
            }

            int childrenLength = 0;
            byte[] childrenData = new byte[0];
            
            foreach (DetailRecord child in details) {
                int childLength = 0;
                byte[] childData = null;
                
                SaveChild (db, child, out childData, out childLength);
                childrenLength += childLength;

                byte[] newChildrenData = new byte[childrenData.Length + childData.Length];
                Array.Copy (childrenData, 0, newChildrenData, 0, childrenData.Length);
                Array.Copy (childData, 0, newChildrenData, childrenData.Length, childData.Length);
                childrenData = newChildrenData;
            }

            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (32 + PadLength);
            
            // as of version 13, the detail record counts as a child
            if (db.Version >= 13) {
                writer.Write (32 + childrenLength + PadLength);
            } else {
                writer.Write (32 + PadLength);
            }

            writer.Write (details.Count);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (TrackId);
            writer.Write (Timestamp);
            writer.Write (new byte[PadLength]);
            writer.Write (childrenData, 0, childrenLength);
        }
    }

    internal class PlaylistRecord : Record {

        private int unknownOne;
        private int unknownTwo;
        private int unknownThree;
        private bool isLibrary;

        private ArrayList stringDetails = new ArrayList ();
        private ArrayList otherDetails = new ArrayList ();
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
                    stringDetails.Add (nameRecord);
                }
                
                nameRecord.Value = value;
            }
        }

        public PlaylistItemRecord[] Items {
            get {
                return (PlaylistItemRecord[]) playlistItems.ToArray (typeof (PlaylistItemRecord));
            }
        }

        public PlaylistRecord (bool isLibrary) {
            this.isLibrary = isLibrary;
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
            if (index < 0) {
                playlistItems.Add (rec);
            } else {
                playlistItems.Insert (index, rec);
            }
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader, this.Name);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numdetails = BitConverter.ToInt32 (body, 0);
            int numitems = BitConverter.ToInt32 (body, 4);
            int hiddenFlag = BitConverter.ToInt32 (body, 8);

            if (hiddenFlag == 1)
                IsHidden = true;

            Timestamp = BitConverter.ToInt32 (body, 12);
            Id = BitConverter.ToInt32 (body, 16);
            unknownOne = BitConverter.ToInt32 (body, 20);

            stringDetails.Clear ();
            otherDetails.Clear ();
            playlistItems.Clear ();

            for (int i = 0; i < numdetails; i++) {
                if (i == 0) {
                    nameRecord = new DetailRecord ();
                    nameRecord.Read (db, reader);
                    stringDetails.Add (nameRecord);
                } else if (isLibrary) {
                    DetailRecord rec = new DetailRecord ();
                    rec.Read (db, reader);
                    otherDetails.Add (rec);
                } else {
                    GenericRecord rec = new GenericRecord ();
                    rec.Read (db, reader);
                    otherDetails.Add (rec);
                }
            }

            for (int i = 0; i < numitems; i++) {
                PlaylistItemRecord item = new PlaylistItemRecord ();
                item.Read (db, reader);
                playlistItems.Add (item);
            }
        }

        private void CreateLibraryIndices () {
            // remove any existing library index records
            foreach (Record rec in (ArrayList) otherDetails.Clone ()) {
                DetailRecord detail = rec as DetailRecord;
                if (detail != null && detail.Type == DetailType.LibraryIndex) {
                    Console.WriteLine ("Removing index for: " + detail.IndexType);
                    otherDetails.Remove (rec);
                }
            }

            // TODO: actually create the new indices
        }
        
        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            if (isLibrary) {
                CreateLibraryIndices ();
            }
            
            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new BinaryWriter (stream);

            foreach (Record rec in stringDetails) {
                rec.Save (db, childWriter);
            }

            foreach (Record rec in otherDetails) {
                rec.Save (db, childWriter);
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
            writer.Write (stringDetails.Count + otherDetails.Count);
            writer.Write (playlistItems.Count);
            writer.Write (IsHidden ? 1 : 0);
            writer.Write (Timestamp);
            writer.Write (Id);
            writer.Write (unknownOne);
            writer.Write (stringDetails.Count);
            writer.Write (otherDetails.Count);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }
        

    internal class PlaylistListRecord : Record, IEnumerable {

        private ArrayList playlists = new ArrayList ();

        public PlaylistRecord this[int index] {
            get {
                return (PlaylistRecord) playlists[index];
            }
        }

        public PlaylistRecord[] Playlists {
            get {
                return (PlaylistRecord[]) playlists.ToArray (typeof (PlaylistRecord));
            }
        }

        public IEnumerator GetEnumerator () {
            return playlists.GetEnumerator ();
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader, "mhlp");

            int numlists = this.HeaderTwo;

            reader.ReadBytes (this.HeaderOne - 12);

            playlists.Clear ();

            for (int i = 0; i < numlists; i++) {
                bool isLibrary = false;
                
                if (i == 0)
                    isLibrary = true;
                
                PlaylistRecord list = new PlaylistRecord (isLibrary);
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
        Category = 9,
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

    internal enum IndexType {
        Song = 3,
        Album = 4,
        Artist = 5,
        Genre = 7,
        Composer = 18
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

        public IndexType IndexType;
        public int[] LibraryIndices;

        public DetailRecord () {
            this.Name = "mhod";
            this.HeaderOne = 24; // this is always the value for mhods
        }

        public DetailRecord (DetailType type, string value) : this () {
            this.Type = type;
            this.Value = value;
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader, "mhod");

            byte[] body = reader.ReadBytes (this.HeaderTwo - 12);
            
            Type = (DetailType) BitConverter.ToInt32 (body, 0);

            if ((int) Type > 50 && Type != DetailType.Misc && Type != DetailType.LibraryIndex)
                throw new DatabaseReadException ("Unsupported detail type: " + Type);

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
            } else if (Type == DetailType.LibraryIndex) {
                IndexType = (IndexType) BitConverter.ToInt32 (body, 12);

                int numEntries = BitConverter.ToInt32 (body, 16);

                ArrayList entries = new ArrayList ();
                
                for (int i = 0; i < numEntries; i++) {
                    int entry = BitConverter.ToInt32 (body, 56 + (i * 4));
                    entries.Add (entry);
                }

                LibraryIndices = (int[]) entries.ToArray (typeof (int));
            } else if (Type == DetailType.Misc) {
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
            } else if (Type == DetailType.LibraryIndex) {
                writer.Write (72 + (4 * LibraryIndices.Length));
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
            } else if (Type == DetailType.LibraryIndex) {
                writer.Write ((int) IndexType);
                writer.Write (LibraryIndices.Length);
                writer.Write (new byte[40]);

                foreach (int index in LibraryIndices) {
                    writer.Write (index);
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

        private short unknownThree = 0;
        private short unknownFour;
        private int unknownFive;
        private int unknownSix = 0x472c4400;
        private int unknownSeven;
        private int unknownEight = 0x0000000c;
        private int unknownNine;
        private int unknownTen;
        private int playCountDup;

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

            base.Read (db, reader, this.Name);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numDetails = BitConverter.ToInt32 (body, 0);
            Id = BitConverter.ToInt32 (body, 4);
            Hidden = BitConverter.ToInt32 (body, 8) == 1 ? false : true;
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
            unknownThree = BitConverter.ToInt16 (body, 50);
            SampleRate = BitConverter.ToUInt16 (body, 48);
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
            ArtworkCount = BitConverter.ToInt16 (body, 114);
            unknownFour = BitConverter.ToInt16 (body, 112);
            ArtworkSize = BitConverter.ToInt32 (body, 116);
            unknownFive = BitConverter.ToInt32 (body, 120);
            unknownSix = BitConverter.ToInt32 (body, 124);
            unknownSeven = BitConverter.ToInt32 (body, 128);
            unknownEight = BitConverter.ToInt32 (body, 132);
            unknownNine = BitConverter.ToInt32 (body, 136);
            unknownTen = BitConverter.ToInt32 (body, 140);

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

            int len;
            if (db.Version >= 12) {
                len = 244;
            } else {
                len = 156;
            }
            
            writer.Write (Encoding.ASCII.GetBytes (this.Name));
            writer.Write (len);
            writer.Write (len + childDataLength);

            writer.Write (details.Count);
            writer.Write (Id);
            writer.Write (Hidden ? 0 : 1);

            switch (Type) {
            case TrackRecordType.MP3:
                writer.Write (new char[] { 'M', 'P', '3', ' ' });
                break;
            case TrackRecordType.AAC:
                writer.Write (new char[] { 'A', 'A', 'C', ' ' });
                break;
            default:
                writer.Write ((Int32) 0);
                break;
            }
            
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
            writer.Write (unknownThree);
            writer.Write (SampleRate);
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
            writer.Write (ArtworkCount);
            writer.Write (unknownFour);
            writer.Write (ArtworkSize);
            writer.Write (unknownFive);
            writer.Write (unknownSix);
            writer.Write (unknownSeven);
            writer.Write (unknownEight);
            writer.Write (unknownNine);
            writer.Write (unknownTen);

            if (db.Version >= 12) {
                writer.Write (new byte[88]);
            }
            
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal class TrackListRecord : Record, IEnumerable {

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

        public IEnumerator GetEnumerator () {
            return tracks.GetEnumerator ();
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {

            base.Read (db, reader, "mhlt");
            
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

        public PlaylistRecord Library {
            get {
                if (PlaylistList != null) {
                    return PlaylistList[0];
                }

                return null;
            }
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader, "mhsd");

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int idx = BitConverter.ToInt32 (body, 0);

            switch (idx) {
            case 1:
                this.TrackList = new TrackListRecord ();
                this.TrackList.Read (db, reader);
                break;
            case 2:
            case 3:
                this.PlaylistList = new PlaylistListRecord ();
                this.PlaylistList.Read (db, reader);
                break;
            default:
                throw new DatabaseReadException ("Can't handle dataset index: " + Index);
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
            case 3:
                PlaylistList.Save (db, childWriter);
                break;
            default:
                throw new DatabaseReadException ("Can't handle DataSet record index: " + Index);
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
            base.Read (db, reader, "mhbd");

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            
            unknownOne = BitConverter.ToInt32 (body, 0);
            Version = BitConverter.ToInt32 (body, 4);
            ChildrenCount = BitConverter.ToInt32 (body, 8);
            Id = BitConverter.ToInt64 (body, 12);
            unknownTwo = BitConverter.ToInt32 (body, 20);

            if (Version > 13)
                throw new DatabaseReadException ("Detected unsupported database version {0}", Version);
            
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

    public delegate void SaveProgressHandler (SongDatabase db, Song currentSong, double currentPercent,
                                              int completed, int total);

    public class SongDatabase {

        private const int CopyBufferSize = 8192;
        private const double PercentThreshold = 0.10;
        
        private DatabaseRecord dbrec;

        private ArrayList songs = new ArrayList ();
        private ArrayList songsToAdd = new ArrayList ();
        private ArrayList songsToRemove = new ArrayList ();

        private ArrayList playlists = new ArrayList ();
        private Playlist otgPlaylist;
        private Playlist podcastPlaylist;
        
        private Random random = new Random();
        private Device device;

        public event EventHandler SaveStarted;
        public event SaveProgressHandler SaveProgressChanged;
        public event EventHandler SaveEnded;

        private string SongDbPath {
            get { return device.MountPoint + "/iPod_Control/iTunes/iTunesDB"; }
        }

        private string MusicBasePath {
            get { return device.MountPoint + "/iPod_Control/Music"; }
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
            get { return otgPlaylist; }
        }

        public Playlist PodcastPlaylist {
            get { return podcastPlaylist; }
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
                // make a blank one
                otgPlaylist = new Playlist (this, new Song[0]);
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

            otgPlaylist = new Playlist (this, (Song[]) otgsongs.ToArray (typeof (Song)));
        }

        public void Reload () {

            lock (this) {

                using (BinaryReader reader = new BinaryReader (new FileStream (SongDbPath, FileMode.Open))) {

                    Clear ();
                    
                    dbrec = new DatabaseRecord ();
                    dbrec.Read (null, reader);

                    // Load the songs
                    foreach (TrackRecord track in dbrec[DataSetIndex.Library].TrackList) {
                        Song song = new Song (this, track);
                        songs.Add (song);
                    }

                    // Load the play counts
                    LoadPlayCounts ();

                    // Load the playlists
                    foreach (PlaylistRecord listrec in dbrec[DataSetIndex.Playlist].PlaylistList) {
                        if (listrec.IsHidden)
                            continue;
                        
                        Playlist list = new Playlist (this, listrec);
                        playlists.Add (list);
                    }

                    // Load the On-The-Go playlist
                    LoadOnTheGo ();

                    // Load the Podcast playlist
                    if (dbrec[DataSetIndex.Podcast] != null) {
                        podcastPlaylist = new Playlist (this, dbrec[DataSetIndex.Podcast].Library);
                    }
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

        
        private string MakeUniquePodSongPath(string filename) {
            const string allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            
            string basePath = MusicBasePath + "/";
            string uniqueName = String.Empty;
            string ext = (new FileInfo(filename)).Extension.ToLower();
            
            do {
                uniqueName = String.Format("F{0:00}/", random.Next(50));
                
                if(!Directory.Exists(basePath + uniqueName))
                    Directory.CreateDirectory(basePath + uniqueName);
                
                for(int i = 0; i < 4; i++)
                    uniqueName += allowed[random.Next(allowed.Length)];

                uniqueName += ext;
            } while(File.Exists(basePath + uniqueName));
				
            return uniqueName.Replace("/", ":");
        }

        private void CopySong (Song song, string dest, int completed, int total) {
            BinaryReader reader = null;
            BinaryWriter writer = null;
            
            try {
                FileInfo info = new FileInfo (song.Filename);
                long length = info.Length;
                long count = 0;
                double lastPercent = 0.0;

                reader = new BinaryReader (new BufferedStream (File.Open (song.Filename, FileMode.Open)));
                writer = new BinaryWriter (new BufferedStream (File.Open (dest, FileMode.Create)));
                
                do {
                    byte[] buf = reader.ReadBytes (CopyBufferSize);
                    writer.Write (buf);
                    count += buf.Length;

                    double percent = (double) count / (double) length;
                    if (percent >= lastPercent + PercentThreshold && SaveProgressChanged != null) {
                        SaveProgressChanged (this, song, (double) count / (double) length, completed, total);
                        lastPercent = percent;
                    }
                } while (count < length);
            } finally {
                if (reader != null)
                    reader.Close ();

                if (writer != null)
                    writer.Close ();
            }
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
                
                if (!Directory.Exists (MusicBasePath))
                    Directory.CreateDirectory (MusicBasePath);
                
                int completed = 0;
                
                foreach (Song song in songsToAdd) {
                    string dest = GetFilesystemPath (song.Track.GetDetail (DetailType.Location).Value);

                    CopySong (song, dest, completed++, songsToAdd.Count);
                }

                // The play count file is invalid now, so we'll remove it (even though the iPod would anyway)
                if (File.Exists (PlayCountsPath))
                    File.Delete (PlayCountsPath);

            } catch (Exception e) {
                // rollback the song db
                File.Copy (SongDbPath + ".bak", SongDbPath, true);
                throw new DatabaseWriteException (e, "Failed to save database");
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
            
            return ":iPod_Control:Music:" + MakeUniquePodSongPath(path);
        }

        private int GetNextSongId () {
            int max = 0;

            foreach (TrackRecord track in dbrec[DataSetIndex.Library].TrackList) {
                if (track.Id > max)
                    max = track.Id;
            }

            return max + 1;
        }

        private void AddSong (Song song, bool existing) {
            dbrec[DataSetIndex.Library].TrackList.Add (song.Track);

            PlaylistItemRecord item = new PlaylistItemRecord ();
            item.TrackId = song.Track.Id;
 
            dbrec[DataSetIndex.Playlist].Library.AddItem (item);

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
                    dbrec[DataSetIndex.Playlist].Library.RemoveItem (song.Track.Id);
                    
                    // remove from the "normal" playlists
                    foreach (Playlist list in playlists) {
                        list.RemoveSong (song);
                    }

                    // remove from On-The-Go playlist
                    otgPlaylist.RemoveOTGSong (song);

                    // remove from podcast playlist
                    if (podcastPlaylist != null) {
                        podcastPlaylist.RemoveSong (song);
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
            
            PlaylistRecord playrec = new PlaylistRecord (false);
            playrec.PlaylistName = name;
            
            dbrec[DataSetIndex.Playlist].PlaylistList.AddPlaylist (playrec);

            Playlist list = new Playlist (this, playrec);
            playlists.Add (list);
            return list;
        }

        public void RemovePlaylist (Playlist playlist) {
            lock (playlists) {
                if (playlist == null) {
                    throw new InvalidOperationException ("playist is null");
                } else if (playlist.IsOnTheGo) {
                    throw new InvalidOperationException ("The On-The-Go playlist cannot be removed.");
                } else if (playlist == podcastPlaylist) {
                    throw new InvalidOperationException ("The Podcast playlist cannot be removed.");
                }
                
                dbrec[DataSetIndex.Playlist].PlaylistList.RemovePlaylist (playlist.PlaylistRecord);
                playlists.Remove (playlist);
            }
        }

        public Playlist LookupPlaylist (string name) {
            lock (playlists) {
                foreach (Playlist list in playlists) {
                    if (list.Name == name)
                        return list;
                }
            }

            return null;
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
