using System;

namespace IPod
{
    public enum ArtworkUsage : int
    {
        Unknown = -1,
        Photo,
        Cover,
        Chapter,
    }

    public enum PixelFormat : int
    {
        Unknown = -1,
        Rgb565,
        Rgb565BE,
        IYUV
    }

    public class ArtworkFormat
    {
        private ArtworkUsage usage;
        private short width;
        private short height;
        private short correlationId;
        private int size;
        private PixelFormat pformat;
        private short rotation;

        public ArtworkFormat (ArtworkUsage usage, short width, short height, short correlationId,
            int size, PixelFormat pformat, short rotation)
        {
            this.usage = usage;
            this.width = width;
            this.height = height;
            this.correlationId = correlationId;
            this.size = size;
            this.pformat = pformat;
            this.rotation = rotation;
        }
        
        public ArtworkUsage Usage {
            get { return usage; }
        }

        public short Width {
            get { return width; }
        }

        public short Height {
            get { return height; }
        }

        public int Size {
            get { return size; }
        }

        public PixelFormat PixelFormat {
            get { return pformat; }
        }

        public short Rotation {
            get { return rotation; }
        }

        public short CorrelationId {
            get { return correlationId; }
        }
    }
}
