
using System;
using System.Collections;

namespace IPod {

    public class Playlist {

        private SongDatabase db;
        private PlaylistRecord record;
        private ArrayList otgsongs;

        internal PlaylistRecord PlaylistRecord {
            get { return record; }
        }

        internal Playlist (SongDatabase db, Song[] otgsongs) {
            this.otgsongs = new ArrayList (otgsongs);
        }
        
        internal Playlist (SongDatabase db, PlaylistRecord record) {
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

        internal bool RemoveOTGSong (Song song) {
            if (!otgsongs.Contains (song))
                return false;

            otgsongs.Remove (song);
            return true;
        }

        public bool MoveSong (int index, Song song) {
            if (!RemoveSong (song))
                return false;

            InsertSong (index, song);
            return true;
        }
    }

}
