
using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace IPod.Tests {

    [TestFixture]
    public class IPodTests {

        private string tarballPath;
        private static int nextSong;

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
            return new Device (String.Format ("{0}/ipod-test-db", testdir));
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

            Directory.Move ("ipod-test-db", testdir + "/" + "ipod-test-db");

            return GetDevice ();
        }

        private string GetTempSongFile () {
            string path = String.Format ("{0}/ipod-sharp-test-song-{1}.mp3", testdir, nextSong++);
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
        public void AddSongTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            int len = db.Songs.Length;

            for (int i = 0; i < 100; i++)
                AddSong (db);

            db.Save ();
            db.Reload ();

            Assert.AreEqual (len + 100, db.Songs.Length);
        }

        [Test]
        public void RemoveSongTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            int origlen = db.Songs.Length;
            
            Song song = AddSong (db);
            int index = origlen;
            
            foreach (Playlist pl in db.Playlists) {
                pl.AddSong (song);
            }

            db.Save ();
            db.Reload ();

            Song foundSong = db.Songs[index];

            Assert.IsNotNull (foundSong);

            db.RemoveSong (foundSong);

            foreach (Playlist pl in db.Playlists) {
                foreach (Song ps in pl.Songs) {
                    Assert.IsFalse (foundSong.Id == ps.Id);
                }
            }

            db.Save ();
            db.Reload ();

            foreach (Playlist pl in db.Playlists) {
                foreach (Song ps in pl.Songs) {
                    Assert.IsFalse (foundSong.Id == ps.Id);
                }
            }

            Assert.AreEqual (db.Songs.Length, origlen);
        }

        [Test]
        public void UpdateSongTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            AddSong (db);
            int index = db.Songs.Length - 1;
            
            db.Save ();
            db.Reload ();

            Song song = db.Songs[index];

            Assert.IsNotNull (song);

            DateTime now = DateTime.Now;
            
            song.Artist = "عَلَيْكُم";
            song.Album = "ሠላም";
            song.Year = 1000;
            song.Duration = TimeSpan.FromMilliseconds (9999);
            song.TrackNumber = 8;
            song.TotalTracks = 9;
            song.BitRate = 444;
            song.SampleRate = 555;
            song.Title = "ආයුබෝවන්";
            song.Genre = "こんにちは";
            song.Comment = "வணக்கம்";
            song.PlayCount = 34;
            song.LastPlayed = now;
            song.Rating = SongRating.Four;
            song.PodcastUrl = "blah blah";
            song.Category = "my category";

            db.Save ();
            db.Reload ();

            song = db.Songs[index];

            Assert.IsNotNull (song);
            Assert.AreEqual ("عَلَيْكُم", song.Artist);
            Assert.AreEqual ("ሠላም", song.Album);
            Assert.AreEqual (1000, song.Year);
            Assert.AreEqual (TimeSpan.FromMilliseconds (9999), song.Duration);
            Assert.AreEqual (8, song.TrackNumber);
            Assert.AreEqual (9, song.TotalTracks);
            Assert.AreEqual (444, song.BitRate);
            Assert.AreEqual (555, song.SampleRate);
            Assert.AreEqual ("ආයුබෝවන්", song.Title);
            Assert.AreEqual ("こんにちは", song.Genre);
            Assert.AreEqual ("வணக்கம்", song.Comment);
            Assert.AreEqual (34, song.PlayCount);
            Assert.AreEqual (SongRating.Four, song.Rating);
            Assert.AreEqual ("blah blah", song.PodcastUrl);
            Assert.AreEqual ("my category", song.Category);

            // the conversion to/from mac time skews this a little
            // so we can't do a straight comparison
            Assert.IsTrue (Math.Abs ((song.LastPlayed - now).TotalSeconds) < 1);
        }

        private Playlist CreatePlaylist (TrackDatabase db, int numSongs) {
            Playlist list = db.CreatePlaylist (playlistName);

            for (int i = 0; i < numSongs; i++) {
                list.AddSong (AddSong (db));
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
            Assert.AreEqual (10, list.Songs.Length);
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
            Assert.AreEqual (10, list.Songs.Length);

            for (int i = 0; i < 10; i++)
                list.AddSong (AddSong (db));

            Assert.AreEqual (20, list.Songs.Length);
            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (20, list.Songs.Length);
        }

        [Test]
        public void ReorderPlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            db.Reload ();


            list = db.LookupPlaylist (playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);

            Song mysong = list.Songs[4];
            mysong.Artist = "reordered";

            list.RemoveSong (4);
            list.InsertSong (0, mysong);
            db.Save ();
            db.Reload ();

            list = db.LookupPlaylist (playlistName);
            Assert.AreEqual ("reordered", list.Songs[0].Artist);
        }

        private Song AddSong (TrackDatabase db) {
            Song song = db.CreateSong ();

            song.Artist = "FOO FOO FOO " + nextSong;
            song.Album = "BAR BAR BAR " + nextSong;
            song.Title = "BAZ BAZ BAZ " + nextSong;
            song.Duration = TimeSpan.FromMilliseconds (99999);
            song.FileName = GetTempSongFile ();

            return song;
        }

        [Test]
        public void SaveDeviceTest () {
            Device device = OpenDevice ();
            device.HostName = "foobar";
            device.UserName = "bazbaz";
            device.Name = "fooname";
            device.Save ();

            device = GetDevice ();
            Assert.AreEqual ("foobar", device.HostName);
            Assert.AreEqual ("bazbaz", device.UserName);
            Assert.AreEqual ("fooname", device.Name);
        }

        [Test]
        public void RescanDeviceTest () {
            Device device = OpenDevice ();
            device.RescanDisk ();
        }

        [Test]
        public void IsIPodTest () {
            Device device = OpenDevice ();
            Assert.IsTrue (device.IsIPod);
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
        public void NullSongFileNameTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Song song = AddSong (db);
            song.FileName = null;

            db.Save ();
        }

        [Test]
        [ExpectedException (typeof (System.IO.FileNotFoundException))]
        public void MissingSongFileNameTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Song song = AddSong (db);
            song.FileName = "/tmp/ipod-sharp-test-missing.mp3";

            db.Save ();
        }

        [Test]
        [ExpectedException (typeof (DeviceException))]
        public void NoDatabaseFoundTest () {
            new Device ("/tmp/no-database-here-move-along");
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void ModifyOTGNameTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist[] lists = db.OnTheGoPlaylists;
            
            if (lists.Length == 0)
                throw new InvalidOperationException ("no lists");
            
            lists[0].Name = "foobar";
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void AddOTGSongTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Song song = AddSong (db);

            Playlist[] lists = db.OnTheGoPlaylists;
            
            if (lists.Length == 0)
                throw new InvalidOperationException ("no lists");

            lists[0].AddSong (song);
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void RemoveOTGSongTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist[] lists = db.OnTheGoPlaylists;

            // make nunit happy if there were no songs or playlists
            if (lists.Length == 0 || lists[0].Songs.Length == 0)
                throw new InvalidOperationException ("no songs");

            Playlist otg = lists[0];
            
            foreach (Song song in otg.Songs) {
                otg.RemoveSong (song);
            }
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void RemoveOTGPlaylistTest () {
            TrackDatabase db = OpenDevice ().TrackDatabase;

            Playlist[] lists = db.OnTheGoPlaylists;

            // make nunit stfu
            if (lists.Length == 0)
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
        }
    }
}
