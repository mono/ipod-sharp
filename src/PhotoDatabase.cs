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
            MemoryStream stream = new MemoryStream ();
            BinaryWriter writer = new EndianBinaryWriter (stream, IsBE);
            record.Save (writer);
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
            /*
            WriteName (writer);
            writer.Write (this.HeaderOne);
            writer.Write (this.HeaderTwo);
            writer.Write (unknownOne);
            writer.Write (unknownTwo);
            writer.Write (numChildren);
            writer.Write (
            */
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

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);

            for (int i = 0; i < this.HeaderTwo; i++) {
                ImageItemRecord item = new ImageItemRecord (IsBE);
                item.Read (reader);

                items.Add (item);
            }
        }

        public override void Save (BinaryWriter writer) {

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

        public override void Read (BinaryReader reader) {
            base.Read (reader);

            byte[] body = reader.ReadBytes (this.HeaderOne - 12);
            Type = (PhotoDetailType) ToInt16 (body, 0);

            switch (Type) {
            case PhotoDetailType.ThumbnailContainer:
                ImageName = new ImageNameRecord (IsBE);
                ImageName.Read (reader);
                break;
            default:
                ReadString (reader, Type == PhotoDetailType.FileName);
                break;
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

        public override void Save (BinaryWriter writer) {

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

        public ThumbnailFormat Format;

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

            SetFormat ();
        }

        private void SetFormat () {
            switch (CorrelationID) {
            case 1009:
            case 1015:
            case 1013:
            case 1036:
                Format = ThumbnailFormat.Rgb565;
                break;
            case 1019:
                Format = ThumbnailFormat.IYUV;
                break;
            case 1020:
                Format = ThumbnailFormat.Rgb565BE;
                break;
            case 1024:
                Format = ThumbnailFormat.Rgb565BE90;
                break;
            default:
                throw new ApplicationException ("Unknown correltion id: " + CorrelationID);
            }
        }

        public override void Save (BinaryWriter writer) {

        }
    }

    internal class ImageItemRecord : PhotoDbRecord {

        private int unknownOne;
        private int unknownTwo;
        private int sourceImageSize;

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
                names.Add (detail.ImageName);
            }
        }

        public override void Save (BinaryWriter writer) {

        }
    }

    internal class FileListRecord : PhotoDbRecord {

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
            }
        }

        public override void Save (BinaryWriter writer) {

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

        internal Image LookupImageById (int id) {
            return images[id];
        }
    }

}
