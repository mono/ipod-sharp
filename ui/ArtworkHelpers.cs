using System;


namespace IPod {

    public class ArtworkHelpers {

        private ArtworkHelpers () {
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
                    i = row * width * 2;
				
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

        private static void UnpackRgb565 (byte[] data, Gdk.Pixbuf dest, bool isbe) {
            unsafe {
                byte * pixels;
                int row, col;
                ushort s;
			
                bool flip = isbe == System.BitConverter.IsLittleEndian;

                int offset = 0;
                for (row = 0; row < dest.Height; row++) {
                    pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
                    for (col = 0; col < dest.Width; col++) {
                        s = BitConverter.ToUInt16 (data, offset);
                        offset+=2;

                        if (flip)
                            s = (ushort) ((s >> 8) | (s << 8));
					
                        *(pixels++) = (byte)(((s >> 8) & 0xf8) | ((s >> 13) & 0x7)); // r
                        *(pixels++) = (byte)(((s >> 3) & 0xfc) | ((s >> 9) & 0x3));  // g
                        *(pixels++) = (byte)(((s << 3) & 0xf8) | ((s >> 2) & 0x7));  // b
                    }
                }
            }
        }

        private static Gdk.Pixbuf ToPixbuf (ArtworkFormat format, byte[] data) {
            Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, format.Width, format.Height);

            // FIXME: sometimes needs to be big endian, or YUV
            UnpackRgb565 (data, pixbuf, false);

            return pixbuf;
        }

        private static byte[] ToBytes (ArtworkFormat format, Gdk.Pixbuf pixbuf) {
            bool disposePixbuf = false;
            
            // FIXME: preserve aspect ratio
            if (pixbuf.Height > format.Height || pixbuf.Width > format.Width) {
                pixbuf = pixbuf.ScaleSimple (format.Width, format.Height,
                                             Gdk.InterpType.Bilinear);
                disposePixbuf = true;
            }
            
            // FIXME: sometimes it needs to be big endian or YUV
            byte[] data = PackRgb565 (pixbuf, false);
            if (disposePixbuf) {
                pixbuf.Dispose ();
            }

            return data;
        }

        public static Gdk.Pixbuf GetCoverArt (Track track, ArtworkFormat format) {
            byte[] data = track.GetCoverArt (format);
            if (data == null)
                return null;

            return ToPixbuf (format, data);
        }

        public static void SetCoverArt (Track track, ArtworkFormat format, Gdk.Pixbuf pixbuf) {
            byte[] data = ToBytes (format, pixbuf);
            track.SetCoverArt (format, data);
        }

        public static Gdk.Pixbuf GetThumbnail (Thumbnail thumbnail) {
            byte[] data = thumbnail.GetData ();
            if (data == null)
                return null;

            return ToPixbuf (thumbnail.Format, data);
        }
        
        public static void SetThumbnail (Thumbnail thumbnail, Gdk.Pixbuf pixbuf) {
            byte[] data = ToBytes (thumbnail.Format, pixbuf);
            thumbnail.SetData (data);
        }
    }
}
