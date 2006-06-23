
using System;

namespace IPod {

    public class VideoAdder {

        private static string Prompt (string msg) {
            Console.Write (msg);
            return Console.ReadLine ();
        }

        public static void Main (string[] args) {

            if (args.Length == 0) {
                Console.WriteLine ("Usage: video-adder <mp4 file>");
                Console.WriteLine ("Adds a video file to all connected devices");
                return;
            }

            string file = args[0];
            
            foreach (Device device in Device.ListDevices ()) {
                Console.WriteLine ("Adding {0} to '{1}'", file, device.Name);
                TrackDatabase db = device.TrackDatabase;

                Track track = db.CreateTrack ();
                track.Type = MediaType.Video;
                track.Artist = Prompt ("Artist: ");
                track.Title = Prompt ("Title: ");
                track.Duration = TimeSpan.Parse (Prompt ("Duration (e.g. 2:00:00 for 2 hours): "));
                track.FileName = file;

                Console.Write ("Saving...");
                db.Save ();
                Console.WriteLine ("Done.");
            }
        }
    }
}
