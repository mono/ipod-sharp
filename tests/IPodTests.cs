
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace IPod.Tests {

    internal class TestVolumeInfo : IPod.VolumeInfo
    {
        public TestVolumeInfo () : base ()
        {
        }
        public override ulong Size { get { return 1024 * 1024 * 1000; } }
        public override ulong SpaceUsed { get { return 1024 * 1024 * 200; } }
    }

    internal class TestDevice : IPod.Device
    {
        TestVolumeInfo volume_info = new TestVolumeInfo ();
        public TestDevice (string path) : base ()
        {
            ControlPath = Path.Combine (path, "iPod_Control");
        }

        public override void RescanDisk () {}
        public override void Eject () {}

        public override VolumeInfo VolumeInfo { get { return volume_info; } }
        public override ProductionInfo ProductionInfo { get { return null; } }
        public override ModelInfo ModelInfo { get { return null; } }
    }

    [TestFixture]
    public class IPodTests {


        /*private string tarballPath;
        private static int nextTrack;

        private const string testdir = "/tmp/ipod-sharp-tests";
        private const string playlistName = "Foo Playlist";
        
        [SetUp]
        public void SetUp () {
            tarballPath = Environment.GetEnvironmentVariable ("IPOD_SHARP_TEST_TARBALL");

            if (tarballPath == null)
                throw new ApplicationException ("No test tarball specified.  Set IPOD_SHARP_TEST_TARBALL env var.");
            
            DeleteDirectory (testdir);
            Directory.CreateDirectory (testdir);
        }

        [TearDown]
        public void TearDown () {
            DeleteDirectory (testdir);
        }

        private void DeleteDirectory (string path) {
            if (Directory.Exists (path)) {
                foreach (string file in Directory.GetFiles (path))
                    File.Delete (file);

                foreach (string dir in Directory.GetDirectories (path))
                    DeleteDirectory (dir);

                Directory.Delete (path);
            }
        }

        private Device GetDevice () {
            return new TestDevice (String.Format ("{0}/ipod-test-db", testdir));
        }

        private Device OpenDevice () {

            ProcessStartInfo startInfo = new ProcessStartInfo ("/bin/tar",
                                                               String.Format ("xvfz {0}", tarballPath));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            Process proc = Process.Start (startInfo);
            string errors = proc.StandardError.ReadToEnd ();
            proc.StandardOutput.ReadToEnd ();
            proc.WaitForExit ();

            if (proc.ExitCode != 0)
                throw new ApplicationException ("Unable to untar test: " + errors);

            // this is just sad
            proc = Process.Start (String.Format ("mv ipod-test-db {0}/ipod-test-db", testdir));
            proc.WaitForExit ();

            return GetDevice ();
        }

        private string GetTempTrackFile () {
            string path = String.Format ("{0}/ipod-sharp-test-track-{1}.mp3", testdir, nextTrack++);
            using (StreamWriter writer = new StreamWriter (File.Open (path, FileMode.Create))) {
                writer.Write ("This is not a mp3.");
            }

            return path;
        }

        // The following tests should all "succeed".

        [Test]
        public void SimpleTest () {
            Device device = OpenDevice ();
            device.Save ();
            device.TrackDatabase.Save ();
        }

        [Test]
        public void AddTrackTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            int len = db.Tracks.Count;

            for (int i = 0; i < 100; i++)
                AddTrack (db);

            db.Save ();
            db.Reload ();

            Assert.AreEqual (len + 100, db.Tracks.Count);
        }

        [Test]
        public void RemoveTrackTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            int origlen = db.Tracks.Count;
            
            Track track = AddTrack (db);
            int index = origlen;
            
            foreach (Playlist pl in db.Playlists) {
                pl.AddTrack (track);
            }

            db.Save ();
            db.Reload ();

            Track foundTrack = db.Tracks[index];

            Assert.IsNotNull (foundTrack);

            db.RemoveTrack (foundTrack);

            foreach (Playlist pl in db.Playlists) {
                foreach (Track ps in pl.Tracks) {
                    Assert.IsFalse (foundTrack.Id == ps.Id);
                }
            }

            db.Save ();
            db.Reload ();

            foreach (Playlist pl in db.Playlists) {
                foreach (Track ps in pl.Tracks) {
                    Assert.IsFalse (foundTrack.Id == ps.Id);
                }
            }

            Assert.AreEqual (db.Tracks.Count, origlen);
        }

        [Test]
        public void UpdateTrackTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            AddTrack (db);
            int index = db.Tracks.Count - 1;
            
            db.Save ();
            db.Reload ();

            Track track = db.Tracks[index];

            Assert.IsNotNull (track);

            DateTime now = DateTime.Now;
            
            track.Artist = "عَلَيْكُم";
            track.Album = "ሠላም";
            track.Year = 1000;
            track.Duration = TimeSpan.FromMilliseconds (9999);
            track.TrackNumber = 8;
            track.TotalTracks = 9;
            track.BitRate = 444;
            track.SampleRate = 555;
            track.Title = "ආයුබෝවන්";
            track.Genre = "こんにちは";
            track.Comment = "வணக்கம்";
            track.PlayCount = 34;
            track.LastPlayed = now;
            track.Rating = TrackRating.Four;
            track.PodcastUrl = "blah blah";
            track.Category = "my category";

            db.Save ();
            db.Reload ();

            track = db.Tracks[index];

            Assert.IsNotNull (track);
            Assert.AreEqual ("عَلَيْكُم", track.Artist);
            Assert.AreEqual ("ሠላም", track.Album);
            Assert.AreEqual (1000, track.Year);
            Assert.AreEqual (TimeSpan.FromMilliseconds (9999), track.Duration);
            Assert.AreEqual (8, track.TrackNumber);
            Assert.AreEqual (9, track.TotalTracks);
            Assert.AreEqual (444, track.BitRate);
            Assert.AreEqual (555, track.SampleRate);
            Assert.AreEqual ("ආයුබෝවන්", track.Title);
            Assert.AreEqual ("こんにちは", track.Genre);
            Assert.AreEqual ("வணக்கம்", track.Comment);
            Assert.AreEqual (34, track.PlayCount);
            Assert.AreEqual (TrackRating.Four, track.Rating);
            Assert.AreEqual ("blah blah", track.PodcastUrl);
            Assert.AreEqual ("my category", track.Category);

            // the conversion to/from mac time skews this a little
            // so we can't do a straight comparison
            Assert.IsTrue (Math.Abs ((track.LastPlayed - now).TotalSeconds) < 1);
        }

        private Playlist CreatePlaylist (TrackDatabase db, int numTracks) {
            Playlist list = db.CreatePlaylist (playlistName);

            for (int i = 0; i < numTracks; i++) {
                list.AddTrack (AddTrack (db));
            }

            return list;
        }

        [Test]
        public void CreatePlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist list = CreatePlaylist (db, 10);

            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Tracks.Count);
        }

        [Test]
        public void RemovePlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;
            Playlist list = CreatePlaylist (db, 10);

            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);

            db.RemovePlaylist (list);
            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);

            Assert.AreEqual (null, list);
        }

        [Test]
        public void UpdatePlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Tracks.Count);

            for (int i = 0; i < 10; i++)
                list.AddTrack (AddTrack (db));

            Assert.AreEqual (20, list.Tracks.Count);
            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (20, list.Tracks.Count);
        }

        [Test]
        public void ReorderPlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            db.Reload ();


            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Tracks.Count);

            Track mytrack = list.Tracks[4];
            mytrack.Artist = "reordered";

            list.RemoveTrack (4);
            list.InsertTrack (0, mytrack);
            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);
            Assert.AreEqual ("reordered", list.Tracks[0].Artist);
        }

        private Track AddTrack (TrackDatabase db) {
            Track track = db.CreateTrack ();

            track.Artist = "FOO FOO FOO " + nextTrack;
            track.Album = "BAR BAR BAR " + nextTrack;
            track.Title = "BAZ BAZ BAZ " + nextTrack;
            track.Duration = TimeSpan.FromMilliseconds (99999);
            track.FileName = GetTempTrackFile ();

            return track;
        }

        [Test]
        public void SaveDeviceTest () {
            Device device = OpenDevice ();
            device.Name = "fooname";
            device.Save ();

            device = GetDevice ();
            Assert.AreEqual ("fooname", device.Name);
        }

        [Test]
        public void RescanDeviceTest () {
            Device device = OpenDevice ();
            device.RescanDisk ();
        }

        [Test]
        public void DeviceVolumeSizeTest () {
            Device device = OpenDevice ();

            Assert.IsTrue (device.VolumeSize > 0);
        }

        private Equalizer FindEq (Device device, string name) {
            foreach (Equalizer eq in device.Equalizers) {
                if (eq.Name == name)
                    return eq;
            }

            return null;
        }

        private Equalizer AddEq (Device device) {
            Equalizer eq = device.CreateEqualizer ();
            
            eq.Name = "my name";
            eq.PreAmp = 50;

            int[] bands = new int[] { 200, 20, 10, 5, 300 };
            eq.BandValues = bands;

            return eq;
        }

        [Test]
        public void AddEqualizerTest () {
            Device device = OpenDevice ();

            Equalizer eq = AddEq (device);
            string name = eq.Name;
            int preamp = eq.PreAmp;
            int[] bands = eq.BandValues;
            
            device.Save ();
            device = GetDevice ();

            eq = FindEq (device, name);

            Assert.IsNotNull (eq);
            Assert.AreEqual (name, eq.Name);
            Assert.AreEqual (preamp, eq.PreAmp);
            Assert.AreEqual (bands, eq.BandValues);
        }

        [Test]
        public void RemoveEqualizerTest () {
            Device device = OpenDevice ();

            string name = AddEq (device).Name;

            device.Save ();
            device = GetDevice ();

            Equalizer eq = FindEq (device, name);

            Assert.IsNotNull (eq);

            device.RemoveEqualizer (eq);
            device.Save ();

            device = GetDevice ();

            Assert.IsNull (FindEq (device, name));
        }

        // The following tests should all "fail".

        [Test]
        [ExpectedException (typeof (ArgumentNullException))]
        public void NullTrackFileNameTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Track track = AddTrack (db);
            track.FileName = null;

            db.Save ();
        }

        [Test]
        [ExpectedException (typeof (System.IO.FileNotFoundException))]
        public void MissingTrackFileNameTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Track track = AddTrack (db);
            track.FileName = "/tmp/ipod-sharp-test-missing.mp3";

            db.Save ();
        }

        [Test]
        [ExpectedException (typeof (DeviceException))]
        public void NoDatabaseFoundTest () {
            new TestDevice ("/tmp/no-database-here-move-along");
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void ModifyOTGNameTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            IList<Playlist> lists = db.OnTheGoPlaylists;
            
            if (lists.Count == 0)
                throw new InvalidOperationException ("no lists");
            
            lists[0].Name = "foobar";
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void AddOTGTrackTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Track track = AddTrack (db);

            IList<Playlist> lists = db.OnTheGoPlaylists;
            
            if (lists.Count == 0)
                throw new InvalidOperationException ("no lists");

            lists[0].AddTrack (track);
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void RemoveOTGTrackTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            IList<Playlist> lists = db.OnTheGoPlaylists;

            // make nunit happy if there were no tracks or playlists
            if (lists.Count == 0 || lists[0].Tracks.Count == 0)
                throw new InvalidOperationException ("no tracks");

            Playlist otg = lists[0];
            
            foreach (Track track in otg.Tracks) {
                otg.RemoveTrack (track);
            }
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void RemoveOTGPlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            IList<Playlist> lists = db.OnTheGoPlaylists;

            // make nunit stfu
            if (lists.Count == 0)
                throw new InvalidOperationException ("no playlists");

            foreach (Playlist pl in lists) {
                db.RemovePlaylist (pl);
            }
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void EqualizerNotEnoughBandsTest () {
            Device device = OpenDevice ();

            Equalizer eq = AddEq (device);
            eq.BandValues = new int[] { 5, 4, 3 };
            
            device.Save ();
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void EqualizerInvalidBandsTest () {
            Device device = OpenDevice ();

            Equalizer eq = AddEq (device);
            eq.BandValues = new int[] { 5, 4, 5000, 0, 0 };
            
            device.Save ();
        }*/
    }
}
