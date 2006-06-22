using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IPod {

    public class Photo {

        private ImageItemRecord item;
        private PhotoDatabase db;
        private List<Thumbnail> thumbnails = new List<Thumbnail> ();
        private string fullSizeFile;
        private bool needsCopy = false;

        internal int Id {
            get { return item.Id; }
        }
        
        internal ImageItemRecord Record {
            get { return item; }
        }

        internal PhotoDatabase PhotoDatabase {
            get { return db; }
        }

        internal bool NeedsCopy {
            get { return needsCopy; }
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

        public IList<Thumbnail> Thumbnails {
            get { return new ReadOnlyCollection<Thumbnail> (thumbnails); }
        }

        public string FullSizeFileName {
            get {
                if (fullSizeFile != null) {
                    return fullSizeFile;
                } else if (item.FullName == null) {
                    return null;
                } else {
                    return db.GetFilesystemPath (item.FullName.FileName);
                }
            } set {
                fullSizeFile = value;
                needsCopy = true;
            }
        }
        
        internal Photo (ImageItemRecord item, PhotoDatabase db) {
            this.item = item;
            this.db = db;

            foreach (ImageNameRecord name in item.Names) {
                thumbnails.Add (new Thumbnail (this, name));
            }
        }

        internal void SetPodFileName () {
            item.FullName.FileName = String.Format (":Full Resolution:{0}:{1}:{2}:{3}", OriginalDate.Year,
                                                    OriginalDate.Month, OriginalDate.Day,
                                                    Path.GetFileName (fullSizeFile));
        }

        public Thumbnail CreateThumbnail () {
            ImageNameRecord name = new ImageNameRecord (item.IsBE);
            item.AddName (name);
            
            Thumbnail thumbnail = new Thumbnail (this, name);
            thumbnails.Add (thumbnail);

            return thumbnail;
        }

        public void RemoveThumbnail (Thumbnail thumbnail) {
            item.RemoveName (thumbnail.Record);
            thumbnails.Remove (thumbnail);
        }

        public Thumbnail LookupThumbnail (ArtworkFormat format) {
            foreach (Thumbnail thumbnail in thumbnails) {
                if (thumbnail.Format == format) {
                    return thumbnail;
                }
            }

            return null;
        }
    }

}
