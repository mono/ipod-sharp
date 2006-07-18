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
        private bool dirty = false;

        internal int Id {
            get { return item.Id; }
        }
        
        internal ImageItemRecord Record {
            get { return item; }
        }

        public PhotoDatabase PhotoDatabase {
            get { return db; }
        }

        internal bool Dirty {
            get { return dirty; }
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
                if (dirty) {
                    return fullSizeFile;
                } else {
                    return CurrentFullSizeFileName;
                }
            } set {
                fullSizeFile = value;
                dirty = true;
            }
        }

        internal string CurrentFullSizeFileName {
            get {
                if (item.FullName == null || item.FullName.FileName == null)
                    return null;

                return db.GetFilesystemPath (item.FullName.FileName);
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
            if (fullSizeFile != null) {
                item.FullName.FileName = String.Format (":Full Resolution:{0}:{1}:{2}:{3}", OriginalDate.Year,
                                                        OriginalDate.Month, OriginalDate.Day,
                                                        Path.GetFileName (fullSizeFile));
            } else {
                item.FullName = null;
            }
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
