
using System;
using System.IO;

namespace IPod.Tools {

    public class PodTool {

        private static void Usage () {
            Console.WriteLine ("ipod-tool [--dump-songs, --dump-playlists, --clear] <mount_point> ");
        }

        private static string GetSongPath (string dest, Song song) {
            string artist = song.Artist;

            if (artist == null || artist == String.Empty)
                artist = "Unknown";

            string album = song.Album;
            
            if (album == null || album == String.Empty)
                album = "Unknown";

            string title = song.Title;
            if (title == null || title == String.Empty)
                title = "Unknown";

            string ext = Path.GetExtension (song.FileName);
            
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
            SongDatabase db = device.SongDatabase;

            switch (args[0]) {
            case "--dump-files":
                if (args.Length < 3) {
                    Console.WriteLine ("Destination directory required.");
                    Environment.Exit (1);
                }
                
                string dest = args[2];
                int count = 1;
                int total = db.Songs.Length;
                
                foreach (Song song in db.Songs) {
                    string path = GetSongPath (dest, song);
                    Console.WriteLine ("Copying ({0} of {1}): {2}", count++, total, song);

                    string dir = Path.GetDirectoryName (path);
                    if (!Directory.Exists (dir))
                        Directory.CreateDirectory (dir);
                    
                    File.Copy (song.FileName, path);
                }
                break;
            case "--dump-songs":
                foreach (Song song in db.Songs) {
                    Console.WriteLine (song);
                }
                break;
            case "--dump-playlists":
                foreach (Playlist playlist in db.Playlists) {
                    Console.WriteLine ("Playlist: " + playlist.Name);
                    foreach (Song song in playlist.Songs) {
                        Console.WriteLine ("\t" + song);
                    }
                }
                break;
            case "--add-song":
                {
                    Song song = db.CreateSong ();
                    song.Artist = "WOO WOO";
                    song.Album = "WOO WOO";
                    song.Title = "WOO WOO";
                    song.Duration = new TimeSpan(333 * TimeSpan.TicksPerMillisecond);
                    song.FileName = "/tmp/foobar.mp3";
                }
                break;
            case "--remove-song":
                int id = Int32.Parse (args[2]);
                foreach (Song song in db.Songs) {
                    if (song.Id == id)
                        db.RemoveSong (song);
                }
                break;
            case "--clear":
                foreach (Song song in db.Songs) {
                    db.RemoveSong (song);
                }
                break;
            case "--playcounts":
                foreach (Song song in db.Songs) {
                    Console.WriteLine ("{0}: total {1}, latest {2}", song.Title,
                                       song.PlayCount, song.LatestPlayCount);
                }
                break;
            default:
                Usage ();
                break;
            }

            db.Save ();
        }
    }
}
