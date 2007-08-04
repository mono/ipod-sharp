using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IPod.Win32
{
    public class DeviceEventListener
    {
        public event EventHandler<DeviceEventArgs> DeviceAdded;
        public event EventHandler<DeviceEventArgs> DeviceRemoved;

        public DeviceEventListener()
        {
            DeviceEventListener.device_added += new EventHandler<DeviceEventArgs>(DeviceEventListener_device_added);
            DeviceEventListener.device_removed += new EventHandler<DeviceEventArgs>(DeviceEventListener_device_removed);
        }

        private void DeviceEventListener_device_added(object sender, DeviceEventArgs e)
        {
            if (DeviceAdded != null)
                DeviceAdded(this, e);
        }
        private void DeviceEventListener_device_removed(object sender, DeviceEventArgs e)
        {
            if (DeviceRemoved != null)
                DeviceRemoved(this, e);
        }

        #region Static

        private static event EventHandler<DeviceEventArgs> device_added;
        private static event EventHandler<DeviceEventArgs> device_removed;

        private static DeviceWatcherWindow watcher_window;

        private static SortedList<char, IPod.Device> devices = new SortedList<char, IPod.Device>();
        internal static IList<IPod.Device> Devices { get { return devices.Values; } }

        static DeviceEventListener ()
        {
            watcher_window = new DeviceWatcherWindow ("ipod-sharp DeviceEventListener Message Window");

            watcher_window.DeviceArrived += new EventHandler<DeviceWindowEventArgs> (watcherWindow_DeviceArrived);
            watcher_window.DeviceRemoved += new EventHandler<DeviceWindowEventArgs>(watcherWindow_DeviceRemoved);

            foreach (string drive in Environment.GetLogicalDrives ()) {
                if (IsIpodDrive (drive [0]))
                    devices.Add (drive [0], new IPod.Device(drive));
            }
        }

        private static void watcherWindow_DeviceArrived(object sender, DeviceWindowEventArgs e)
        {
            foreach (char dr in e.Drives) {
                if (IsIpodDrive(dr))
                {
                    IPod.Device device = new IPod.Device(dr + ":\\");
                    devices.Add (dr, device);

                    if (device_added != null)
                        device_added (null, new DeviceEventArgs(device));
                }
            }
        }

        private static void watcherWindow_DeviceRemoved(object sender, DeviceWindowEventArgs e)
        {
            foreach (char dr in e.Drives)
                if (devices.ContainsKey (dr)) {
                    
                    IPod.Device device = devices[dr];

                    devices.Remove (dr);

                    if (device_removed != null)
                        device_removed(null, new DeviceEventArgs(device));
                }
        }

        private static bool IsIpodDrive(char driveLetter)
        {
            DirectoryInfo dir = new DirectoryInfo (driveLetter + ":\\iPod_Control");
            if (dir.Exists)
                return true;

            dir = new DirectoryInfo (driveLetter + ":\\iTunes_Control");
            if (dir.Exists)
                return true;

            return false;
        }

        #endregion
    }

    public class DeviceEventArgs : EventArgs
    {
        private IPod.Device _device;
        public IPod.Device Device { get { return _device; } }

        internal DeviceEventArgs(IPod.Device Device)
        {
            _device = Device;
        }
    }
}