
using System;
using System.Collections;

namespace IPod {

    public class Playlist {

        private SongDatabase db;
        private PlaylistRecord record;
        internal Song[] otgsongs = null;

        internal PlaylistRecord PlaylistRecord {
            get { return record; }
        }

        internal Playlist (SongDatabase db, Song[] otgsongs) {
            this.otgsongs = otgsongs;
        }
        
        internal Playlist (SongDatabase db, PlaylistRecord record) {
            this.db = db;
            this.record = record;
        }

        public Song[] Songs {
            get {
                if (IsOnTheGo)
                    return otgsongs;
                    
                ArrayList songs = new ArrayList ();

                foreach (PlaylistItemRecord item in record.Items) {
                    Song song = db.GetSongById (item.TrackId);

                    if (song == null)
                        throw new ApplicationException (String.Format ("Song with id '{0}' was not found",
                                                                       item.TrackId));

                    songs.Add (song);
                }
                
                return (Song[]) songs.ToArray (typeof (Song));
            }
        }

        public string Name {
            get {
                if (IsOnTheGo)
                    return "On-The-Go";
                
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
            
            PlaylistItemRecord item = new PlaylistItemRecord ();
            item.TrackId = song.Id;

            record.InsertItem (index, item);
        }
        
        public void AddSong (Song song) {
            InsertSong (-1, song);
        }

        public bool RemoveSong (Song song) {
            if (IsOnTheGo)
                throw new InvalidOperationException ("The On-The-Go playlist cannot be modified");
            
            return record.RemoveItem (song.Id);
        }

        public bool MoveSong (int index, Song song) {
            if (!RemoveSong (song))
                return false;

            InsertSong (index, song);
            return true;
        }
    }

}
