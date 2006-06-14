using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IPod {

    public class Image {

        private ImageItemRecord item;
        private Device device;
        private List<Thumbnail> thumbnails = new List<Thumbnail> ();

        internal ImageItemRecord Record {
            get { return item; }
        }

        internal Device Device {
            get { return device; }
        }

        public DateTime OriginalDate {
            get { return item.OriginalDate; }
            set { item.OriginalDate = value; }
        }

        public DateTime DigitizedDate {
            get { return item.DigitizedDate; }
            set { item.DigitizedDate = value; }
        }

        // FIXME: use an enum or whatever
        public int Rating {
            get { return item.Rating; }
            set { item.Rating = value; }
        }

        public ReadOnlyCollection<Thumbnail> Thumbnails {
            get { return new ReadOnlyCollection<Thumbnail> (thumbnails); }
        }
        
        internal Image (ImageItemRecord item, Device device) {
            this.item = item;
            this.device = device;

            foreach (ImageNameRecord name in item.Names) {
                thumbnails.Add (new Thumbnail (this, name));
            }
        }
    }

}
