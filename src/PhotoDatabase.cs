using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace IPod {

    internal abstract class PhotoDbRecord : Record {

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
    }

    internal class DataFileRecord : PhotoDbRecord {

        private int unknownOne;
        private int unknownTwo;
        private int unknownThree;
        private int unknownFour;
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
            writer.Write (68 + PadLength);
            writer.Write (68 + PadLength + childLen);
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

        public PhotoDataSetRecord (bool isbe) : base (isbe) {
            this.Name = "mhsd";
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            Index = (PhotoDataSetIndex) ToInt32 (body, 0);

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
            writer.Write (16 + PadLength);
            writer.Write (16 + PadLength + childLen);
            writer.Write ((int) Index);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class AlbumListRecord : PhotoDbRecord {

        private List<AlbumRecord> albums = new List<AlbumRecord> ();

        public ReadOnlyCollection<AlbumRecord> Albums {
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
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            if (HeaderOne - 12 > 0) {
                reader.ReadBytes (HeaderOne - 12);
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
            writer.Write (12 + PadLength);
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

        private List<AlbumItemRecord> items = new List<AlbumItemRecord> ();
        
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

        public ReadOnlyCollection<AlbumItemRecord> Items {
            get {
                return new ReadOnlyCollection<AlbumItemRecord> (items);
            }
        }

        public string AlbumName {
            get { return nameRecord.Value; }
            set { nameRecord.Value = value; }
        }

        public AlbumRecord (bool isbe) : base (isbe) {
            this.Name = "mhba";
            nameRecord = new PhotoDetailRecord (IsBE);
        }

        public void AddItem (AlbumItemRecord item) {
            items.Add (item);
        }

        public void RemoveItem (AlbumItemRecord item) {
            items.Remove (item);
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

            for (int i = 0; i < numDataObjects; i++) {
                PhotoDetailRecord detail = new PhotoDetailRecord (IsBE);
                detail.Read (reader);

                if (i == 0) {
                    nameRecord = detail;
                }
            }

            for (int i = 0; i < numAlbums; i++) {
                AlbumItemRecord item = new AlbumItemRecord (IsBE);
                item.Read (reader);
                items.Add (item);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] detailBytes;
            int detailLen;

            SaveChild (nameRecord, out detailBytes, out detailLen);

            byte[] childBytes;
            int childLen;

            SaveChildren (items, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (64 + PadLength);
            writer.Write (64 + PadLength + detailLen + childLen);
            writer.Write (1);
            writer.Write (items.Count);
            writer.Write (PlaylistId);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (IsMaster ? (byte) 1 : (byte) 0);
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

        public int ImageId;

        public AlbumItemRecord (bool isbe) : base (isbe) {
            this.Name = "mhia";
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (HeaderOne - 12);

            unknownOne = ToInt32 (body, 0);
            ImageId = ToInt32 (body, 4);
        }

        public override void Save (BinaryWriter writer) {
            WriteName (writer);
            writer.Write (20 + PadLength);
            writer.Write (20 + PadLength);
            writer.Write (unknownOne);
            writer.Write (ImageId);
            WritePadding (writer);
        }
    }


    internal class ImageListRecord : PhotoDbRecord {

        private List<ImageItemRecord> items = new List<ImageItemRecord> ();

        public ReadOnlyCollection<ImageItemRecord> Items {
            get {
                return new ReadOnlyCollection<ImageItemRecord> (items);
            }
        }

        public ImageListRecord (bool isbe) : base (isbe) {
            this.Name = "mhli";
        }

        public void AddItem (ImageItemRecord item) {
            items[item.Id] = item;
        }

        public void RemoveItem (ImageItemRecord item) {
            items.Remove (item);
        }

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            reader.ReadBytes (this.HeaderOne - 12);

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
            writer.Write (12 + PadLength);
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
            writer.Write (utf16 ? 2 : 0);
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
                int totalLength = 16 + PadLength + childLen;
                while (totalLength%4 != 0) {
                    totalLength++;
                    childPadding++;
                }
            }

            WriteName (writer);
            writer.Write (16 + PadLength);
            writer.Write (16 + PadLength + childLen + childPadding);
            writer.Write ((short) Type);
            writer.Write ((short) 2);
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

    internal class ImageNameRecord : PhotoDbRecord {

        private PhotoDetailRecord fileDetail;
        
        public int CorrelationID;
        public int ThumbnailOffset;
        public int ImageSize;
        public short VerticalPadding;
        public short HorizontalPadding;
        public short ImageHeight;
        public short ImageWidth;

        public string FileName {
            get { return fileDetail.Value; }
            set { fileDetail.Value = value; }
        }

        public ImageNameRecord (bool isbe) : base (isbe) {
            this.Name = "mhni";

            fileDetail = new PhotoDetailRecord (IsBE);
        }

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            int numChildren = ToInt32 (body, 0);
            CorrelationID = ToInt32 (body, 4);
            ThumbnailOffset = ToInt32 (body, 8);
            ImageSize = ToInt32 (body, 12);
            VerticalPadding = ToInt16 (body, 16);
            HorizontalPadding = ToInt16 (body, 18);
            ImageHeight = ToInt16 (body, 20);
            ImageWidth = ToInt16 (body, 22);

            for (int i = 0; i < numChildren; i++) {
                PhotoDetailRecord detail = new PhotoDetailRecord (IsBE);
                detail.Read (reader);

                if (i == 0) {
                    fileDetail = detail;
                }
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChild (fileDetail, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (36 + PadLength);
            writer.Write (36 + PadLength + childLen);
            writer.Write (1);
            writer.Write (CorrelationID);
            writer.Write (ThumbnailOffset);
            writer.Write (ImageSize);
            writer.Write (VerticalPadding);
            writer.Write (HorizontalPadding);
            writer.Write (ImageHeight);
            writer.Write (ImageWidth);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class ImageItemRecord : PhotoDbRecord {

        private int unknownOne;
        private int unknownTwo;
        private int sourceImageSize;
        private ImageNameRecord fullName;

        private List<ImageNameRecord> names = new List<ImageNameRecord> ();
        
        public int Id;
        public long TrackId;
        public int Rating;
        public DateTime OriginalDate;
        public DateTime DigitizedDate;

        public ReadOnlyCollection<ImageNameRecord> Names {
            get {
                return new ReadOnlyCollection<ImageNameRecord> (names);
            }
        }
        
        public ImageItemRecord (bool isbe) : base (isbe) {
            this.Name = "mhii";
        }

        public void AddName (ImageNameRecord name) {
            names.Add (name);
        }

        public void RemoveName (ImageNameRecord name) {
            names.Remove (name);
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
            sourceImageSize = ToInt32 (body, 36);

            for (int i = 0; i < numChildren; i++) {
                PhotoDetailRecord detail = new PhotoDetailRecord (IsBE);
                detail.Read (reader);

                if (detail.Type == PhotoDetailType.ThumbnailContainer) {
                    names.Add (detail.ImageName);
                } else {
                    fullName = detail.ImageName;
                }
            }
        }

        public override void Save (BinaryWriter writer) {

            byte[] childBytes;
            int childLen;

            List<PhotoDetailRecord> details = new List<PhotoDetailRecord> ();
            foreach (ImageNameRecord name in names) {
                details.Add (new PhotoDetailRecord (IsBE, name, PhotoDetailType.ThumbnailContainer));
            }

            if (fullName != null) {
                details.Add (new PhotoDetailRecord (IsBE, fullName, PhotoDetailType.ImageContainer));
            }
            
            SaveChildren (details, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (52 + PadLength);
            writer.Write (52 + PadLength + childLen);
            writer.Write (details.Count);
            writer.Write (Id);
            writer.Write (TrackId);
            writer.Write (unknownOne);
            writer.Write (Rating);
            writer.Write (unknownTwo);
            writer.Write (Utility.DateToMacTime (OriginalDate));
            writer.Write (Utility.DateToMacTime (DigitizedDate));
            writer.Write (sourceImageSize);
            WritePadding (writer);
            writer.Write (childBytes, 0, childLen);
        }
    }

    internal class FileListRecord : PhotoDbRecord {

        private List<FileRecord> files = new List<FileRecord> ();

        public ReadOnlyCollection<FileRecord> Files {
            get { return new ReadOnlyCollection<FileRecord> (files); }
        }
        
        public FileListRecord (bool isbe) : base (isbe) {
            this.Name = "mhlf";
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            if (HeaderOne - 12 > 0) {
                reader.ReadBytes (HeaderOne - 12);
            }
            
            int numChildren = HeaderTwo;
            for (int i = 0; i < numChildren; i++) {
                FileRecord record = new FileRecord (IsBE);
                record.Read (reader);
                files.Add (record);
            }
        }

        public override void Save (BinaryWriter writer) {
            byte[] childBytes;
            int childLen;

            SaveChildren (files, out childBytes, out childLen);

            WriteName (writer);
            writer.Write (12 + PadLength);
            writer.Write (files.Count);
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
        }
        
        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (HeaderOne - 12);
            
            unknownOne = ToInt32 (body, 0);
            CorrelationId = ToInt32 (body, 4);
            ImageSize = ToInt32 (body, 8);
        }

        public override void Save (BinaryWriter writer) {
            WriteName (writer);
            writer.Write (24 + PadLength);
            writer.Write (24 + PadLength);
            writer.Write (unknownOne);
            writer.Write (CorrelationId);
            writer.Write (ImageSize);
            WritePadding (writer);
        }
    }

    public class PhotoDatabase {

        private Device device;
        private Dictionary<int, Image> images = new Dictionary<int, Image> ();
        private List<Album> albums = new List<Album> ();
        private DataFileRecord dfr;

        private Album masterAlbum;

        private string PhotoDbPath {
            get {
                return device.MountPoint + "/Photos/Photo Database";
            }
        }

        private string PhotoDbBackupPath {
            get {
                return PhotoDbPath + ".bak";
            }
        }

        public ReadOnlyCollection<Album> Albums {
            get { return new ReadOnlyCollection<Album> (albums); }
        }

        public ReadOnlyCollection<Image> Images {
            get {
                return new ReadOnlyCollection<Image> (new List<Image> (images.Values));
            }
        }
        
        internal PhotoDatabase (Device device) : this (device, false) {
        }

        internal PhotoDatabase (Device device, bool createFresh) {
            this.device = device;

            // FIXME: do something with createFresh

            Reload ();
        }

        public void Reload () {
            images.Clear ();
            albums.Clear ();
            
            dfr = new DataFileRecord (false);

            using (BinaryReader reader = new BinaryReader (File.Open (PhotoDbPath, FileMode.Open))) {
                dfr.Read (reader);

                PhotoDataSetRecord albumSet = dfr[PhotoDataSetIndex.AlbumList];
                ImageListRecord imageList = dfr[PhotoDataSetIndex.ImageList].Data as ImageListRecord;

                foreach (ImageItemRecord image in imageList.Items) {
                    images[image.Id] = new Image (image, device);
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
            using (BinaryWriter writer = new BinaryWriter (File.Open (PhotoDbPath, FileMode.Create))) {
                dfr.Save (writer);
            }
        }

        internal Image LookupImageById (int id) {
            return images[id];
        }
    }

}
