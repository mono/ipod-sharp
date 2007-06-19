
using System;
using System.IO;

namespace IPod.Tools {

    public class PodTool {

        private static void Usage () {
            Console.WriteLine ("ipod-tool [--dump-tracks, --dump-playlists, --clear] <mount_point> ");
        }

        private static string GetTrackPath (string dest, Track track) {
            string artist = track.Artist;

            if (artist == null || artist == String.Empty)
                artist = "Unknown";

            string album = track.Album;
            
            if (album == null || album == String.Empty)
                album = "Unknown";

            string title = track.Title;
            if (title == null || title == String.Empty)
                title = "Unknown";

            string ext = Path.GetExtension (track.FileName);
            
            int index = 0;
            string path = null;
            
            while (true) {
                if (index > 0)
                    path = String.Format ("{0}/{1}/{2}/{1} - {3} ({4}){5}", dest, artist, album, title, index, ext);
                else
                    path = String.Format ("{0}/{1}/{2}/{1} - {3}{4}", dest, artist, album, title, ext);

                if (!File.Exists (path))
                    break;
                else
                    index++;
            }
                
            return path;
        }
        
        public static void Main (string[] args) {

            if (args.Length < 2) {
                Usage ();
                return;
            }

            Device device = new Device (args[1]);
            TrackDatabase db = device.TrackDatabase;

            switch (args[0]) {
            case "--dump-files":
                if (args.Length < 3) {
                    Console.WriteLine ("Destination directory required.");
                    Environment.Exit (1);
                }
                
                string dest = args[2];
                int count = 1;
                int total = db.Tracks.Count;
                
                foreach (Track track in db.Tracks) {
                    string path = GetTrackPath (dest, track);
                    Console.WriteLine ("Copying ({0} of {1}): {2}", count++, total, track);

                    string dir = Path.GetDirectoryName (path);
                    if (!Directory.Exists (dir))
                        Directory.CreateDirectory (dir);
                    
                    File.Copy (track.FileName, path);
                }
                break;
            case "--dump-tracks":
                foreach (Track track in db.Tracks) {
                    Console.WriteLine (track);
                }
                break;
            case "--dump-playlists":
                foreach (Playlist playlist in db.Playlists) {
                    Console.WriteLine ("Playlist: " + playlist.Name);
                    foreach (Track track in playlist.Tracks) {
                        Console.WriteLine ("\t" + track);
                    }
                }
                break;
            case "--add-track":
                {
                    Track track = db.CreateTrack ();
                    track.Artist = "WOO WOO";
                    track.Album = "WOO WOO";
                    track.Title = "WOO WOO";
                    track.Duration = new TimeSpan(333 * TimeSpan.TicksPerMillisecond);
                    track.FileName = "/tmp/foobar.mp3";
                }
                break;
            case "--remove-track":
                int id = Int32.Parse (args[2]);
                foreach (Track track in db.Tracks) {
                    if (track.Id == id)
                        db.RemoveTrack (track);
                }
                break;
            case "--clear":
                foreach (Track track in db.Tracks) {
                    db.RemoveTrack (track);
                }
                break;
            case "--playcounts":
                foreach (Track track in db.Tracks) {
                    Console.WriteLine ("{0}: total {1}, latest {2}", track.Title,
                                       track.PlayCount, track.LatestPlayCount);
                }
                break;
            case "--dump-sportkits":
                SportKit[] kits = device.SportKitManager.SportKits;
                Console.WriteLine ("{0} sport kits found.", kits.Length);

                for (int i = 0; i < kits.Length; i++) {
                    SportKit kit = kits[i];

                    Console.WriteLine ("Kit {0}:", i);
                    Console.WriteLine ("  ID: {0}", kit.ID);
                    Console.WriteLine ("  Path: {0}", kit.Path);

                    // ugly ugly ugly
                    foreach (object[] data in new object[] { new object[] { "Latest Workouts", kit.LatestWorkouts },
                                                             new object[] { "Synched Workouts", kit.SynchedWorkouts } } ) {
                        Console.WriteLine ("  {0}:", data[0]);

                        foreach (Workout w in ((Workout[])data[1])) {
                            Console.WriteLine ("   - Workout on {0}:", w.Time);
                            Console.WriteLine ("      * FileName: {0}", w.FileName);
                            Console.WriteLine ("      * Format Version: {0}", w.Version);
                            Console.WriteLine ("      * Name: {0}", w.Name);
                            Console.WriteLine ("      * Duration: {0} ({1} min)", w.Duration, w.Duration.TotalMinutes);
                            Console.WriteLine ("      * Distance: {0} {1}", w.Distance, w.DistanceUnit);
                            Console.WriteLine ("      * Pace: {0}", w.Pace);
                            Console.WriteLine ("      * Calories: {0}", w.Calories);
                            Console.WriteLine ("      * Template ID: {0}", w.TemplateID);
                            Console.WriteLine ("      * Template Name: {0}", w.TemplateName);
                            Console.WriteLine ("      * Start Time: {0}", w.StartTime);
                            Console.WriteLine ("      * Interval Value: {0}", w.IntervalValue);
                            
                            Console.WriteLine ("      * Intervals:");
                            Console.Write ("        ");

                            foreach (double interval in w.Intervals) {
                                Console.Write ("{0}  ", interval);
                            }

                            Console.WriteLine ();
                            Console.WriteLine ();
                        }
                    }
                }

                return;
            default:
                Usage ();
                break;
            }

            db.Save ();
        }
    }
}
