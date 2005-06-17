
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

            Assert.AreEqual (true, foundSong != null);

            db.RemoveSong (foundSong);
            db.Save ();

            LoadDatabase (db);

            Assert.AreEqual (true, FindSong (db, id) == null);
        }

        [Test]
        public void UpdateSongTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            int id = AddSong (db).Id;

            db.Save ();
            LoadDatabase (db);

            Song song = FindSong (db, id);

            Assert.AreEqual (true, song != null);
            
            song.Artist = "DEADBEEF";
            song.Album = "DEADBEEF";
            song.Year = 1000;

            db.Save ();
            LoadDatabase (db);

            song = FindSong (db, id);

            Assert.AreEqual (true, song != null);
            Assert.AreEqual ("DEADBEEF", song.Artist);
            Assert.AreEqual ("DEADBEEF", song.Album);
            Assert.AreEqual (1000, song.Year);
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

            Assert.AreEqual (true, list != null);
            Assert.AreEqual (10, list.Songs.Length);
        }

        [Test]
        public void RemovePlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;
            Playlist list = CreatePlaylist (db, 10);

            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.AreEqual (true, list != null);

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

            Assert.AreEqual (true, list != null);
            Assert.AreEqual (10, list.Songs.Length);

            for (int i = 0; i < 10; i++)
                list.AddSong (AddSong (db));

            Assert.AreEqual (20, list.Songs.Length);
            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.AreEqual (true, list != null);
            Assert.AreEqual (20, list.Songs.Length);
        }

        [Test]
        public void ReorderPlaylistTest () {
            SongDatabase db = OpenDevice ().SongDatabase;

            Playlist list = CreatePlaylist (db, 10);
            db.Save ();
            LoadDatabase (db);

            list = FindPlaylist (db, playlistName);

            Assert.AreEqual (true, list != null);
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
            Assert.AreEqual (true, device.IsIPod);
        }

        [Test]
        public void DeviceVolumeTest () {
            Device device = OpenDevice ();

            Assert.AreEqual (true, device.VolumeSize > 0);
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
    }
}
