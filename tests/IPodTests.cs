
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

        private Song FindSong (SongDatabase db, int id) {
            foreach (Song song in db.Songs) {
                if (song.Id == id)
                    return song;
            }

            return null;
        }

        // The following tests should all succeed.

        [Test]
        public void SimpleTest () {
            Device device = OpenDevice ();
            device.Save ();
            device.SongDatabase.Save ();
        }

        [Test]
        public void AddSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int len = db.Songs.Length;

            for (int i = 0; i < 100; i++)
                AddSong (db);

            db.Save ();
            db.Reload ();

            Assert.AreEqual (len + 100, db.Songs.Length);
        }

        [Test]
        public void RemoveSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int id = AddSong (db).Id;
            db.Save ();

            db.Reload ();

            Song foundSong = FindSong (db, id);

            Assert.IsNotNull (foundSong);

            db.RemoveSong (foundSong);
            db.Save ();

            db.Reload ();

            Assert.IsNull (FindSong (db, id));
        }

        [Test]
        public void UpdateSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int id = AddSong (db).Id;

            db.Save ();
            db.Reload ();

            Song song = FindSong (db, id);

            Assert.IsNotNull (song);

            DateTime now = DateTime.Now;
            
            song.Artist = "عَلَيْكُم";
            song.Album = "ሠላም";
            song.Year = 1000;
            song.Length = 9999;
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

            db.Save ();
            db.Reload ();

            song = FindSong (db, id);

            Assert.IsNotNull (song);
            Assert.AreEqual ("عَلَيْكُم", song.Artist);
            Assert.AreEqual ("ሠላም", song.Album);
            Assert.AreEqual (1000, song.Year);
            Assert.AreEqual (9999, song.Length);
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

            // the conversion to/from mac time skews this a little
            // so we can't do a straight comparison
            Assert.IsTrue (Math.Abs ((song.LastPlayed - now).TotalSeconds) < 1);
        }

        private Playlist FindPlaylist (SongDatabase db, string name) {
            foreach (Playlist p in db.Playlists) {
                if (p.Name == playlistName)
                    return p;
            }

            return null;
        }

        private Playlist CreatePlaylist (SongDatabase db, int numSongs) {
            Playlist list = db.CreatePlaylist (playlistName);

            for (int i = 0; i < numSongs; i++) {
                list.AddSong (AddSong (db));
            }

            return list;
        }

        [Test]
        public void CreatePlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist list = CreatePlaylist (db, 10);

            db.Save ();
            db.Reload ();

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);
        }

        [Test]
        public void RemovePlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;
            Playlist list = CreatePlaylist (db, 10);

            db.Save ();
            db.Reload ();

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);

            db.RemovePlaylist (list);
            db.Save ();
            db.Reload ();

            list = FindPlaylist (db, playlistName);

            Assert.AreEqual (null, list);
        }

        [Test]
        public void UpdatePlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            db.Reload ();

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);

            for (int i = 0; i < 10; i++)
                list.AddSong (AddSong (db));

            Assert.AreEqual (20, list.Songs.Length);
            db.Save ();
            db.Reload ();

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (20, list.Songs.Length);
        }

        [Test]
        public void ReorderPlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            db.Reload ();


            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);

            Song mysong = list.Songs[4];
            mysong.Artist = "reordered";

            list.MoveSong (0, mysong);
            db.Save ();
            db.Reload ();

            list = FindPlaylist (db, playlistName);
            Assert.AreEqual ("reordered", list.Songs[0].Artist);
        }

        private Song AddSong (SongDatabase db) {
            Song song = db.CreateSong ();

            song.Artist = "FOO FOO FOO " + nextSong;
            song.Album = "BAR BAR BAR " + nextSong;
            song.Title = "BAZ BAZ BAZ " + nextSong;
            song.Length = 99999;
            song.Filename = GetTempSongFile ();

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

        [Test]
        public void EqualizerOverflowTest () {
            Device device = OpenDevice ();

            Equalizer eq = AddEq (device);
            
            // the iTunesDB file only allows 510 chars here, so intentionally
            // overflow it to make sure it gets truncated properly.
            eq.Name = "my eq".PadRight (1000);

            device.Save ();
            device = GetDevice ();

            eq = FindEq (device, "my eq".PadRight (510));

            Assert.IsNotNull (eq);
        }

        // The following tests should all "fail".

        [Test]
        [ExpectedException (typeof (ArgumentException))]
        public void NullSongFilenameTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Song song = AddSong (db);
            song.Filename = null;

            db.Save ();
        }

        [Test]
        [ExpectedException (typeof (System.IO.FileNotFoundException))]
        public void MissingSongFilenameTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Song song = AddSong (db);
            song.Filename = "/tmp/ipod-sharp-test-missing.mp3";

            db.Save ();
        }

        [Test]
        [ExpectedException (typeof (DeviceException))]
        public void NoDatabaseFoundTest () {
            Device device = new Device ("/tmp/no-database-here-move-along");
        }

        private Playlist GetOTGPlaylist (SongDatabase db) {
            foreach (Playlist p in db.Playlists) {
                if (p.IsOnTheGo)
                    return p;
            }

            return null;
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void ModifyOTGNameTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            GetOTGPlaylist (db).Name = "foobar";
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void AddOTGSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Song song = AddSong (db);
            GetOTGPlaylist (db).AddSong (song);
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void RemoveOTGSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist otg = GetOTGPlaylist (db);

            // make nunit happy if there were no songs
            if (otg.Songs.Length == 0)
                throw new InvalidOperationException ("no songs");
            
            foreach (Song song in otg.Songs) {
                otg.RemoveSong (song);
            }
        }

        [Test]
        [ExpectedException (typeof (InvalidOperationException))]
        public void RemoveOTGPlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist otg = GetOTGPlaylist (db);
            db.RemovePlaylist (otg);
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
