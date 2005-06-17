
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace IPod {

    internal enum EjectResult : uint {
        Ok,
        Error,
        Busy
    };

    public class Device : GLib.Object {

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_new (string mountOrDevice);

        [DllImport ("ipoddevice")]
        private static extern bool ipod_device_rescan_disk (IntPtr raw);

        [DllImport ("ipoddevice")]
        private static extern uint ipod_device_eject (IntPtr raw, out IntPtr error);

        [DllImport ("ipoddevice")]
        private static extern bool ipod_device_save (IntPtr raw, out IntPtr error);

        [DllImport ("ipoddevice")]
        private static extern void ipod_device_debug (IntPtr raw);

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_list_devices ();

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_get_type ();

        public DeviceModel Model {
            get {
                uint rawtype = (uint) GetProperty ("device-model").Val;
                return (DeviceModel) rawtype;
            }
        }

        public string DevicePath {
            get {
                return (string) GetProperty ("device-path").Val;
            }
        }

        public string MountPoint {
            get {
                return (string) GetProperty ("mount-point").Val;
            }
        }

        public string UserName {
            get {
                return (string) GetProperty ("user-name").Val;
            } set {
                SetProperty ("user-name", new GLib.Value (value));
            }
        }

        public string HostName {
            get {
                return (string) GetProperty ("host-name").Val;
            } set {
                SetProperty ("host-name", new GLib.Value (value));
            }
        }

        public UInt64 VolumeSize {
            get {
                return (UInt64) GetProperty ("volume-size").Val;
            }
        }

        public UInt64 VolumeUsed {
            get {
                return (UInt64) GetProperty ("volume-used").Val;
            }
        }

        public UInt64 VolumeAvailable {
            get {
                return (UInt64) GetProperty ("volume-available").Val;
            }
        }

        public bool IsIPod {
            get {
                return (bool) GetProperty ("is-ipod").Val;
            }
        }

        public string Name {
            get {
                return (string) GetProperty ("device-name").Val;
            } set {
                SetProperty ("device-name", new GLib.Value (value));
            }
        }

        public SongDatabase SongDatabase {
            get {
                if (!IsIPod) {
                    throw new DeviceException (this, "Cannot get song database, as this device is not an iPod");
                }

                return new SongDatabase (this);
            }
        }

        static Device () {
            Gtk.Application.Init ();
            GLib.GType.Register (new GLib.GType (ipod_device_get_type ()),
                                 typeof (Device));
        }

        protected Device (IntPtr ptr) : base (ptr) {
        }

        public Device (string mountOrDevice) : this (ipod_device_new (mountOrDevice)) {
            if (Raw == IntPtr.Zero) {
                throw new DeviceException (this, "Failed to create device");
            }
        }

        public void RescanDisk () {
            if (!ipod_device_rescan_disk (Raw)) {
                throw new DeviceException (this, "Failed to rescan disk");
            }
        }

        public void Eject () {
            IntPtr error = IntPtr.Zero;
            EjectResult result = (EjectResult) ipod_device_eject (Raw, out error);

            switch (result) {
            case EjectResult.Ok:
                return;
            case EjectResult.Error:
                GLib.GException exc = new GLib.GException (error);
                throw new DeviceException (this, exc.Message, exc);
            case EjectResult.Busy:
                throw new DeviceBusyException (this);
            }
        }

        public void Save () {
            IntPtr error = IntPtr.Zero;
            
            if (!ipod_device_save (Raw, out error)) {
                GLib.GException exc = new GLib.GException (error);
                throw new DeviceException (this, exc.Message, exc);
            }
        }

        public void Debug () {
            ipod_device_debug (Raw);
        }

        public static Device[] ListDevices () {
            GLib.List list = new GLib.List (ipod_device_list_devices ());

            ArrayList alist = new ArrayList (list);
            return (Device[]) alist.ToArray (typeof (Device));
        }
    }
}
