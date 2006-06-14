using System;
using System.IO;

namespace IPod {

    public enum ThumbnailFormat {
        Rgb565,
        IYUV,
        Rgb565BE,
        Rgb565BE90
    }

    public class Thumbnail {

        private Image image;
        private ImageNameRecord record;

        private ArtworkFormat format = null;

        public Image Image {
            get { return image; }
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
                if (format == null) {
                    foreach (ArtworkFormat f in image.Device.ArtworkFormats) {
                        if (f.CorrelationId == record.CorrelationID) {
                            format = f;
                            break;
                        }
                    }
                }

                return format;
            }
        }
        
        internal Thumbnail (Image image, ImageNameRecord record) {
            this.image = image;
            this.record = record;
        }

        private string GetThumbPath (string file) {
            return String.Format ("{0}/Photos{1}", image.Device.MountPoint,
                                  file.Replace (":", "/"));
        }

        public byte[] GetData () {
            string file = GetThumbPath (record.FileName);

            using (FileStream stream = File.Open (file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                stream.Seek ((long) record.ThumbnailOffset, SeekOrigin.Begin);

                byte[] buf = new byte[record.ImageSize];
                stream.Read (buf, 0, record.ImageSize);
                return buf;
            }
        }
    }
}
