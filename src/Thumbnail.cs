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
                Format = photo.PhotoDatabase.Device.LookupFormat (record.CorrelationId);
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
            int expectedLength = photo.PhotoDatabase.GetThumbSize (format.CorrelationId);
            if (expectedLength > 0 && data.Length != expectedLength)
                throw new ArgumentException (String.Format ("Expected data length of {0}, but got {1}",
                                                            expectedLength, data.Length));
            
            Stream stream = photo.PhotoDatabase.GetTempFile ();
            stream.Seek (0, SeekOrigin.End);
            
            record.ThumbnailOffset = (int) stream.Position;
            record.ImageSize = data.Length;
            
            stream.Write (data, 0, data.Length);
            
            record.Dirty = true;
        }

        private static byte[] PackRgb565 (Gdk.Pixbuf src, bool IsBigEndian) {
            int row, col;
            byte r, g, b;
            byte[] packed;
            int i;
            int width = src.Width;
            int height = src.Height;
            ushort s;

            bool flip = IsBigEndian == System.BitConverter.IsLittleEndian;

            unsafe {
                byte * pixels;			 

                packed = new byte[width * height * 2];
            
                for (row = 0; row < height; row ++) {
                    pixels = ((byte *)src.Pixels) + row * src.Rowstride;
                    i = row * width;
				
                    for (col = 0; col < width; col ++) {
                        r = *(pixels ++);
                        g = *(pixels ++);
                        b = *(pixels ++);
					
                        s = (ushort) (((r & 0xf8) << 8) | ((g & 0xfc) << 3) | (b >> 3));

                        if (flip)
                            s = (ushort)((s >> 8) | (s << 8));

                        byte[] sbytes = BitConverter.GetBytes (s);
                    
                        packed[i++] = sbytes[0];
                        packed[i++] = sbytes[1];
                    }
                }
            }

            return packed;
        }

        public void SetDataFromPixbuf (Gdk.Pixbuf pixbuf) {
            // FIXME: sometimes it needs to be big endian, or YUV
            byte[] data = PackRgb565 (pixbuf, false);
            SetData (data);
        }
    }
}
