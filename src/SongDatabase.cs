using System;
using System.IO;
using System.Text;
using System.Collections;
using Mono.Unix;

namespace IPod {

    internal abstract class Record {

        public const int PadLength = 8;

        public string Name;
        public int HeaderOne; // usually the size of this record
        public int HeaderTwo; // usually the size of this record + size of children
        public bool IsBE = false;

        public Record (bool isbe) {
            this.IsBE = isbe;
        }

        public virtual void Read (DatabaseRecord db, BinaryReader reader) {
            byte[] nameBytes = reader.ReadBytes (4);
            if (IsBE)
                nameBytes = Utility.Swap (nameBytes);
            
            string n = Encoding.ASCII.GetString (nameBytes);

            if (this.Name != null && this.Name != n) {
                throw new DatabaseReadException ("Expected record name of '{0}', got '{1}'", this.Name, n);
            }

            this.Name = n;
            this.HeaderOne = reader.ReadInt32 ();
            this.HeaderTwo = reader.ReadInt32 ();

            if (IsBE) {
                this.HeaderOne = Utility.Swap (this.HeaderOne);
                this.HeaderTwo = Utility.Swap (this.HeaderTwo);
            }
        }

        protected void WriteName (BinaryWriter writer) {
            byte[] nameBytes = Encoding.ASCII.GetBytes (this.Name);
            if (IsBE)
                nameBytes = Utility.Swap (nameBytes);
            
            writer.Write (nameBytes);
        }

        public long ToInt64 (byte[] buf, int offset) {
            return MaybeSwap (BitConverter.ToInt64 (buf, offset));
        }

        public int ToInt32 (byte[] buf, int offset) {
            return MaybeSwap (BitConverter.ToInt32 (buf, offset));
        }

        public uint ToUInt32 (byte[] buf, int offset) {
            return (uint) ToInt32 (buf, offset);
        }

        public short ToInt16 (byte[] buf, int offset) {
            return MaybeSwap (BitConverter.ToInt16 (buf, offset));
        }

        public ushort ToUInt16 (byte[] buf, int offset) {
            return (ushort) ToInt16 (buf, offset);
        }

        public short MaybeSwap (short val) {
            return Utility.MaybeSwap (val, IsBE);
        }

        public int MaybeSwap (int val) {
            return Utility.MaybeSwap (val, IsBE);
        }

        public long MaybeSwap (long val) {
            return Utility.MaybeSwap (val, IsBE);
        }

        public byte[] MaybeSwap (byte[] val) {
            return Utility.MaybeSwap (val, IsBE);
        }

        public abstract void Save (DatabaseRecord db, BinaryWriter writer);

        protected void SaveChild (DatabaseRecord db, Record record, out byte[] data, out int length) {
            MemoryStream stream = new MemoryStream ();
            BinaryWriter writer = new EndianBinaryWriter (stream, IsBE);
            record.Save (db, writer);
            writer.Flush ();
            length = (int) stream.Length;
            data = stream.GetBuffer ();
            writer.Close ();
        }
    }

    internal class GenericRecord : Record {

        private byte[] data;

        public GenericRecord (bool isbe) : base (isbe) {
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            data = reader.ReadBytes (this.HeaderTwo - 12);
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {
            WriteName (writer);
            writer.Write (this.HeaderOne);
            writer.Write (this.HeaderTwo);
            writer.Write (this.data);
        }
    }

    internal class PlaylistItemRecord : Record {

        private int unknownOne = 0;
        private int unknownTwo = 0;
        private ArrayList details = new ArrayList ();
        private DetailRecord posrec;

        public int TrackId;
        public int Position {
            get { return posrec.Position; }
            set { posrec.Position = value; }
        }
        
        public int Timestamp;

        public PlaylistItemRecord (bool isbe) : base (isbe) {
            this.Name = "mhip";
            posrec = new DetailRecord (isbe);
            posrec.Type = DetailType.Misc;
            details.Add (posrec);
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numDataObjects = ToInt32 (body, 0);
            unknownOne = ToInt32 (body, 4);
            unknownTwo = ToInt32 (body, 8);
            TrackId = ToInt32 (body, 12);
            Timestamp = ToInt32 (body, 16);

            details.Clear ();

            for (int i = 0; i < numDataObjects; i++) {
                DetailRecord detail = new DetailRecord (IsBE);
                detail.Read (db, reader);
                details.Add (detail);

                // it's possible there will be more than one of these, but the last one
                // should always be the "right" one.  Sigh.
                if (detail.Type == DetailType.Misc) {
                    posrec = detail;
                }
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

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

            WriteName (writer);
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

    internal enum SortOrder : int {
        Manual = 1,
        Unknown,
        Title,
        Album,
        Artist,
        BitRate,
        Genre,
        Kind,
        DateModified,
        Track,
        Size,
        Time,
        Year,
        SampleRate,
        Comment,
        DateAdded,
        Equalizer,
        Composer,
        Unknown2,
        PlayCount,
        LastPlayed,
        Disc,
        Rating,
        ReleaseDate,
        Bpm,
        Grouping,
        Category,
        Description
    }

    internal class PlaylistRecord : Record {

        private int unknownOne;
        //private int unknownTwo;
        //private int unknownThree;
        private bool isLibrary;

        private ArrayList stringDetails = new ArrayList ();
        private ArrayList otherDetails = new ArrayList ();
        private ArrayList playlistItems = new ArrayList ();

        private DetailRecord nameRecord;
        
        public bool IsHidden;
        public int Timestamp;
        public int Id;
        public bool IsPodcast;
        public SortOrder Order = SortOrder.Manual;

        public string PlaylistName {
            get { return nameRecord.Value; }
            set {
                if (nameRecord == null) {
                    nameRecord = new DetailRecord (IsBE);
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

        public PlaylistRecord (bool isLibrary, bool isbe) : base (isbe) {
            this.isLibrary = isLibrary;
            this.Name = "mhyp";
        }

        public void Clear () {
            playlistItems.Clear ();
        }

        public bool RemoveItem (int index) {
            if (index < 0 || index >= playlistItems.Count)
                return false;
            
            playlistItems.RemoveAt (index);
            return true;
        }

        public void RemoveTrack (int id) {
            foreach (PlaylistItemRecord item in playlistItems) {
                if (item.TrackId == id) {
                    playlistItems.Remove (item);
                    break;
                }
            }
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

        public PlaylistItemRecord CreateItem () {
            return new PlaylistItemRecord (IsBE);
        }

        public int IndexOf (int trackid) {

            int i = 0;
            foreach (PlaylistItemRecord rec in playlistItems) {
                if (rec.TrackId == trackid) {
                    return i;
                }

                i++;
            }

            return -1;
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numdetails = ToInt32 (body, 0);
            int numitems = ToInt32 (body, 4);
            int hiddenFlag = ToInt32 (body, 8);

            if (hiddenFlag == 1)
                IsHidden = true;

            Timestamp = ToInt32 (body, 12);
            Id = ToInt32 (body, 16);
            unknownOne = ToInt32 (body, 20);

            if (db.Version >= 13) {
                IsPodcast = ToInt16 (body, 30) == 1 ? true : false;
                Order = (SortOrder) ToInt32 (body, 32);
            }

            stringDetails.Clear ();
            otherDetails.Clear ();
            playlistItems.Clear ();

            for (int i = 0; i < numdetails; i++) {
                if (i == 0) {
                    nameRecord = new DetailRecord (IsBE);
                    nameRecord.Read (db, reader);
                    stringDetails.Add (nameRecord);
                } else if (isLibrary) {
                    DetailRecord rec = new DetailRecord (IsBE);
                    rec.Read (db, reader);
                    otherDetails.Add (rec);
                } else {
                    GenericRecord rec = new GenericRecord (IsBE);
                    rec.Read (db, reader);
                    otherDetails.Add (rec);
                }
            }

            for (int i = 0; i < numitems; i++) {
                PlaylistItemRecord item = new PlaylistItemRecord (IsBE);
                item.Read (db, reader);
                playlistItems.Add (item);
            }
        }

#pragma warning disable 0169
        private DetailRecord CreateIndexRecord (TrackListRecord tracks, IndexType type) {
            DetailRecord record = new DetailRecord (IsBE);
            record.Type = DetailType.LibraryIndex;
            record.IndexType = type;

            // blah, this is soooo sleaux
            
            ArrayList items = (ArrayList) playlistItems.Clone ();
            TrackSorter sorter = new TrackSorter (tracks, type);
            items.Sort (sorter);

            ArrayList indices = new ArrayList ();
            foreach (PlaylistItemRecord item in items) {
                indices.Add (tracks.IndexOf (item.TrackId));
            }

            record.LibraryIndices = (int[]) indices.ToArray (typeof (int));
            return record;
        }
#pragma warning restore 0169

        private void CreateLibraryIndices (TrackListRecord tracks) {

            ArrayList details = new ArrayList ();
            
            // remove any existing library index records
            foreach (Record rec in (ArrayList) otherDetails) {
                DetailRecord detail = rec as DetailRecord;
                if (detail != null && detail.Type != DetailType.LibraryIndex) {
                    details.Add (rec);
                }
            }

            otherDetails = details;

            /* this is causing problems, leave it out for now
            otherDetails.Add (CreateIndexRecord (tracks, IndexType.Song));
            otherDetails.Add (CreateIndexRecord (tracks, IndexType.Album));
            otherDetails.Add (CreateIndexRecord (tracks, IndexType.Artist));
            otherDetails.Add (CreateIndexRecord (tracks, IndexType.Genre));
            otherDetails.Add (CreateIndexRecord (tracks, IndexType.Composer));
            */
        }
        
        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            if (isLibrary) {
                CreateLibraryIndices (db[DataSetIndex.Library].TrackList);
            }
            
            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new EndianBinaryWriter (stream, IsBE);

            foreach (Record rec in stringDetails) {
                rec.Save (db, childWriter);
            }

            foreach (Record rec in otherDetails) {
                rec.Save (db, childWriter);
            }

            int pos = 1;
            foreach (PlaylistItemRecord item in playlistItems) {
                item.Position = pos++;
                item.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();

            WriteName (writer);

            int reclen;

            if (db.Version >= 13) {
                reclen = 48;
            } else {
                reclen = 42;
            }
            
            writer.Write (reclen + PadLength);
            writer.Write (reclen + PadLength + childDataLength);
            writer.Write (stringDetails.Count + otherDetails.Count);
            writer.Write (playlistItems.Count);
            writer.Write (IsHidden ? 1 : 0);
            writer.Write (Timestamp);
            writer.Write (Id);
            writer.Write (unknownOne);
            writer.Write (stringDetails.Count);
            writer.Write ((short) otherDetails.Count);

            if (db.Version >= 13) {
                writer.Write (IsPodcast ? (short) 1 : (short) 0);
                writer.Write ((int) Order);
            }
            
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }

        private class TrackSorter : IComparer {

            private TrackListRecord tracks;
            private IndexType type;

            public TrackSorter (TrackListRecord tracks, IndexType type) {
                this.tracks = tracks;
                this.type = type;
            }
            
            public int Compare (object a, object b) {
                TrackRecord trackA = tracks.LookupTrack ((a as PlaylistItemRecord).TrackId);
                TrackRecord trackB = tracks.LookupTrack ((b as PlaylistItemRecord).TrackId);

                if (trackA == null || trackB == null)
                    return 0;
                
                switch (type) {
                case IndexType.Song:
                    return String.Compare (trackA.GetDetail (DetailType.Title).Value,
                                           trackB.GetDetail (DetailType.Title).Value);
                case IndexType.Artist:
                    return String.Compare (trackA.GetDetail (DetailType.Artist).Value,
                                           trackB.GetDetail (DetailType.Artist).Value);
                case IndexType.Album:
                    return String.Compare (trackA.GetDetail (DetailType.Album).Value,
                                           trackB.GetDetail (DetailType.Album).Value);
                case IndexType.Genre:
                    return String.Compare (trackA.GetDetail (DetailType.Genre).Value,
                                           trackB.GetDetail (DetailType.Genre).Value);
                case IndexType.Composer:
                    return String.Compare (trackA.GetDetail (DetailType.Composer).Value,
                                           trackB.GetDetail (DetailType.Composer).Value);
                default:
                    return 0;
                }
            }
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

        public PlaylistListRecord (bool isbe) : base (isbe) {
            PlaylistRecord record = new PlaylistRecord (true, isbe);
            record.IsHidden = true;
            record.PlaylistName = "IPOD";
            playlists.Add (record);

            this.Name = "mhlp";
        }

        public IEnumerator GetEnumerator () {
            return playlists.GetEnumerator ();
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            int numlists = this.HeaderTwo;

            reader.ReadBytes (this.HeaderOne - 12);

            playlists.Clear ();

            for (int i = 0; i < numlists; i++) {
                bool isLibrary = false;
                
                if (i == 0)
                    isLibrary = true;
                
                PlaylistRecord list = new PlaylistRecord (isLibrary, IsBE);
                list.Read (db, reader);
                playlists.Add (list);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {
            WriteName (writer);
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
        public string Value;
        public int Position = 1;

        public IndexType IndexType;
        public int[] LibraryIndices;

        public DetailRecord (bool isbe) : base (isbe) {
            this.Name = "mhod";
            this.HeaderOne = 24; // this is always the value for mhods
        }

        public DetailRecord (DetailType type, string value, bool isbe) : this (isbe) {
            this.Type = type;
            this.Value = value;
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            byte[] body = reader.ReadBytes (this.HeaderTwo - 12);
            
            Type = (DetailType) ToInt32 (body, 0);

            if ((int) Type > 50 && Type != DetailType.Misc && Type != DetailType.LibraryIndex)
                throw new DatabaseReadException ("Unsupported detail type: " + Type);

            unknownOne = ToInt32 (body, 4);
            unknownTwo = ToInt32 (body, 8);
            
            if ((int) Type < 50) {
                if (Type == DetailType.PodcastUrl ||
                    Type == DetailType.PodcastUrl2) {

                    Value = Encoding.UTF8.GetString (body, 12, body.Length - 12);
                } else if (Type == DetailType.ChapterData) {
                    // ugh ugh ugh, just preserve it for now -- no parsing

                    chapterData = new byte[body.Length - 12];
                    Array.Copy (body, 12, chapterData, 0, body.Length - 12);
                } else {
                    
                    Position = ToInt32 (body, 12);

                    int strlen = 0;
                    //int strenc = 0;
            
                    if ((int) Type < 50) {
                        // 'string' mhods       
                        strlen = ToInt32 (body, 16);
                        //strenc = ToInt32 (body, 20); // 0 == UTF16, 1 == UTF8
                        unknownThree = ToInt32 (body, 24);
                    }

                    // the strenc field is not what it was thought to be
                    // latest DBs have the field set to 1 even when the encoding
                    // is UTF-16. For now I'm just encoding as UTF-16
                    if (strlen >= 2 && body[29] == '\0') {
                        Value = encoding.GetString (body, 28, strlen);
                    } else {
                        Value = Encoding.UTF8.GetString(body, 28, strlen);
                    }
                }
            } else if (Type == DetailType.LibraryIndex) {
                IndexType = (IndexType) ToInt32 (body, 12);

                /*

                this is totally hosing stuff up on SLVR, and we don't use it anyway,
                so nuke it for now.
                
                int numEntries = ToInt32 (body, 16);

                ArrayList entries = new ArrayList ();
                
                for (int i = 0; i < numEntries; i++) {
                    int entry = ToInt32 (body, 56 + (i * 4));
                    entries.Add (entry);
                }

                LibraryIndices = (int[]) entries.ToArray (typeof (int));
                */
            } else if (Type == DetailType.Misc) {
                Position = ToInt32 (body, 12);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            if (Value == null)
                Value = String.Empty;

            WriteName (writer);
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
                    if (IsBE) {
                        // ugh. it looks like the big endian databases normally use utf8
                        valbytes = Encoding.UTF8.GetBytes (Value);
                    } else {
                        valbytes = encoding.GetBytes (Value);
                    }
                    
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
        //private int unknownNine;
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
        public int WeirdDRMValue;

        public DetailRecord[] Details {
            get { return (DetailRecord[]) details.ToArray (typeof (DetailRecord)); }
        }

        public TrackRecord (bool isbe) : base (isbe) {
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

            DetailRecord rec = new DetailRecord (IsBE);
            rec.Type = type;
            AddDetail (rec);
            
            return rec;
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {

            base.Read (db, reader);
            
            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numDetails = ToInt32 (body, 0);
            Id = ToInt32 (body, 4);
            Hidden = ToInt32 (body, 8) == 1 ? false : true;
            Type = (TrackRecordType) ToInt16 (body, 16);
            CompilationFlag = body[18];
            Rating = body[19];
            Date = ToUInt32 (body, 20);
            Size = ToInt32 (body, 24);
            Length = ToInt32 (body, 28);
            TrackNumber = ToInt32 (body, 32);
            TotalTracks = ToInt32 (body, 36);
            Year = ToInt32 (body, 40);
            BitRate = ToInt32 (body, 44);
            unknownThree = ToInt16 (body, 48);
            SampleRate = ToUInt16 (body, 50);
            Volume = ToInt32 (body, 52);
            StartTime = ToInt32 (body, 56);
            StopTime = ToInt32 (body, 60);
            SoundCheck = ToInt32 (body, 64);
            PlayCount = ToInt32 (body, 68);
            playCountDup = ToInt32 (body, 72);
            LastPlayedTime = ToUInt32 (body, 76);
            DiscNumber = ToInt32 (body, 80);
            TotalDiscs = ToInt32 (body, 84);
            UserId = ToInt32 (body, 88);
            LastModifiedTime = ToUInt32 (body, 92);
            BookmarkTime = ToInt32 (body, 96);
            DatabaseId = ToInt64 (body, 100);
            Checked = body[108];
            ApplicationRating = body[109];
            BPM = ToInt16 (body, 110);
            ArtworkCount = ToInt16 (body, 114);
            unknownFour = ToInt16 (body, 112);
            ArtworkSize = ToInt32 (body, 116);
            unknownFive = ToInt32 (body, 120);
            unknownSix = ToInt32 (body, 124);
            unknownSeven = ToInt32 (body, 128);
            unknownEight = ToInt32 (body, 132);
            WeirdDRMValue = ToInt32 (body, 136);
            unknownTen = ToInt32 (body, 140);

            details.Clear ();

            for (int i = 0; i < numDetails; i++) {
                DetailRecord rec = new DetailRecord (IsBE);
                rec.Read (db, reader);
                details.Add (rec);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new EndianBinaryWriter (stream, IsBE);

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
            
            WriteName (writer);
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
            writer.Write (WeirdDRMValue);
            writer.Write (unknownTen);

            if (db.Version >= 12) {
                writer.Write (new byte[88]);
            }
            
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal class TrackListRecord : Record, IEnumerable {

        private ArrayList tracks = new ArrayList ();

        public TrackRecord[] Tracks {
            get { return (TrackRecord[]) tracks.ToArray (typeof (TrackRecord)); }
        }

        public TrackListRecord (bool isbe) : base (isbe) {
            this.Name = "mhlt";
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

        public TrackRecord LookupTrack (int id) {
            foreach (TrackRecord record in tracks) {
                if (record.Id == id)
                    return record;
            }

            return null;
        }

        public int IndexOf (int id) {
            for (int i = 0; i < tracks.Count; i++) {
                if ((tracks[i] as TrackRecord).Id == id)
                    return i;
            }

            return -1;
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {

            base.Read (db, reader);
            
            reader.ReadBytes (this.HeaderOne - 12);

            int trackCount = this.HeaderTwo;

            tracks.Clear ();
            
            for (int i = 0; i < trackCount; i++) {
                TrackRecord rec = new TrackRecord (IsBE);
                rec.Read (db, reader);
                tracks.Add (rec);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new EndianBinaryWriter (stream, IsBE);

            foreach (TrackRecord rec in tracks) {
                rec.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();

            WriteName (writer);
            writer.Write (12 + PadLength);
            writer.Write (tracks.Count);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal enum DataSetIndex {
        Library = 1,
        Playlist = 2,
        PlaylistDuplicate = 3
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

        public DataSetRecord (bool isbe) : base (isbe) {
            this.Name = "mhsd";
        }

        public DataSetRecord (DataSetIndex index, bool isbe) : this (isbe) {
            this.Index = index;

            if (Index == DataSetIndex.Library) {
                TrackList = new TrackListRecord (isbe);
            } else {
                PlaylistList = new PlaylistListRecord (isbe);
            }
        }

        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            Index = (DataSetIndex) ToInt32 (body, 0);

            switch (Index) {
            case DataSetIndex.Library:
                this.TrackList = new TrackListRecord (IsBE);
                this.TrackList.Read (db, reader);
                break;
            case DataSetIndex.Playlist:
            case DataSetIndex.PlaylistDuplicate:
                this.PlaylistList = new PlaylistListRecord (IsBE);
                this.PlaylistList.Read (db, reader);
                break;
            default:
                throw new DatabaseReadException ("Can't handle dataset index: " + Index);
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            byte[] childData;
            int childDataLength;

            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new EndianBinaryWriter (stream, IsBE);

            switch (Index) {
            case DataSetIndex.Library:
                TrackList.Save (db, childWriter);
                break;
            case DataSetIndex.Playlist:
            case DataSetIndex.PlaylistDuplicate:
                PlaylistList.Save (db, childWriter);
                break;
            default:
                throw new DatabaseReadException ("Can't handle DataSet record index: " + Index);
            }

            childWriter.Flush ();
            childData = stream.GetBuffer ();
            childDataLength = (int) stream.Length;
            childWriter.Close ();

            WriteName (writer);
            writer.Write (16 + PadLength);
            writer.Write (16 + PadLength + childDataLength);
            writer.Write ((int) Index);
            writer.Write (new byte[PadLength]);
            writer.Write (childData, 0, childDataLength);
        }
    }

    internal class DatabaseRecord : Record {

        private const int MaxSupportedVersion = 17;
        private const int SongIdStart = 1000;

        private int unknownOne = 1;
        private int unknownTwo = 2;
        
        private ArrayList datasets = new ArrayList ();

        public int Version = MaxSupportedVersion;
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

        public DatabaseRecord (bool isbe) : base (isbe) {
            datasets = new ArrayList ();
            datasets.Add (new DataSetRecord (DataSetIndex.Library, isbe));

            DataSetRecord plrec = new DataSetRecord (DataSetIndex.Playlist, isbe);
            datasets.Add (plrec);

            DataSetRecord plrec2 = new DataSetRecord (DataSetIndex.PlaylistDuplicate, isbe);
            plrec2.PlaylistList = plrec.PlaylistList;
            datasets.Add (plrec2);

            this.Name = "mhbd";
        }
        
        public override void Read (DatabaseRecord db, BinaryReader reader) {
            base.Read (db, reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            
            unknownOne = ToInt32 (body, 0);
            Version = ToInt32 (body, 4);
            int childrenCount = ToInt32 (body, 8);
            Id = ToInt64 (body, 12);
            unknownTwo = ToInt32 (body, 20);

            if (Version > MaxSupportedVersion)
                throw new DatabaseReadException ("Detected unsupported database version {0}", Version);
            
            datasets.Clear ();

            for (int i = 0; i < childrenCount; i++) {
                DataSetRecord rec = new DataSetRecord (IsBE);
                rec.Read (this, reader);
                datasets.Add (rec);
            }

            // make the duplicate record have the same stuff as the 'real' one
            if (this[DataSetIndex.PlaylistDuplicate] != null)
                this[DataSetIndex.PlaylistDuplicate].PlaylistList = this[DataSetIndex.Playlist].PlaylistList;
        }

        private void ReassignTrackIds () {
            Hashtable oldids = new Hashtable ();

            int id = SongIdStart;
            foreach (TrackRecord track in this[DataSetIndex.Library].TrackList.Tracks) {
                oldids[track.Id] = id;
                track.Id = id++;
            }

            foreach (PlaylistRecord pl in this[DataSetIndex.Playlist].PlaylistList.Playlists) {
                foreach (PlaylistItemRecord item in pl.Items) {
                    if (oldids[item.TrackId] == null)
                        continue;
                    
                    item.TrackId = (int) oldids[item.TrackId];
                }
            }
        }

        public override void Save (DatabaseRecord db, BinaryWriter writer) {

            ReassignTrackIds ();
            
            MemoryStream stream = new MemoryStream ();
            BinaryWriter childWriter = new EndianBinaryWriter (stream, IsBE);

            foreach (DataSetRecord rec in datasets) {
                rec.Save (db, childWriter);
            }

            childWriter.Flush ();
            byte[] childData = stream.GetBuffer ();
            int childDataLength = (int) stream.Length;
            childWriter.Close ();

            WriteName (writer);
            writer.Write (36 + PadLength);
            writer.Write (36 + PadLength + childDataLength);

            writer.Write (unknownOne);
            writer.Write (Version);
            writer.Write (datasets.Count);
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

    public class SaveProgressArgs : EventArgs {

        private Song song;
        private double songPercent;
        private int completed;
        private int total;
        
        public Song CurrentSong {
            get { return song; }
        }

        public double SongProgress {
            get { return songPercent; }
        }

        public double TotalProgress {
            get {
                if (completed == total) {
                    return 1.0;
                } else {
                    double fraction = (double) completed / (double) total;
                    
                    return fraction + (1.0 / (double) total) * songPercent;
                }
            }
        }

        public int SongsCompleted {
            get { return completed; }
        }

        public int SongsTotal {
            get { return total; }
        }

        public SaveProgressArgs (Song song, double songPercent, int completed, int total) {
            this.song = song;
            this.songPercent = songPercent;
            this.completed = completed;
            this.total = total;
        }
    }

    public delegate void PlaylistHandler (object o, Playlist playlist);
    public delegate void SaveProgressHandler (object o, SaveProgressArgs args);

    public class SongDatabase {

        private const int CopyBufferSize = 8192;
        private const double PercentThreshold = 0.10;
        
        private DatabaseRecord dbrec;

        private ArrayList songs = new ArrayList ();
        private ArrayList songsToAdd = new ArrayList ();
        private ArrayList songsToRemove = new ArrayList ();

        private ArrayList playlists = new ArrayList ();
        private ArrayList otgPlaylists = new ArrayList ();
        
        private Random random = new Random();
        private Device device;

        private string controlPath;

        public event EventHandler SaveStarted;
        public event SaveProgressHandler SaveProgressChanged;
        public event EventHandler SaveEnded;

        public event PlaylistHandler PlaylistAdded;
        public event PlaylistHandler PlaylistRemoved;

        public event EventHandler Reloaded;

        private string ControlPath {
            get { return device.ControlPath; }
        }

        private string ControlDirectoryName {
            get {
                // so lame
                if (device.ControlPath.IndexOf ("iPod_Control") >= 0)
                    return "iPod_Control";
                else
                    return "iTunes_Control";
            }
        }

        private string SongDbPath {
            get { return ControlPath + "iTunes/iTunesDB"; }
        }

        private string SongDbBackupPath {
            get { return SongDbPath + ".bak"; }
        }

        private string MusicBasePath {
            get { return ControlPath + "Music"; }
        }

        private string PlayCountsPath {
            get { return ControlPath + "iTunes/Play Counts"; }
        }

        public Song[] Songs {
            get {
                return (Song[]) songs.ToArray (typeof (Song));
            }
        }

        public Playlist[] Playlists {
            get {
                return (Playlist[]) playlists.ToArray (typeof (Playlist));
            }
        }

        public Playlist[] OnTheGoPlaylists {
            get { return (Playlist[]) otgPlaylists.ToArray (typeof (Playlist)); }
        }

        public int Version {
            get { return dbrec.Version; }
        }

        internal SongDatabase (Device device) : this (device, false) {
        }
        
        internal SongDatabase (Device device, bool createFresh) {
            this.device = device;
            
            if(createFresh && File.Exists(SongDbPath)) {
                File.Copy (SongDbPath, SongDbBackupPath, true);
            }
            
            Reload (createFresh);
        }
        
        
        private void Clear () {
            dbrec = null;
            songs.Clear ();
            songsToAdd.Clear ();
            songsToRemove.Clear ();
            playlists.Clear ();
            otgPlaylists.Clear ();
        }

        private void LoadPlayCounts () {
            if (!File.Exists (PlayCountsPath))
                return;
            
            using (BinaryReader reader = new
                   BinaryReader (File.Open (PlayCountsPath, FileMode.Open, FileAccess.Read))) {

                byte[] header = reader.ReadBytes (96);
                int entryLength = 0;
                int numEntries = 0;

                if(header.Length < 16) {
                    return;
                }

                try {
                    entryLength = dbrec.ToInt32 (header, 8);
                    numEntries = dbrec.ToInt32 (header, 12);
                } catch { 
                    return;
                }

                for (int i = 0; i < numEntries; i++) {
                    
                    byte[] entry = reader.ReadBytes (entryLength);
                    
                    (songs[i] as Song).LatestPlayCount = dbrec.ToInt32 (entry, 0);
                    (songs[i] as Song).PlayCount += (songs[i] as Song).LatestPlayCount;

                    uint lastPlayed = dbrec.ToUInt32 (entry, 4);
                    if (lastPlayed > 0) {
                        (songs[i] as Song).Track.LastPlayedTime = lastPlayed;
                    }

                    // if it has rating info, get it
                    if (entryLength >= 16) {
                        // Why is this one byte in iTunesDB and 4 here?
                        
                        int rating = dbrec.ToInt32 (entry, 12);
                        (songs[i] as Song).Track.Rating  = (byte) rating;
                    }
                }
            }
        }

        private bool LoadOnTheGo (int num) {
            string path = ControlPath + "iTunes/OTGPlaylistInfo";

            if (num != 0) {
                path += "_" + num;
            }                

            FileInfo finfo = new FileInfo (path);
            
            if (!finfo.Exists || finfo.Length == 0) {
                return false;
            }
            
            ArrayList otgsongs = new ArrayList ();
            
            using (BinaryReader reader = new BinaryReader (File.Open (path, FileMode.Open, FileAccess.Read))) {

                byte[] header = reader.ReadBytes (20);

                int numTracks = dbrec.ToInt32 (header, 12);

                for (int i = 0; i < numTracks; i++) {
                    int index = reader.ReadInt32 ();

                    if (dbrec.IsBE)
                        index = Utility.Swap (index);

                    otgsongs.Add (songs[index]);
                }
            }

            string title = "On-The-Go";

            if (num != 0) {
                title += " " + num;
            }
            
            otgPlaylists.Add (new Playlist (this, title, (Song[]) otgsongs.ToArray (typeof (Song))));
            return true;
        }

        private void LoadOnTheGo () {
            int i = 0;
            while (LoadOnTheGo (i++));
        }

        public void Reload () {
            Reload(false);
        }

        private void Reload (bool createFresh) {
        
            Clear ();

            // This blows, we need to use the device model number or something
            bool useBE = ControlPath.EndsWith ("iTunes_Control");
                
            if (!File.Exists (SongDbPath) || createFresh) {
                dbrec = new DatabaseRecord (useBE);
                LoadOnTheGo ();
                return;
            }

            using (BinaryReader reader = new BinaryReader (File.Open (SongDbPath, FileMode.Open, FileAccess.Read))) {

                // FIXME
                dbrec = new DatabaseRecord (useBE);
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

                if (Reloaded != null)
                    Reloaded (this, new EventArgs ());
            }
        }

        internal bool IsSongOnDevice(string path) {
            return path.StartsWith (MusicBasePath + "/F");
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
                if (!IsSongOnDevice (song.FileName)) {
                    required += (UInt64) song.Size;
                }
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
                FileInfo info = new FileInfo (song.FileName);
                long length = info.Length;
                long count = 0;
                double lastPercent = 0.0;

                reader = new BinaryReader (new BufferedStream (File.Open (song.FileName, FileMode.Open, FileAccess.Read)));
                writer = new BinaryWriter (new BufferedStream (File.Open (dest, FileMode.Create)));
                
                do {
                    byte[] buf = reader.ReadBytes (CopyBufferSize);
                    writer.Write (buf);
                    count += buf.Length;

                    double percent = (double) count / (double) length;
                    if (percent >= lastPercent + PercentThreshold && SaveProgressChanged != null) {
                        SaveProgressArgs args = new SaveProgressArgs (song, (double) count / (double) length,
                                                                      completed, total);

                        try {
                            SaveProgressChanged (this, args);
                        } catch (Exception e) {
                            Console.Error.WriteLine ("Exception in progress handler: " + e);
                        }
                        
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

            // make sure all the new songs have file names, and that they exist
            foreach (Song song in songsToAdd) {
                if (song.FileName == null)
                    throw new DatabaseWriteException (String.Format ("Song '{0}' has no file assigned", song.Title));
                else if (!File.Exists (song.FileName)) {
                    throw new DatabaseWriteException (String.Format ("File '{0}' for song '{1}' does not exist",
                                                                     song.FileName, song.Title));
                }
            }

            if (SaveStarted != null)
                SaveStarted (this, new EventArgs ());

            // Back up the current song db
            if (File.Exists (SongDbPath))
                File.Copy (SongDbPath, SongDbBackupPath, true);
            
            try {
                // Save the songs db
                using (BinaryWriter writer = new EndianBinaryWriter (new FileStream (SongDbPath, FileMode.Create),
                                                                     dbrec.IsBE)) {
                    dbrec.Save (dbrec, writer);
                }

                foreach (Song song in songsToRemove) {
                    if (File.Exists (song.FileName))
                        File.Delete (song.FileName);
                }
                
                if (!Directory.Exists (MusicBasePath))
                    Directory.CreateDirectory (MusicBasePath);
                
                int completed = 0;
                
                // Copy songs to iPod; if song is already in the Music directory structure, do not copy
                foreach (Song song in songsToAdd) {
                    if (!IsSongOnDevice (song.FileName)) {
                        string dest = GetFilesystemPath (song.Track.GetDetail (DetailType.Location).Value);
                        CopySong (song, dest, completed++, songsToAdd.Count);
                        song.FileName = dest;
                    }
                }

                // Save the shuffle songs db (will only create if device is shuffle);
                try {
                    ShuffleSongDatabase.Save (device);
                } catch (Exception) {}
                
                // The play count file is invalid now, so we'll remove it (even though the iPod would anyway)
                if (File.Exists (PlayCountsPath))
                    File.Delete (PlayCountsPath);

                // Force progress to 100% so the app can now we're in the "sync()" phase
                if (SaveProgressChanged != null) {
                    try {
                        SaveProgressChanged (this, new SaveProgressArgs (null, 1.0, 1, 1));
                    } catch (Exception e) {
                        Console.Error.WriteLine ("Exception in progress handler: " + e);
                    }
                }

                // Remove empty Music "F" directories
                DirectoryInfo music_dir = new DirectoryInfo (MusicBasePath);
                foreach (DirectoryInfo f_dir in music_dir.GetDirectories ()) {
                    try {
                        if (f_dir.GetFiles ().Length == 0) {
                            f_dir.Delete();
                        }
                    } catch {
                    }
                }

                Mono.Unix.Native.Syscall.sync ();
            } catch (Exception e) {
                // rollback the song db
                if (File.Exists (SongDbBackupPath))
                    File.Copy (SongDbBackupPath, SongDbPath, true);

                throw new DatabaseWriteException (e, "Failed to save database");
            } finally {
                try {
                    device.RescanDisk();
                } catch(Exception) {}

                songsToAdd.Clear ();
                songsToRemove.Clear ();
                
                if (SaveEnded != null)
                    SaveEnded (this, new EventArgs ());
            }
        }

        internal string GetFilesystemPath (string ipodPath) {
            if (ipodPath == null)
                return null;
            else if (ipodPath == String.Empty)
                return String.Empty;
            
            return device.MountPoint + ipodPath.Replace (":", "/");
        }

        internal string GetPodPath (string path) {
            if (path == null || !path.StartsWith (device.MountPoint))
                return null;

            string ret = path.Replace (device.MountPoint, "");
            return ret.Replace ("/", ":");
        }

        internal string GetUniquePodPath (string path) {
            if (path == null)
                return null;
            
            return String.Format (":{0}:Music:{1}", ControlDirectoryName, MakeUniquePodSongPath(path));

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

            PlaylistItemRecord item = new PlaylistItemRecord (dbrec.IsBE);
            item.TrackId = song.Track.Id;
 
            dbrec[DataSetIndex.Playlist].Library.AddItem (item);

            if (!existing)
                songsToAdd.Add (song);
            else if (songsToRemove.Contains (song))
                songsToRemove.Remove (song);
                
            songs.Add (song);
        }

        public void RemoveSong (Song song) {
            if (songs.Contains (song)) {
                songs.Remove (song);

                if (songsToAdd.Contains (song))
                    songsToAdd.Remove (song);
                else
                    songsToRemove.Add (song);

                // remove from the song db
                dbrec[DataSetIndex.Library].TrackList.Remove (song.Id);
                dbrec[DataSetIndex.Playlist].Library.RemoveTrack (song.Track.Id);
                    
                // remove from the "normal" playlists
                foreach (Playlist list in playlists) {
                    list.RemoveSong (song);
                }

                // remove from On-The-Go playlists
                foreach (Playlist list in otgPlaylists) {
                    list.RemoveOTGSong (song);
                }
            }
        }

        public Song CreateSong () {
            TrackRecord track = new TrackRecord (dbrec.IsBE);
            track.Id = GetNextSongId ();
            track.Date = Utility.DateToMacTime (DateTime.Now);
            track.LastModifiedTime = track.Date;
            track.DatabaseId = (long) new Random ().Next ();
            
            Song song = new Song (this, track);
            
            AddSong (song, false);
            
            return song;
        }

        public Playlist CreatePlaylist (string name) {
            if (name == null)
                throw new ArgumentException ("name cannot be null");

            PlaylistRecord playrec = new PlaylistRecord (false, dbrec.IsBE);
            playrec.PlaylistName = name;
            
            dbrec[DataSetIndex.Playlist].PlaylistList.AddPlaylist (playrec);

            Playlist list = new Playlist (this, playrec);
            playlists.Add (list);

            if (PlaylistAdded != null)
                PlaylistAdded (this, list);
            
            return list;
        }

        public void RemovePlaylist (Playlist playlist) {
            if (playlist == null) {
                throw new InvalidOperationException ("playist is null");
            } else if (playlist.IsOnTheGo) {
                throw new InvalidOperationException ("The On-The-Go playlist cannot be removed.");
            }
            
            dbrec[DataSetIndex.Playlist].PlaylistList.RemovePlaylist (playlist.PlaylistRecord);
            playlists.Remove (playlist);

            if (PlaylistRemoved != null)
                PlaylistRemoved (this, playlist);
        }

        public Playlist LookupPlaylist (string name) {
            foreach (Playlist list in playlists) {
                if (list.Name == name)
                    return list;
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
