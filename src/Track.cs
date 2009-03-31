
using System;
using System.Text;
using System.IO;

namespace IPod {

    public enum TrackRating {
        Zero = 0,
        One = 20,
        Two = 40,
        Three = 60,
        Four = 80,
        Five = 100
    }

    public enum MediaType {
        AudioVideo = 0,
        Audio,
        Video,
        Podcast,
        VideoPodcast,
        Audiobook,
        MusicVideo,
        TVShow,
        Movie
    }
    
    public class Track {

        private static char[] CharsToQuote = { ';', '?', '@', '&', '=', '$', ',', '#', '%' };

        private TrackRecord record;
        private TrackDatabase db;
        private int latestPlayCount;
        private int? latestRating = null;
        private Uri uri;
        private Photo coverPhoto;

        internal TrackRecord Record {
            get { return record; }
        }

        public long Id {
            get { return record.DatabaseId; }
        }

        public MediaType Type {
            get { return record.MediaType; }
            set { record.MediaType = value; }
        }

        public string FileName {
            get {
                if (this.Uri == null)
                    return null;
                else 
                    return this.Uri.LocalPath;
            } set {
                this.Uri = PathToFileUri (value);
            }
        }

        public Uri Uri {
            get {
                if (uri == null) {
                    DetailRecord detail = record.GetDetail (DetailType.Location);
                    uri = PathToFileUri (db.GetFilesystemPath (detail.Value));
                }

                return uri;
            } set {
                if (value == null)
                    throw new ArgumentNullException ("Uri cannot be null");

                if (value.Equals (uri)) {
                    return;
                }

                if (value.Scheme != Uri.UriSchemeFile)
                    throw new ArgumentException ("only file scheme is allowed");

                DetailRecord detail = record.GetDetail (DetailType.Location);
                
                if (db.IsTrackOnDevice (value.LocalPath))
                    detail.Value = db.GetPodPath (value.LocalPath);
                else
                    detail.Value = db.GetUniquePodPath (value.LocalPath);

                FileInfo info = new FileInfo (value.LocalPath);
                record.Size = (int) info.Length;

                uri = value;

                if (uri.LocalPath.ToLower ().EndsWith ("mp3"))
                    record.Type = TrackRecordType.MP3;
                else
                    record.Type = TrackRecordType.AAC;
            }
        }

        public int Size {
            get { return record.Size; }
            set { record.Size = value; }
        }

        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds (record.Length); }
            set { record.Length = (int) value.TotalMilliseconds; }
        }

        public int TrackNumber {
            get { return record.TrackNumber; }
            set { record.TrackNumber = value; }
        }

        public int TotalTracks {
            get { return record.TotalTracks; }
            set { record.TotalTracks = value; }
        }

        public int DiscNumber {
            get { return record.DiscNumber; }
            set { record.DiscNumber = value; }
        }

        public int TotalDiscs {
            get { return record.TotalDiscs; }
            set { record.TotalDiscs = value; }
        }

        public short BPM {
            get { return record.BPM; }
            set { record.BPM = value; }
        }
        
        public int Year {
            get { return record.Year; }
            set { record.Year = value; }
        }
        
        public int BitRate {
            get { return record.BitRate; }
            set { record.BitRate = value; }
        }
        
        public ushort SampleRate {
            get { return record.SampleRate; }
            set { record.SampleRate = value; }
        }

        public string Title {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Title);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.Title, value);
            }
        }
        
        public string Artist {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Artist);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.Artist, value);
            }
        }
        
        public string Album {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Album);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.Album, value);
            }
        }

        public string AlbumArtist {
            get {
                DetailRecord detail = record.GetDetail(DetailType.AlbumArtist);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.AlbumArtist, value);
            }
        }
        
        public string Genre {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Genre);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.Genre, value);
            }
        }
        
        public string Comment {
            get {
                DetailRecord detail = record.GetDetail (DetailType.Comment);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.Comment, value);
            }
        }

        public string Composer {
            get {
                DetailRecord detail = record.GetDetail(DetailType.Composer);
                return detail.Value;
            } set {
                SetDetailValue (DetailType.Composer, value);
            }
        }

        public int PlayCount {
            get { return record.PlayCount; }
            set { record.PlayCount = value; }
        }

        public int LatestPlayCount {
            get { return latestPlayCount; }
            internal set { latestPlayCount = value; }
        }

        /// <summary>
        /// The rating set since the last sync (if any)
        /// 
        /// This is the rating referenced from the PlayCounts file on the iPod
        /// </summary>
        public TrackRating? LatestRating {
            get { return (TrackRating?)latestRating; }
            internal set { latestRating = (int?)value; }
        }

        public bool IsCompilation {
            get { return record.CompilationFlag == (byte) 1; }
            set {
                record.CompilationFlag = value ? (byte) 1 : (byte) 0;
            }
        }

        public DateTime LastPlayed {
            get {
                if (record.LastPlayedTime > 0) {
                    return Utility.MacTimeToDate (record.LastPlayedTime);
                } else {
                    return DateTime.MinValue;
                }
            } set {
                record.LastPlayedTime = Utility.DateToMacTime (value);
            }
        }

        public DateTime DateAdded {
            get { return Utility.MacTimeToDate (record.Date); }
            set { record.Date = Utility.DateToMacTime(value); }
        }

        public DateTime LastModifiedTime {
            get { return Utility.MacTimeToDate (record.LastModifiedTime); }
            set { record.LastModifiedTime = Utility.DateToMacTime(value); }
        }

        /// <summary>
        /// The latest rating on the iPod (if LatestRating !=null, Rating == LatestRating)
        /// </summary>
        public TrackRating Rating {
            get { return (TrackRating) record.Rating; }
            set { record.Rating = (byte) value; }
        }

        public string PodcastUrl {
            get { return record.GetDetail (DetailType.PodcastUrl).Value; }
            set { SetDetailValue (DetailType.PodcastUrl, value); }
        }

        public string Category {
            get { return record.GetDetail (DetailType.Category).Value; }
            set { SetDetailValue (DetailType.Category, value); }
        }

        public string Grouping {
            get { return record.GetDetail (DetailType.Grouping).Value; }
            set { SetDetailValue (DetailType.Grouping, value); }
        }

        public bool IsProtected {
            get { return record.UserId != 0; }
        }

        public TrackDatabase TrackDatabase {
            get { return db; }
        }

        internal Track (TrackDatabase db, TrackRecord record) {
            this.db = db;
            this.record = record;
        }
        
        public override string ToString () {
            return String.Format ("({0}) {1} - {2} - {3} ({4})", Id, Artist, Album, Title, FileName);
        }

        public override bool Equals (object a) {
            Track song = a as Track;

            if (song == null)
                return false;

            return this.Id == song.Id;
        }

        public override int GetHashCode () {
            return Id.GetHashCode ();
        }

        private void FindCoverPhoto () {
            if (coverPhoto == null && db.ArtworkDatabase != null) {
                coverPhoto = db.ArtworkDatabase.LookupPhotoByTrackId (record.DatabaseId);
            }
        }

        public bool HasCoverArt (ArtworkFormat format) {
            return GetThumbnail (format, false) != null;
        }

        public byte[] GetCoverArt (ArtworkFormat format) {
            Thumbnail thumbnail = GetThumbnail (format, false);
            if (thumbnail == null)
                return null;
            else
                return thumbnail.GetData ();
        }

        private Thumbnail GetThumbnail (ArtworkFormat format, bool createNew) {
            FindCoverPhoto ();
            
            if (coverPhoto == null) {
                if (!createNew)
                    return null;
                
                
                if (db.ArtworkDatabase != null) {
                    coverPhoto = db.ArtworkDatabase.CreatePhoto ();
                    coverPhoto.Record.TrackId = record.DatabaseId;
                    record.RightSideArtworkId = coverPhoto.Id;
                }
            }

            if (coverPhoto == null) {
                return null;
            }

            Thumbnail thumbnail = coverPhoto.LookupThumbnail (format);
            if (thumbnail == null && createNew) {
                thumbnail = coverPhoto.CreateThumbnail ();
                thumbnail.Format = format;
                thumbnail.Width = format.Width;
                thumbnail.Height = format.Height;
            }

            return thumbnail;
        }

        public void SetCoverArt (ArtworkFormat format, byte[] data) {
            Thumbnail thumbnail = GetThumbnail (format, true);
            thumbnail.SetData (data);

            record.HasArtwork = true;
            record.ArtworkCount = 1; // this is actually for artwork in mp3 tags, but it seems to be needed anyway
        }

        public void RemoveCoverArt () {
            FindCoverPhoto ();

            if (coverPhoto != null && db.ArtworkDatabase != null) {
                db.ArtworkDatabase.RemovePhoto (coverPhoto);
            }
        }
        
        public void RemoveCoverArt (ArtworkFormat format) {
            Thumbnail thumbnail = GetThumbnail (format, false);
            if (thumbnail != null) {
                coverPhoto.RemoveThumbnail (thumbnail);
            }
        }

        private static Uri PathToFileUri (string path) {
            if (path == null)
                return null;

            path = Path.GetFullPath (path).Replace('\\','/');

            StringBuilder builder = new StringBuilder ();
            builder.Append (Uri.UriSchemeFile);
            builder.Append (Uri.SchemeDelimiter);

            int i;
            while ((i = path.IndexOfAny (CharsToQuote)) != -1) {
                if(i > 0) {
                    builder.Append (path.Substring(0, i));
                }

                builder.Append (Uri.HexEscape (path[i]));
                path = path.Substring (i + 1);
            }

            builder.Append (path);

            return new Uri (builder.ToString (), true);
        }
        
        private void SetDetailValue (IPod.DetailType type, string val)
        {
            DetailRecord detail = record.GetDetail (type);
            if (String.IsNullOrEmpty (val))
                record.RemoveDetail (detail);
            else
                detail.Value = val;
        }
    }
}
