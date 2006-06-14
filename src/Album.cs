using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace IPod {

    public class Album {

        private AlbumRecord record;
        private PhotoDatabase db;

        private List<Image> images;

        public ReadOnlyCollection<Image> Images {
            get {
                return new ReadOnlyCollection<Image> (images);
            }
        }

        public string Name {
            get { return record.AlbumName; }
            set { record.AlbumName = value; }
        }

        internal bool IsMaster {
            get { return record.IsMaster; }
        }

        internal AlbumRecord Record {
            get { return record; }
        }
        
        internal Album (AlbumRecord record, PhotoDatabase db) {
            this.record = record;
            this.db = db;

            images = new List<Image> ();

            foreach (AlbumItemRecord item in record.Items) {
                Image img = db.LookupImageById (item.ImageId);
                images.Add (img);
            }
        }
    }
}
