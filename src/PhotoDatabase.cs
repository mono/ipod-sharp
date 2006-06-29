using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.IO;
using Mono.Unix;
using Mono.Unix.Native;

namespace IPod {

    internal abstract class PhotoDbRecord : Record {

        protected int recordPadding = PadLength;

        public PhotoDbRecord (bool isbe) : base (isbe) {
        }
        
        public virtual void Read (BinaryReader reader) {
            ReadHeader (reader);
        }

        public abstract void Save (BinaryWriter writer);

        protected void SaveChild (PhotoDbRecord record, out byte[] data, out int length) {
            List<PhotoDbRecord> list = new List<PhotoDbRecord> ();
            list.Add (record);
            
            SaveChildren (list, out data, out length);
        }

        protected void SaveChildren (ICollection records, out byte[] data, out int length) {
            MemoryStream stream = new MemoryStream ();
            BinaryWriter writer = new EndianBinaryWriter (stream, IsBE);

            foreach (PhotoDbRecord record in records) {
                record.Save (writer);
            }
            
            writer.Flush ();
            length = (int) stream.Length;
            data = stream.GetBuffer ();
            writer.Close ();
        }

        protected override void WritePadding (BinaryWriter writer) {
            writer.Write (new byte[recordPadding]);
        }
    }

    internal class DataFileRecord : PhotoDbRecord {

        private int unknownOne;
        private int unknownTwo;
        private int unknownThree;
        private long unknownFive;
        private long unknownSix;
        private int unknownSeven = 2;
        private int unknownEight;
        private int unknownNine;
        private int unknownTen;
        private int unknownEleven;

        private List<PhotoDataSetRecord> children = new List<PhotoDataSetRecord> ();

        public int NextId;

        public PhotoDataSetRecord this [PhotoDataSetIndex index] {
            get {
                foreach (PhotoDataSetRecord ds in children) {
                    if (ds.Index == index) {
                        return ds;
                    }
                }

                return null;
            }
        }

        public DataFileRecord (bool isbe) : base (isbe) {
            this.Name = "mhfd";
            recordPadding = 64;

            children.Add (new PhotoDataSetRecord (isbe, new ImageListRecord (isbe), PhotoDataSetIndex.ImageList));
            children.Add (new PhotoDataSetRecord (isbe, new AlbumListRecord (isbe), PhotoDataSetIndex.AlbumList));
            children.Add (new PhotoDataSetRecord (isbe, new FileListRecord (isbe), PhotoDataSetIndex.FileList));
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            unknownOne = ToInt32 (body, 0);
            unknownTwo = ToInt32 (body, 4);
            int numChildren = ToInt32 (body, 8);
            unknownThree = ToInt32 (body, 12);
            NextId = ToInt32 (body, 16);
            unknownFive = ToInt64 (body, 20);
            unknownSix = ToInt64 (body, 28);
            unknownSeven = ToInt32 (body, 36);
            unknownEight = ToInt32 (body, 40);
            unknownNine = ToInt32 (body, 44);
            unknownTen = ToInt32 (body, 48);
            unknownEleven = ToInt32 (body, 52);

            recordPadding = body.Length - 56;

            children.Clear ();

            for (int i = 0; i < numChildren; i++) {
                PhotoDataSetRecord ds = new PhotoDataSetRecord (IsBE);
                ds.Read (reader);
                children.Add (ds);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChildren (children,  out childBytes, out childLen);

            WriteName (writer);
            writer.Write (68 + recordPadding);
            writer.Write (68 + recordPadding + childLen);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (children.Count);
            writer.Write (unknownThree);
            writer.Write (NextId);
            writer.Write (unknownFive);
            writer.Write (unknownSix);
            writer.Write (unknownSeven);
            writer.Write (unknownEight);
            writer.Write (unknownNine);
            writer.Write (unknownTen);
            writer.Write (unknownEleven);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal enum PhotoDataSetIndex {
        ImageList = 1,
        AlbumList = 2,
        FileList = 3
    }

    internal class PhotoDataSetRecord : PhotoDbRecord {

        public PhotoDataSetIndex Index;

        public PhotoDbRecord Data;

        public PhotoDataSetRecord (bool isbe) : this (isbe, null, 0) {
        }
        
        public PhotoDataSetRecord (bool isbe, PhotoDbRecord data, PhotoDataSetIndex index) : base (isbe) {
            this.Name = "mhsd";
            recordPadding = 80;

            this.Data = data;
            this.Index = index;
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            Index = (PhotoDataSetIndex) ToInt32 (body, 0);

            recordPadding = body.Length - 4;

            switch (Index) {
            case PhotoDataSetIndex.ImageList:
                Data = new ImageListRecord (IsBE);
                Data.Read (reader);
                break;
            case PhotoDataSetIndex.AlbumList:
                Data = new AlbumListRecord (IsBE);
                Data.Read (reader);
                break;
            case PhotoDataSetIndex.FileList:
                Data = new FileListRecord (IsBE);
                Data.Read (reader);
                break;
            default:
                break;
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChild (Data, out childBytes, out childLen);
            WriteName (writer);
            writer.Write (16 + recordPadding);
            writer.Write (16 + recordPadding + childLen);
            writer.Write ((int) Index);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class AlbumListRecord : PhotoDbRecord {

        private List<AlbumRecord> albums = new List<AlbumRecord> ();

        public IList<AlbumRecord> Albums {
            get {
                return new ReadOnlyCollection<AlbumRecord> (albums);
            }
        }

        public void AddAlbum (AlbumRecord album) {
            albums.Add (album);
        }

        public void RemoveAlbum (AlbumRecord album) {
            albums.Remove (album);
        }
        
        public AlbumListRecord (bool isbe) : base (isbe) {
            this.Name = "mhla";
            recordPadding = 80;
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            if (HeaderOne - 12 > 0) {
                byte[] body = reader.ReadBytes (HeaderOne - 12);
                recordPadding = body.Length;
            }

            albums.Clear ();
            
            int numChildren = HeaderTwo;
            for (int i = 0; i < numChildren; i++) {
                AlbumRecord record = new AlbumRecord (IsBE);
                record.Read (reader);
                albums.Add (record);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChildren (albums, out childBytes, out childLen);
            WriteName (writer);
            writer.Write (12 + recordPadding);
            writer.Write (albums.Count);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class AlbumRecord : PhotoDbRecord {

        private int unknownOne;
        private short unknownTwo;
        private int unknownThree;
        private int unknownFour;

        private PhotoDetailRecord nameRecord;

        private Dictionary<int, AlbumItemRecord> items = new Dictionary<int, AlbumItemRecord> ();
        
        public int PlaylistId;
        public bool IsMaster;
        public bool PlayMusic;
        public bool Repeat;
        public bool Random;
        public bool ShowTitles;
        public byte TransitionDirection;
        public int SlideDuration;
        public int TransitionDuration;
        public long TrackId;
        public int PreviousPlaylistId;

        public IList<AlbumItemRecord> Items {
            get {
                return new ReadOnlyCollection<AlbumItemRecord> (new List<AlbumItemRecord> (items.Values));
            }
        }

        public string AlbumName {
            get { return nameRecord.Value; }
            set { nameRecord.Value = value; }
        }

        public AlbumRecord (bool isbe) : base (isbe) {
            this.Name = "mhba";
            nameRecord = new PhotoDetailRecord (IsBE);
            nameRecord.Type = PhotoDetailType.String;
            nameRecord.BrokenChildPadding = true;

            recordPadding = 84;
        }

        public void AddItem (AlbumItemRecord item) {
            items[item.Id] = item;
        }

        public void RemoveItem (int id) {
            items.Remove (id);
        }

        public void RemoveItem (AlbumItemRecord item) {
            items.Remove (item.Id);
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (HeaderOne - 12);

            int numDataObjects = ToInt32 (body, 0);
            int numAlbums = ToInt32 (body, 4);
            PlaylistId = ToInt32 (body, 8);
            unknownOne = ToInt32 (body, 12);
            unknownTwo = ToInt16 (body, 16);
            IsMaster = body[18] == (byte) 1;

            PlayMusic = body[19] == (byte) 1;
            Repeat = body[20] == (byte) 1;
            Random = body[21] == (byte) 1;
            ShowTitles = body[22] == (byte) 1;
            TransitionDirection = body[23];
            SlideDuration = ToInt32 (body, 24);
            TransitionDuration = ToInt32 (body, 28);
            unknownThree = ToInt32 (body, 32);
            unknownFour = ToInt32 (body, 36);
            TrackId = ToInt64 (body, 40);
            PreviousPlaylistId = ToInt32 (body, 48);

            recordPadding = body.Length - 52;

            for (int i = 0; i < numDataObjects; i++) {
                PhotoDetailRecord detail = new PhotoDetailRecord (IsBE);
                detail.Read (reader);
                detail.BrokenChildPadding = true;

                if (i == 0) {
                    nameRecord = detail;
                }
            }

            items.Clear ();

            for (int i = 0; i < numAlbums; i++) {
                AlbumItemRecord item = new AlbumItemRecord (IsBE);
                item.Read (reader);

                AddItem (item);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] detailBytes;
            int detailLen;

            SaveChild (nameRecord, out detailBytes, out detailLen);

            byte[] childBytes;
            int childLen;

            SaveChildren (items.Values, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (64 + recordPadding);
            writer.Write (64 + recordPadding + detailLen + childLen);
            writer.Write (1);
            writer.Write (items.Count);
            writer.Write (PlaylistId);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (IsMaster ? (byte) 1 : (byte) 6);
            writer.Write (PlayMusic ? (byte) 1 : (byte) 0);
            writer.Write (Repeat ? (byte) 1 : (byte) 0);
            writer.Write (Random ? (byte) 1 : (byte) 0);
            writer.Write (ShowTitles ? (byte) 1 : (byte) 0);
            writer.Write (TransitionDirection);
            writer.Write (SlideDuration);
            writer.Write (TransitionDuration);
            writer.Write (unknownThree);
            writer.Write (unknownFour);
            writer.Write (TrackId);
            writer.Write (PreviousPlaylistId);
            WritePadding (writer);
            writer.Write (detailBytes, 0, detailLen);
            writer.Write (childBytes, 0, childLen);

        }
    }

    internal class AlbumItemRecord : PhotoDbRecord {

        private int unknownOne;

        public int Id;

        public AlbumItemRecord (bool isbe) : this (isbe, 0) {
        }

        public AlbumItemRecord (bool isbe, int id) : base (isbe) {
            this.Name = "mhia";
            this.Id = id;

            recordPadding = 20;
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (HeaderOne - 12);

            unknownOne = ToInt32 (body, 0);
            Id = ToInt32 (body, 4);

            recordPadding = body.Length - 8;
        }

        public override void Save (BinaryWriter writer) {
            WriteName (writer);
            writer.Write (20 + recordPadding);
            writer.Write (20 + recordPadding);
            writer.Write (unknownOne);
            writer.Write (Id);
            WritePadding (writer);
        }
    }


    internal class ImageListRecord : PhotoDbRecord {

        private List<ImageItemRecord> items = new List<ImageItemRecord> ();
        private List<ImageItemRecord> removedItems = new List<ImageItemRecord> ();

        public IList<ImageItemRecord> Items {
            get {
                return new ReadOnlyCollection<ImageItemRecord> (items);
            }
        }

        public IList<ImageItemRecord> RemovedItems {
            get {
                return new ReadOnlyCollection<ImageItemRecord> (removedItems);
            }
        }

        public ImageListRecord (bool isbe) : base (isbe) {
            this.Name = "mhli";
            recordPadding = 80;
        }

        public void AddItem (ImageItemRecord item) {
            items.Add (item);
            removedItems.Remove (item);
        }

        public void RemoveItem (ImageItemRecord item) {
            items.Remove (item);
            removedItems.Add (item);
        }

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            recordPadding = body.Length;

            for (int i = 0; i < this.HeaderTwo; i++) {
                ImageItemRecord item = new ImageItemRecord (IsBE);
                item.Read (reader);
                
                items.Add (item);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChildren (items, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (12 + recordPadding);
            writer.Write (items.Count);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal enum PhotoDetailType {
        String = 1,
        ThumbnailContainer = 2,
        FileName = 3,
        ImageContainer = 5
    }

    internal class PhotoDetailRecord : PhotoDbRecord {

        public PhotoDetailType Type;

        public ImageNameRecord ImageName;
        public string Value;

        public bool BrokenChildPadding = false;

        public PhotoDetailRecord (bool isbe) : base (isbe) {
            this.Name = "mhod";
        }

        public PhotoDetailRecord (bool isbe, ImageNameRecord name, PhotoDetailType type) : this (isbe) {
            Type = PhotoDetailType.ThumbnailContainer;
            ImageName = name;
            Type = type;
        }

        public PhotoDetailRecord (bool isbe, PhotoDetailType type) : this (isbe, null, type) {
        }

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            Type = (PhotoDetailType) ToInt16 (body, 0);

            switch (Type) {
            case PhotoDetailType.ThumbnailContainer:
            case PhotoDetailType.ImageContainer:
                ImageName = new ImageNameRecord (IsBE);
                ImageName.Read (reader);
                break;
            case PhotoDetailType.FileName:
                ReadString (reader, true);
                break;
            case PhotoDetailType.String:
                ReadString (reader, false);
                break;
            default:
                throw new DatabaseReadException ("Unknown detail type: " + Type);
            }
        }

        private void ReadString (BinaryReader reader, bool utf16) {
            byte[] stringData = reader.ReadBytes (12);
            int len = ToInt32 (stringData, 0);

            if (utf16) {
                Value = Encoding.Unicode.GetString (reader.ReadBytes (len));
            } else {
                Value = Encoding.UTF8.GetString (reader.ReadBytes (len));
            }
            
            int padding = HeaderTwo - len - HeaderOne - 12;
            
            if (padding > 0) {
                reader.ReadBytes (padding);
            }
        }

        private void WriteString (BinaryWriter writer, byte[] bytes, int padding, bool utf16) {
            writer.Write (bytes.Length);
            writer.Write (utf16 ? 2 : 1);
            writer.Write (0);
            writer.Write (bytes);
            writer.Write (new byte[padding]);
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;
            int childPadding;

            switch (Type) {
            case PhotoDetailType.ThumbnailContainer:
            case PhotoDetailType.ImageContainer:
                SaveChild (ImageName, out childBytes, out childLen);
                break;
            case PhotoDetailType.FileName:
                childBytes = Encoding.Unicode.GetBytes (Value);
                childLen = 12 + childBytes.Length;
                break;
            case PhotoDetailType.String:
                childBytes = Encoding.UTF8.GetBytes (Value);
                childLen = 12 + childBytes.Length;
                break;
            default:
                throw new DatabaseWriteException ("Unknown detail type: " + Type);
            }

            childPadding = 0;

            if (Type == PhotoDetailType.FileName || Type == PhotoDetailType.String) {
                childPadding = 4 - (16 + recordPadding + childLen)%4;
            }

            WriteName (writer);
            writer.Write (16 + recordPadding);
            writer.Write (16 + recordPadding + childLen + childPadding);
            writer.Write ((short) Type);

            if (BrokenChildPadding) {
                writer.Write (Utility.Swap ((short) childPadding));
            } else {
                writer.Write ((short) childPadding);
            }
            
            WritePadding (writer);

            switch (Type) {
            case PhotoDetailType.ThumbnailContainer:
            case PhotoDetailType.ImageContainer:
                writer.Write (childBytes, 0, childLen);
                break;
            case PhotoDetailType.FileName:
                WriteString (writer, childBytes, childPadding, true);
                break;
            case PhotoDetailType.String:
                WriteString (writer, childBytes, childPadding, false);
                break;
            default:
                throw new DatabaseWriteException ("Unknown detail type: " + Type);
            }
        }
    }

    internal class ImageNameRecordSorter : IComparer<ImageNameRecord> {

        public int Compare (ImageNameRecord a, ImageNameRecord b) {
            return a.ThumbnailOffset.CompareTo (b.ThumbnailOffset);
        }
    }

    internal class ImageNameRecord : PhotoDbRecord {

        private PhotoDetailRecord fileDetail;
        
        public int CorrelationId;
        public int ThumbnailOffset = -1;
        public int ImageSize;
        public short VerticalPadding;
        public short HorizontalPadding;
        public short ImageHeight;
        public short ImageWidth;

        public bool Dirty = false;
        
        public string FileName {
            get { return fileDetail.Value; }
            set { fileDetail.Value = value; }
        }

        public ImageNameRecord (bool isbe) : base (isbe) {
            this.Name = "mhni";
            recordPadding = 40;

            fileDetail = new PhotoDetailRecord (IsBE);
            fileDetail.Type = PhotoDetailType.FileName;
        }

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numChildren = ToInt32 (body, 0);
            CorrelationId = ToInt32 (body, 4);
            ThumbnailOffset = ToInt32 (body, 8);
            ImageSize = ToInt32 (body, 12);
            VerticalPadding = ToInt16 (body, 16);
            HorizontalPadding = ToInt16 (body, 18);
            ImageHeight = ToInt16 (body, 20);
            ImageWidth = ToInt16 (body, 22);

            recordPadding = body.Length - 24;

            for (int i = 0; i < numChildren; i++) {
                PhotoDetailRecord detail = new PhotoDetailRecord (IsBE);
                detail.Read (reader);

                if (i == 0) {
                    fileDetail = detail;
                }
            }
        }

        public void SetThumbFileName (bool isPhoto) {
            if (CorrelationId <= 0)
                return;

            if (isPhoto) {
                fileDetail.Value = String.Format (":Thumbs:F{0}_1.ithmb", CorrelationId);
            } else {
                fileDetail.Value = String.Format (":F{0}_1.ithmb", CorrelationId);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            fileDetail.BrokenChildPadding = true;
            SaveChild (fileDetail, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (36 + recordPadding);
            writer.Write (36 + recordPadding + childLen);
            writer.Write (1);
            writer.Write (CorrelationId);
            writer.Write (ThumbnailOffset);
            writer.Write (ImageSize);
            writer.Write (VerticalPadding);
            writer.Write (HorizontalPadding);
            writer.Write (ImageHeight);
            writer.Write (ImageWidth);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }

        public byte[] GetData (Stream stream) {
            if (ThumbnailOffset < 0)
                return null;
            
            stream.Seek ((long) ThumbnailOffset, SeekOrigin.Begin);

            byte[] buf = new byte[ImageSize];
            stream.Read (buf, 0, ImageSize);
            return buf;
        }

        public void SetData (Stream stream, byte[] data) {
            SetData (stream, data, ThumbnailOffset);
        }

        public void SetData (Stream stream, byte[] data, int offset) {
            if (offset < 0) {
                stream.Seek (0, SeekOrigin.End);
            } else {
                stream.Seek (offset, SeekOrigin.Begin);
            }

            stream.Write (data, 0, data.Length);
        }
    }

    internal class ImageItemRecord : PhotoDbRecord {

        private int unknownOne;
        private int unknownTwo;

        private List<ImageNameRecord> names = new List<ImageNameRecord> ();
        private List<ImageNameRecord> newNames = new List<ImageNameRecord> ();
        private List<ImageNameRecord> removedNames = new List<ImageNameRecord> ();
        
        public int Id;
        public long TrackId;
        public int Rating;
        public DateTime OriginalDate = DateTime.Now;
        public DateTime DigitizedDate = DateTime.Now;
        public int SourceImageSize;

        public IList<ImageNameRecord> Names {
            get {
                return new ReadOnlyCollection<ImageNameRecord> (names);
            }
        }

        public IList<ImageNameRecord> NewNames {
            get {
                return new ReadOnlyCollection<ImageNameRecord> (newNames);
            }
        }

        public IList<ImageNameRecord> RemovedNames {
            get {
                return new ReadOnlyCollection<ImageNameRecord> (removedNames);
            }
        }

        public ImageNameRecord FullName;
        
        public ImageItemRecord (bool isbe) : base (isbe) {
            this.Name = "mhii";
            recordPadding = 100;

            FullName = new ImageNameRecord (isbe);
        }

        public void AddName (ImageNameRecord name) {
            names.Add (name);
            newNames.Add (name);
        }

        public void RemoveName (ImageNameRecord name) {
            names.Remove (name);
            removedNames.Add (name);
        }

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numChildren = ToInt32 (body, 0);
            Id = ToInt32 (body, 4);
            TrackId = ToInt64 (body, 8);
            unknownOne = ToInt32 (body, 16);
            Rating = ToInt32 (body, 20);
            unknownTwo = ToInt32 (body, 24);
            OriginalDate = Utility.MacTimeToDate (ToUInt32 (body, 28));
            DigitizedDate = Utility.MacTimeToDate (ToUInt32 (body, 32));
            SourceImageSize = ToInt32 (body, 36);

            recordPadding = body.Length - 40;

            for (int i = 0; i < numChildren; i++) {
                PhotoDetailRecord detail = new PhotoDetailRecord (IsBE);
                detail.Read (reader);

                if (detail.Type == PhotoDetailType.ThumbnailContainer) {
                    names.Add (detail.ImageName);
                } else {
                    FullName = detail.ImageName;
                }
            }
        }

        public override void Save (BinaryWriter writer) {

            byte[] childBytes;
            int childLen;

            List<PhotoDetailRecord> details = new List<PhotoDetailRecord> ();
            foreach (ImageNameRecord name in names) {
                // if the thumbnail doesn't have any data, don't write it
                if (name.ThumbnailOffset < 0)
                    continue;
                
                details.Add (new PhotoDetailRecord (IsBE, name, PhotoDetailType.ThumbnailContainer));
            }

            if (FullName != null && FullName.FileName != null) {
                details.Insert (0, new PhotoDetailRecord (IsBE, FullName, PhotoDetailType.ImageContainer));
            }
            
            SaveChildren (details, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (52 + recordPadding);
            writer.Write (52 + recordPadding + childLen);
            writer.Write (details.Count);
            writer.Write (Id);
            writer.Write (TrackId);
            writer.Write (unknownOne);
            writer.Write (Rating);
            writer.Write (unknownTwo);
            writer.Write (Utility.DateToMacTime (OriginalDate));
            writer.Write (Utility.DateToMacTime (DigitizedDate));
            writer.Write (SourceImageSize);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class FileListRecord : PhotoDbRecord {

        private Dictionary<int, FileRecord> files = new Dictionary<int, FileRecord> ();

        public IList<FileRecord> Files {
            get { return new ReadOnlyCollection<FileRecord> (new List<FileRecord> (files.Values)); }
        }
        
        public FileListRecord (bool isbe) : base (isbe) {
            this.Name = "mhlf";
            recordPadding = 80;
        }

        public void AddFile (FileRecord file) {
            files[file.CorrelationId] = file;
        }

        public void RemoveFile (int correlationId) {
            files.Remove (correlationId);
        }

        public void RemoveFile (FileRecord file) {
            RemoveFile (file.CorrelationId);
        }

        public FileRecord LookupFile (int correlationId) {
            try {
                return files[correlationId];
            } catch (KeyNotFoundException e) {
                return null;
            }
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            if (HeaderOne - 12 > 0) {
                byte[] body = reader.ReadBytes (HeaderOne - 12);
                recordPadding = body.Length;
            }
            
            int numChildren = HeaderTwo;
            for (int i = 0; i < numChildren; i++) {
                FileRecord record = new FileRecord (IsBE);
                record.Read (reader);
                files[record.CorrelationId] = record;
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChildren (files.Values, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (12 + recordPadding);
            writer.Write (files.Values.Count);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class FileRecord : PhotoDbRecord {

        private int unknownOne;

        public int CorrelationId;
        public int ImageSize;

        public FileRecord (bool isbe) : base (isbe) {
            this.Name = "mhif";
            recordPadding = 100;
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (HeaderOne - 12);
            
            unknownOne = ToInt32 (body, 0);
            CorrelationId = ToInt32 (body, 4);
            ImageSize = ToInt32 (body, 8);

            recordPadding = body.Length - 12;
        }

        public override void Save (BinaryWriter writer) {
            WriteName (writer);
            writer.Write (24 + recordPadding);
            writer.Write (24 + recordPadding);
            writer.Write (unknownOne);
            writer.Write (CorrelationId);
            writer.Write (ImageSize);
            WritePadding (writer);
        }
    }

    public class PhotoSaveProgressArgs : EventArgs {

        private double percent;

        public double Percent {
            get { return percent; }
        }
        
        public PhotoSaveProgressArgs (double percent) {
            this.percent = percent;
        }
    }

    public delegate void PhotoSaveProgressHandler (object o, PhotoSaveProgressArgs args);

    public class PhotoDatabase {

        private const ulong MapSize = 32 * 1024 * 1024;

        private Device device;
        private Dictionary<int, Photo> photos = new Dictionary<int, Photo> ();
        private Dictionary<long, Photo> trackPhotos = new Dictionary<long, Photo> ();
        private List<Album> albums = new List<Album> ();
        private DataFileRecord dfr;
        private bool isPhoto; // is this the photo database or the artwork database

        private Stream tempStream; // place where we can put to-be-saved thumbnails
        private Album masterAlbum;

        private List<Photo> addedPhotos = new List<Photo> ();
        private List<Photo> removedPhotos = new List<Photo> ();

        public event EventHandler SaveStarted;
        public event PhotoSaveProgressHandler SaveProgressChanged;
        public event EventHandler SaveEnded;

        private string PhotoDbPath {
            get {
                if (isPhoto) {
                    return device.MountPoint + "/Photos/Photo Database";
                } else {
                    return device.ControlPath + "/Artwork/ArtworkDB";
                }
            }
        }

        private string PhotoDbBackupPath {
            get {
                return PhotoDbPath + ".bak";
            }
        }
        
        public Device Device {
            get { return device; }
        }

        internal bool IsPhotoDatabase {
            get { return isPhoto; }
        }

        public IList<Album> Albums {
            get { return new ReadOnlyCollection<Album> (albums); }
        }

        public IList<Photo> Photos {
            get {
                return new ReadOnlyCollection<Photo> (new List<Photo> (photos.Values));
            }
        }
        
        internal PhotoDatabase (Device device, bool isPhoto, bool createFresh) {
            this.device = device;
            this.isPhoto = isPhoto;

            if(createFresh && File.Exists (PhotoDbPath)) {
                File.Copy (PhotoDbPath, PhotoDbBackupPath, true);
            }

            Reload (createFresh);
        }

        public void Reload (bool createFresh) {
            photos.Clear ();
            albums.Clear ();

            CloseTempFile ();
            
            dfr = new DataFileRecord (device.IsBE);

            if (isPhoto) {
                // create a default master album if this is a photo database
                masterAlbum = CreateAlbum ();
                masterAlbum.Name = "My Pictures";
                masterAlbum.Record.IsMaster = true;
            }
            
            if (createFresh || !File.Exists (PhotoDbPath))
                return;

            using (BinaryReader reader = new BinaryReader (File.OpenRead (PhotoDbPath))) {
                dfr.Read (reader);

                PhotoDataSetRecord albumSet = dfr[PhotoDataSetIndex.AlbumList];
                ImageListRecord imageList = dfr[PhotoDataSetIndex.ImageList].Data as ImageListRecord;

                foreach (ImageItemRecord item in imageList.Items) {
                    Photo photo = new Photo (item, this);
                    photos[item.Id] = photo;

                    if (item.TrackId != 0) {
                        trackPhotos[item.TrackId] = photo;
                    }
                }

                foreach (AlbumRecord albumRecord in (albumSet.Data as AlbumListRecord).Albums) {
                    Album album = new Album (albumRecord, this);
                    
                    if (album.IsMaster) {
                        masterAlbum = album;
                    } else {
                        albums.Add (album);
                    }
                }
            }
        }

        public void Save () {
            if (SaveStarted != null)
                SaveStarted (this, new EventArgs ());

            try {
                string dbdir = Path.GetDirectoryName (PhotoDbPath);
                if (!Directory.Exists (dbdir))
                    Directory.CreateDirectory (dbdir);
                
                SaveThumbnails ();
                CloseTempFile ();
                
                List<Photo> dirtyPhotos = new List<Photo> ();
                foreach (Photo photo in photos.Values) {
                    if (photo.NeedsCopy) {
                        photo.SetPodFileName ();
                        dirtyPhotos.Add (photo);
                    }
                }
                
                using (BinaryWriter writer = new EndianBinaryWriter (File.Open (PhotoDbPath, FileMode.Create),
                                                                     device.IsBE)) {
                    dfr.Save (writer);
                }

                for (int i = 0; i < dirtyPhotos.Count; i++) {
                    if (SaveProgressChanged != null)
                        SaveProgressChanged (this, new PhotoSaveProgressArgs ((double) i / (double) dirtyPhotos.Count));
                    CopyPhoto (dirtyPhotos[i]);
                }

                if (SaveProgressChanged != null)
                    SaveProgressChanged (this, new PhotoSaveProgressArgs ((double) 1.0));
            } catch (Exception e) {
                throw new DatabaseWriteException (e, "Failed to save database");
            } finally {
                if (SaveEnded != null)
                    SaveEnded (this, new EventArgs ());
            }
        }

        private void CopyPhoto (Photo photo) {
            string src = photo.FullSizeFileName;
            string dest = GetFilesystemPath (photo.Record.FullName.FileName);

            string destdir = Path.GetDirectoryName (dest);
            if (!Directory.Exists (destdir)) {
                Directory.CreateDirectory (destdir);
            }
            
            File.Copy (src, dest, true);
        }

        private Album CreateAlbum () {
            AlbumRecord record = new AlbumRecord (device.IsBE);

            AlbumListRecord albumList = dfr[PhotoDataSetIndex.AlbumList].Data as AlbumListRecord;
            albumList.AddAlbum (record);

            return new Album (record, this);
        }

        public Album CreateAlbum (string name) {
            Album album = CreateAlbum ();
            album.Name = name;
            albums.Add (album);
            return album;
        }

        public void RemoveAlbum (Album album) {
            AlbumListRecord albumList = dfr[PhotoDataSetIndex.AlbumList].Data as AlbumListRecord;
            albumList.RemoveAlbum (album.Record);

            albums.Remove (album);
        }

        public Photo CreatePhoto () {
            ImageListRecord imageList = dfr[PhotoDataSetIndex.ImageList].Data as ImageListRecord;
            ImageItemRecord item = new ImageItemRecord (device.IsBE);
            item.Id = dfr.NextId++;
            
            imageList.AddItem (item);
            
            Photo photo = new Photo (item, this);
            photos[photo.Id] = photo;

            if (masterAlbum != null) {
                masterAlbum.Add (photo);
            }
            
            addedPhotos.Add (photo);

            return photo;
        }

        public void RemovePhoto (Photo photo) {
            photos.Remove (photo.Id);
            trackPhotos.Remove (photo.Record.TrackId);
            addedPhotos.Remove (photo);
            removedPhotos.Add (photo);

            foreach (Album album in albums) {
                album.Remove (photo);
            }

            if (masterAlbum != null) {
                masterAlbum.Remove (photo);
            }
            
            ImageListRecord imageList = dfr[PhotoDataSetIndex.ImageList].Data as ImageListRecord;
            imageList.RemoveItem (photo.Record);
        }

        internal int GetThumbSize (int correlationId) {
            FileListRecord fileList = dfr[PhotoDataSetIndex.FileList].Data as FileListRecord;
            FileRecord file = fileList.LookupFile (correlationId);
            if (file == null)
                return -1;

            return file.ImageSize;
        }

        internal Photo LookupPhotoById (int id) {
            try {
                return photos[id];
            } catch (KeyNotFoundException e) {
                return null;
            }
        }

        internal Photo LookupPhotoByTrackId (long id) {
            try {
                return trackPhotos[id];
            } catch (KeyNotFoundException e) {
                return null;
            }
        }

        internal string GetFilesystemPath (string podpath) {
            return String.Format ("{0}/Photos{1}", device.MountPoint, podpath.Replace(':', '/'));
        }

        internal string GetThumbPath (ArtworkFormat format) {
            if (isPhoto) {
                return String.Format ("{0}/Photos/Thumbs/F{1}_1.ithmb", device.MountPoint, format.CorrelationId);
            } else {
                return String.Format ("{0}/Artwork/F{1}_1.ithmb", device.ControlPath, format.CorrelationId);
            }

        }

        private void FindThumbnails (IList<ImageItemRecord> items, List<ImageNameRecord> existingNames,
                                     List<ImageNameRecord> newNames, List<ImageNameRecord> removedNames,
                                     ArtworkFormat format) {
            foreach (ImageItemRecord item in items) {

                if (existingNames != null) {
                    foreach (ImageNameRecord name in item.Names) {
                        if (name.CorrelationId == format.CorrelationId) {
                            existingNames.Add (name);
                        }
                    }
                }

                if (newNames != null) {
                    foreach (ImageNameRecord name in item.NewNames) {
                        if (name.CorrelationId == format.CorrelationId) {
                            newNames.Add (name);
                        }
                    }
                }

                if (removedNames != null) {
                    foreach (ImageNameRecord name in item.RemovedNames) {
                        if (name.CorrelationId == format.CorrelationId) {
                            removedNames.Add (name);
                        }
                    }
                }
            }
        }

        private void SaveThumbnails () {
            foreach (ArtworkFormat format in device.ArtworkFormats) {
                List<ImageNameRecord> existingNames = new List<ImageNameRecord> ();
                List<ImageNameRecord> removedNames = new List<ImageNameRecord> ();
                List<ImageNameRecord> newNames = new List<ImageNameRecord> ();

                ImageListRecord imageList = dfr[PhotoDataSetIndex.ImageList].Data as ImageListRecord;

                FindThumbnails (imageList.Items, existingNames, newNames, removedNames, format);
                existingNames.Sort (new ImageNameRecordSorter ());
                
                FindThumbnails (imageList.RemovedItems, removedNames, null, removedNames, format);

                if (existingNames.Count == 0 && newNames.Count == 0 && removedNames.Count == 0) {
                    continue;
                }

                SaveThumbnails (existingNames, newNames, removedNames, format);
            }
        }

        private ImageNameRecord Pop (List<ImageNameRecord> list) {
            ImageNameRecord record = Peek (list);
            if (record != null)
                list.Remove (record);

            return record;
        }

        private ImageNameRecord Peek (List<ImageNameRecord> list) {
            if (list == null || list.Count == 0)
                return null;

            return list[list.Count - 1];
        }

        private byte[] GetNameData (ImageNameRecord record, Stream stream) {
            if (record.Dirty) {
                return record.GetData (GetTempFile ());
            } else {
                return record.GetData (stream);
            }
        }

        private void SaveThumbnails (List<ImageNameRecord> existingNames, List<ImageNameRecord> newNames,
                                     List<ImageNameRecord> removedNames, ArtworkFormat format) {
            string thumbPath = GetThumbPath (format);

            int fileLength = 0;
            if (existingNames.Count > 0) {
                ImageNameRecord last = Peek (existingNames);
                fileLength = last.ThumbnailOffset + last.ImageSize;
            }

            string thumbDir = Path.GetDirectoryName (thumbPath);
            if (!Directory.Exists (thumbDir)) {
                Directory.CreateDirectory (thumbDir);
            }

            using (FileStream stream = File.Open (thumbPath, FileMode.OpenOrCreate)) {
                // process the removals, filling the gaps with new or existing records when possible
                foreach (ImageNameRecord removal in removedNames) {

                    // try to replace it with a new one
                    ImageNameRecord replacement = Pop (newNames);
                    
                    if (replacement == null) {

                        // no new ones, try to replace it with an existing one from the end of the file
                        ImageNameRecord record = Peek (existingNames);
                        if (record != null && record.ThumbnailOffset > removal.ThumbnailOffset) {
                            replacement = record;

                            // it was the last one in the file, so we can reduce the file length
                            fileLength -= replacement.ImageSize;
                        }
                    }

                    if (replacement != null) {
                        // write the replacement chunk into the old one's spot
                        byte[] data = GetNameData (replacement, stream);
                        replacement.ThumbnailOffset = removal.ThumbnailOffset;
                        replacement.SetData (stream, data);
                        replacement.Dirty = false;

                        existingNames.Sort (new ImageNameRecordSorter ());
                    } else if (fileLength > removal.ThumbnailOffset) {
                        // we can't fill the gap, but it's ok to reduce the file length
                        fileLength = removal.ThumbnailOffset;
                    }
                }

                // process the additions by appending them to the file
                foreach (ImageNameRecord addition in newNames) {
                    byte[] data = GetNameData (addition, stream);
                    addition.ThumbnailOffset = fileLength;
                    addition.SetData (stream, data);
                    addition.Dirty = false;
                    
                    fileLength += addition.ImageSize;
                }

                // lastly, flush any dirty existing records
                foreach (ImageNameRecord existing in existingNames) {
                    if (existing.Dirty) {
                        byte[] data = GetNameData (existing, stream);
                        existing.SetData (stream, data);
                    }
                }
            }

            Syscall.truncate (thumbPath, (long) fileLength);
        }

        internal Stream GetTempFile () {
            if (tempStream == null) {
                string path = Path.GetTempFileName ();
                tempStream = File.Open (path, FileMode.Create);
                File.Delete (path);
            }

            return tempStream;
        }

        private void CloseTempFile () {
            if (tempStream != null) {
                tempStream.Close ();
                tempStream = null;
            }
        }
    }
}
