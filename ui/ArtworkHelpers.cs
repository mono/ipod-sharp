using System;
using System.IO;

namespace IPod {

    public class ArtworkHelpers {

        private ArtworkHelpers () {
        }

        private static int Clamp (int val, int bottom, int top)
	{
            if (val < bottom)
                return bottom;
            else if (val > top)
                return top;

            return val;
	}

	private static void UnpackYUV (ushort y, ushort u, ushort v, out int r, out int g, out int b)
	{
            r = Clamp ((int) (y + (1.370705 * (v - 128))), 0, 255); // r
            g = Clamp ((int) (y - (0.698001 * (v - 128)) - (0.3337633 * (u - 128))), 0, 255); // g
            b = Clamp ((int) (y + (1.732446 * (u -128))), 0, 255); // b
	}

	private static void UnpackIYUV (byte[] data, Gdk.Pixbuf dest)
	{
            BinaryReader reader = new BinaryReader (new MemoryStream (data));
            
            unsafe {
                byte * pixels;
                ushort y0, y1, u, v;
                int r, g, b;
                int row, col;

                for (row = 0; row < dest.Height; row += 2) {
                    pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
                    for (col = 0; col < dest.Width; col += 2) {
                        u = reader.ReadByte ();
                        y0 = reader.ReadByte ();
                        v = reader.ReadByte ();
                        y1 = reader.ReadByte ();
					 
                        UnpackYUV (y0, u, v, out r, out g, out b);
                        *(pixels ++) = (byte) r;
                        *(pixels ++) = (byte) g;
                        *(pixels ++) = (byte) b;
					 
                        UnpackYUV (y1, u, v, out r, out g, out b);
                        *(pixels ++) = (byte) r;
                        *(pixels ++) = (byte) g;
                        *(pixels ++) = (byte) b;
                    }
                }
                for (row = 1; row < dest.Height; row += 2) {
                    pixels = ((byte *)dest.Pixels) + row * dest.Rowstride;
                    for (col = 0; col < dest.Width; col += 2) {
                        u = reader.ReadByte ();
                        y0 = reader.ReadByte ();
                        v = reader.ReadByte ();
                        y1 = reader.ReadByte ();

                        UnpackYUV (y0, u, v, out r, out g, out b);
                        *(pixels ++) = (byte) r;
                        *(pixels ++) = (byte) g;
                        *(pixels ++) = (byte) b;
					 
                        UnpackYUV (y1, u, v, out r, out g, out b);
                        *(pixels ++) = (byte) r;
                        *(pixels ++) = (byte) g;
                        *(pixels ++) = (byte) b;
                    }
                }
            }

            reader.Close ();
        }
	
	private static byte[] PackIYUV (Gdk.Pixbuf src) {
            int row, col;
            int r, g, b;
            int y, u, v;
            byte[] packed;
            int i;
            int width = src.Width;
            int height = src.Height;

            unsafe {
                byte * pixels;

                packed = new byte [width * height * 2];
                for (row = 0; row < height; row ++) {
                    pixels = ((byte *)src.Pixels) + row * src.Rowstride;
                    i = row * width;
                    if (row % 2 > 0)
                        i += (height - 1)  * width;
				
                    for (col = 0; col < width; col ++) {
                        r = *(pixels ++);
                        g = *(pixels ++);
                        b = *(pixels ++);

                        // These were taken directly from the jfif spec
                        y  = (int) (0.299  * r + 0.587  * g + 0.114  * b);
                        u  = (int) (-0.1687 * r - 0.3313 * g + 0.5    * b + 128);
                        v  = (int) (0.5    * r - 0.4187 * g - 0.0813 * b + 128);

                        y = Clamp (y, 0, 255);
                        u = Clamp (u, 0, 255);
                        v = Clamp (v, 0, 255);

                        byte[] sbytes;
                                        
                        if (col % 2 > 0)
                            sbytes = BitConverter.GetBytes ((ushort) ((y << 8) | v));
                        else
                            sbytes = BitConverter.GetBytes ((ushort) ((y << 8) | u));

                        packed[i++] = sbytes[0];
                        packed[i++] = sbytes[1];
                    }
                }
            }

            return packed;
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

            if (format.Type == ArtworkType.PhotoTvScreen) {
                UnpackIYUV (data, pixbuf);
            } else {
                // FIXME: this is totally lame
                UnpackRgb565 (data, pixbuf, format.Width == 176);
            }

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
            
            byte[] data;
            if (format.Type == ArtworkType.PhotoTvScreen) {
                data = PackIYUV (pixbuf);
            } else {
                // FIXME: this is totally lame
                data = PackRgb565 (pixbuf, format.Width == 176);
            }
            
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
