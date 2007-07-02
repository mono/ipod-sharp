using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IPod.Win32
{
    internal class DeviceEventListener
    {
        public event EventHandler DeviceAdded;
        public event EventHandler DeviceRemoved;

        private DeviceWatcherWindow watcher_window;
        private static List<char> ipod_drives;

        public DeviceEventListener ()
        {
            watcher_window = new DeviceWatcherWindow ("ipod-sharp DeviceEventListener Message Window");

            watcher_window.DeviceArrived += new EventHandler<DeviceEventArgs> (watcherWindow_DeviceArrived);
            watcher_window.DeviceRemoved += new EventHandler<DeviceEventArgs> (watcherWindow_DeviceRemoved);

            foreach (string drive in Environment.GetLogicalDrives ()) {
                if (IsIpodDrive (drive [0]))
                    ipod_drives.Add (drive [0]);
            }
        }

        private void watcherWindow_DeviceArrived (object sender, DeviceEventArgs e)
        {
            foreach (char dr in e.Drives) {
                if (IsIpodDrive (dr))
                    ipod_drives.Add (dr);

                if (DeviceAdded != null)
                    DeviceAdded (this, new EventArgs ());
            }
        }

        private void watcherWindow_DeviceRemoved (object sender, DeviceEventArgs e)
        {
            foreach (char dr in e.Drives)
                if (ipod_drives.Contains (dr)) {
                    ipod_drives.Remove (dr);

                    if (DeviceRemoved != null)
                        DeviceRemoved (this, new EventArgs ());
                }
        }

        private bool IsIpodDrive (char driveLetter)
        {
            DirectoryInfo dir = new DirectoryInfo (driveLetter + ":\\iPod_Control");
            if (dir.Exists)
                return true;

            dir = new DirectoryInfo (driveLetter + ":\\iTunes_Control");
            if (dir.Exists)
                return true;

            return false;
        }
    }
}
