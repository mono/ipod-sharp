
using System;
using System.Collections;

namespace IPod {

    public delegate void PlaylistSongHandler (object o, int index, Song song);
    
    public class Playlist {

        private TrackDatabase db;
        private PlaylistRecord record;
        private ArrayList otgsongs;
        private string otgtitle;

        public event PlaylistSongHandler SongAdded;
        public event PlaylistSongHandler SongRemoved;

        internal PlaylistRecord PlaylistRecord {
            get { return record; }
        }

        internal Playlist (TrackDatabase db, string title, Song[] otgsongs) {
            this.otgtitle = title;
            this.otgsongs = new ArrayList (otgsongs);
        }
        
        internal Playlist (TrackDatabase db, PlaylistRecord record) {
            this.db = db;
            this.record = record;
        }

        public Song[] Songs {
            get {
                if (IsOnTheGo)
                    return (Song[]) otgsongs.ToArray (typeof (Song));
                    
                ArrayList songs = new ArrayList ();

                foreach (PlaylistItemRecord item in record.Items) {
                    Song song = db.GetSongById (item.TrackId);

                    if (song == null) {
                        Console.Error.WriteLine ("Playlist '{0}' contains invalid song id '{0}'",
                                                 Name, item.TrackId);
                        continue;
                    }

                    songs.Add (song);
                }
                
                return (Song[]) songs.ToArray (typeof (Song));
            }
        }

        public Song this[int index] {
            get {
                return db.GetSongById (record.Items[index].TrackId);
            } set {
                record.Items[index].TrackId = value.Id;
            }
        }

        public string Name {
            get {
                if (otgtitle != null)
                    return otgtitle;
                
                return record.PlaylistName;
            } set {
                if (IsOnTheGo)
                    throw new InvalidOperationException ("The On-The-Go playlist cannot be modified");
                
                record.PlaylistName = value;
            }
        }

        public bool IsOnTheGo {
            get { return otgsongs != null; }
        }

        public void InsertSong (int index, Song song) {
            if (IsOnTheGo)
                throw new InvalidOperationException ("The On-The-Go playlist cannot be modified");
            
            PlaylistItemRecord item = record.CreateItem ();
            item.TrackId = song.Id;

            record.InsertItem (index, item);

            if (SongAdded != null)
                SongAdded (this, index, song);
        }

        public void Clear () {
            record.Clear ();
        }
        
        public void AddSong (Song song) {
            InsertSong (-1, song);
        }

        public void RemoveSong (int index) {
            if (IsOnTheGo)
                throw new InvalidOperationException ("The On-The-Go playlist cannot be modified");

            Song song = this[index];
            record.RemoveItem (index);

            if (SongRemoved != null)
                SongRemoved (this, index, song);
        }

        public bool RemoveSong (Song song) {
            int index;
            bool ret = false;
            
            while ((index = IndexOf (song)) >= 0) {
                ret = true;
                RemoveSong (index);
            }

            return ret;
        }

        internal bool RemoveOTGSong (Song song) {
            if (!otgsongs.Contains (song))
                return false;

            otgsongs.Remove (song);
            return true;
        }

        public int IndexOf (Song song) {
            return record.IndexOf (song.Id);
        }
    }

}
