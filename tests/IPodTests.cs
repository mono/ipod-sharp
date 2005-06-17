
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

        // The following tests should all succeed.
        
        [SetUp]
        public void SetUp () {
            tarballPath = Environment.GetEnvironmentVariable ("IPOD_SHARP_TEST_TARBALL");

            if (tarballPath == null)
                throw new ApplicationException ("No test tarball specified.  Set IPOD_SHARP_TEST_TARBALL env var.");
            
            if (!Directory.Exists (testdir))
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

        private void LoadDatabase (SongDatabase db) {
            db.Load (GetDevice ());
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

        [Test]
        public void AddSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int len = db.Songs.Length;

            for (int i = 0; i < 100; i++)
                AddSong (db);

            db.Save ();

            LoadDatabase (db);

            Assert.AreEqual (len + 100, db.Songs.Length);
        }

        [Test]
        public void RemoveSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int id = AddSong (db).Id;
            db.Save ();

            LoadDatabase (db);

            Song foundSong = FindSong (db, id);

            Assert.IsNotNull (foundSong);

            db.RemoveSong (foundSong);
            db.Save ();

            LoadDatabase (db);

            Assert.IsNull (FindSong (db, id));
        }

        [Test]
        public void UpdateSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int id = AddSong (db).Id;

            db.Save ();
            LoadDatabase (db);

            Song song = FindSong (db, id);

            Assert.IsNotNull (song);

            DateTime now = DateTime.Now;
            
            song.Artist = "TEST ARTIST";
            song.Album = "TEST ALBUM";
            song.Year = 1000;
            song.Length = 9999;
            song.TrackNumber = 8;
            song.TotalTracks = 9;
            song.BitRate = 444;
            song.SampleRate = 555;
            song.Title = "TEST TITLE";
            song.Genre = "Metal";
            song.Comment = "test comment";
            song.PlayCount = 34;
            song.LastPlayed = now;
            song.Rating = SongRating.Four;

            db.Save ();
            LoadDatabase (db);

            song = FindSong (db, id);

            Assert.IsNotNull (song);
            Assert.AreEqual ("TEST ARTIST", song.Artist);
            Assert.AreEqual ("TEST ALBUM", song.Album);
            Assert.AreEqual (1000, song.Year);
            Assert.AreEqual (9999, song.Length);
            Assert.AreEqual (8, song.TrackNumber);
            Assert.AreEqual (9, song.TotalTracks);
            Assert.AreEqual (444, song.BitRate);
            Assert.AreEqual (555, song.SampleRate);
            Assert.AreEqual ("TEST TITLE", song.Title);
            Assert.AreEqual ("Metal", song.Genre);
            Assert.AreEqual ("test comment", song.Comment);
            Assert.AreEqual (34, song.PlayCount);
            Assert.AreEqual (SongRating.Four, song.Rating);

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
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);
        }

        [Test]
        public void RemovePlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;
            Playlist list = CreatePlaylist (db, 10);

            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);

            db.RemovePlaylist (list);
            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.AreEqual (null, list);
        }

        [Test]
        public void UpdatePlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);

            for (int i = 0; i < 10; i++)
                list.AddSong (AddSong (db));

            Assert.AreEqual (20, list.Songs.Length);
            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (20, list.Songs.Length);
        }

        [Test]
        public void ReorderPlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.IsNotNull (list);
            Assert.AreEqual (10, list.Songs.Length);

            Song mysong = list.Songs[4];
            mysong.Artist = "reordered";

            list.MoveSong (0, mysong);
            db.Save ();
            LoadDatabase (db);

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
        public void DeviceVolumeTest () {
            Device device = OpenDevice ();

            Assert.IsTrue (device.VolumeSize > 0);
        }

        [Test]
        public void SimpleTest () {
            SongDatabase db = OpenDevice ().SongDatabase;
            db.Save ();
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
            SongDatabase db = new SongDatabase ();
            db.Load (new Device ("/tmp/no-database-here-move-along"));
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
    }
}
