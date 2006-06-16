
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IPod {

    public delegate void PlaylistTrackHandler (object o, int index, Track track);
    
    public class Playlist {

        private TrackDatabase db;
        private PlaylistRecord record;
        private List<Track> otgtracks;
        private string otgtitle;

        public event PlaylistTrackHandler TrackAdded;
        public event PlaylistTrackHandler TrackRemoved;

        internal PlaylistRecord PlaylistRecord {
            get { return record; }
        }

        internal Playlist (TrackDatabase db, string title, List<Track> otgtracks) {
            this.otgtitle = title;
            this.otgtracks = otgtracks;
        }
        
        internal Playlist (TrackDatabase db, PlaylistRecord record) {
            this.db = db;
            this.record = record;
        }

        public ReadOnlyCollection<Track> Tracks {
            get {
                if (IsOnTheGo)
                    return new ReadOnlyCollection<Track> (otgtracks);

                List<Track> tracks = new List<Track> ();

                foreach (PlaylistItemRecord item in record.Items) {
                    Track track = db.GetTrackById (item.TrackId);

                    if (track == null) {
                        Console.Error.WriteLine ("Playlist '{0}' contains invalid track id '{0}'",
                                                 Name, item.TrackId);
                        continue;
                    }

                    tracks.Add (track);
                }

                return new ReadOnlyCollection<Track> (tracks);
            }
        }

        public Track this[int index] {
            get {
                return db.GetTrackById (record.Items[index].TrackId);
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
            get { return otgtracks != null; }
        }

        public void InsertTrack (int index, Track track) {
            if (IsOnTheGo)
                throw new InvalidOperationException ("The On-The-Go playlist cannot be modified");
            
            PlaylistItemRecord item = record.CreateItem ();
            item.TrackId = track.Id;

            record.InsertItem (index, item);

            if (TrackAdded != null)
                TrackAdded (this, index, track);
        }

        public void Clear () {
            record.Clear ();
        }
        
        public void AddTrack (Track track) {
            InsertTrack (-1, track);
        }

        public void RemoveTrack (int index) {
            if (IsOnTheGo)
                throw new InvalidOperationException ("The On-The-Go playlist cannot be modified");

            Track track = this[index];
            record.RemoveItem (index);

            if (TrackRemoved != null)
                TrackRemoved (this, index, track);
        }

        public bool RemoveTrack (Track track) {
            int index;
            bool ret = false;
            
            while ((index = IndexOf (track)) >= 0) {
                ret = true;
                RemoveTrack (index);
            }

            return ret;
        }

        internal bool RemoveOTGTrack (Track track) {
            if (!otgtracks.Contains (track))
                return false;

            otgtracks.Remove (track);
            return true;
        }

        public int IndexOf (Track track) {
            return record.IndexOf (track.Id);
        }
    }

}
