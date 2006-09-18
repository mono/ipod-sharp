using System;
using System.IO;

namespace IPod {

    /*
    public enum ThumbnailFormat {
        Rgb565,
        IYUV,
        Rgb565BE,
        Rgb565BE90
    }
    */

    public class Thumbnail {

        private Photo photo;
        private ImageNameRecord record;

        private ArtworkFormat format = null;

        public Photo Photo {
            get { return photo; }
        }

        public int Size {
            get { return record.ImageSize; }
            set { record.ImageSize = value; }
        }

        public short VerticalPadding {
            get { return record.VerticalPadding; }
            set { record.VerticalPadding = value; }
        }

        public short HorizontalPadding {
            get { return record.HorizontalPadding; }
            set { record.HorizontalPadding = value; }
        }

        public short Height {
            get { return record.ImageHeight; }
            set { record.ImageHeight = value; }
        }

        public short Width {
            get { return record.ImageWidth; }
            set { record.ImageWidth = value; }
        }

        public ArtworkFormat Format {
            get {
                return format;
            } set {
                if (value == null)
                    throw new ArgumentNullException ("Format cannot be null");
                
                format = value;
                record.CorrelationId = format.CorrelationId;
                record.SetThumbFileName (photo.PhotoDatabase.IsPhotoDatabase);
            }
        }

        internal ImageNameRecord Record {
            get { return record; }
        }
        
        internal Thumbnail (Photo photo, ImageNameRecord record) {
            this.photo = photo;
            this.record = record;

            if (record.CorrelationId > 0) {
                Format = photo.PhotoDatabase.Device.LookupArtworkFormat (record.CorrelationId);
            }
        }

        public byte[] GetData () {
            if (record.Dirty) {
                return record.GetData (photo.PhotoDatabase.GetTempFile ());
            } else {
                string file = photo.PhotoDatabase.GetThumbPath (Format);
                
                using (FileStream stream = File.Open (file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    return record.GetData (stream);
                }
            }
        }

        public void SetData (byte[] data) {
            if (data.Length < format.Size)
                throw new ArgumentException (String.Format ("Expected data length of {0}, but got {1}",
                                                            format.Size, data.Length));
            
            Stream stream = photo.PhotoDatabase.GetTempFile ();
            stream.Seek (0, SeekOrigin.End);
            
            record.ThumbnailOffset = (int) stream.Position;
            record.ImageSize = format.Size;
            
            stream.Write (data, 0, format.Size);
            
            record.Dirty = true;
        }
    }
}
