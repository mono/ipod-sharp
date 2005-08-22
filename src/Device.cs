
using System;
using System.IO;
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
        private static unsafe extern uint ipod_device_eject (IntPtr raw, out IntPtr error);

        [DllImport ("ipoddevice")]
        private static unsafe extern bool ipod_device_save (IntPtr raw, out IntPtr error);

        [DllImport ("ipoddevice")]
        private static extern void ipod_device_debug (IntPtr raw);

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_list_devices ();

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_get_type ();
        
        private ArrayList equalizers;
        private EqualizerContainerRecord eqsrec;
        private SongDatabase songs;

        public event EventHandler Changed;

        private string EqDbPath {
            get { return this.MountPoint + "/iPod_Control/iTunes/iTunesEQPresets"; }
        }
        
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
                EmitChanged ();
            }
        }

        public string HostName {
            get {
                return (string) GetProperty ("host-name").Val;
            } set {
                SetProperty ("host-name", new GLib.Value (value));
                EmitChanged ();
            }
        }

        public string VolumeId {
            get {
                return (string) GetProperty ("hal-volume-id").Val;
            }
        }

        public string AdvertisedCapacity {
            get {
                return Capacities.GetCapacity (ModelNumber);
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

        public bool CanWrite {
            get {
                return (bool) GetProperty ("can-write").Val;
            }
        }

        public string Name {
            get {
                return (string) GetProperty ("device-name").Val;
            } set {
                SetProperty ("device-name", new GLib.Value (value));
                EmitChanged ();
            }
        }

        public bool IsNew {
            get {
                return (bool) GetProperty ("is-new").Val;
            }
        }

        public string SerialNumber {
            get {
                return (string) GetProperty ("serial-number").Val;
            }
        }

        public string ModelNumber {
            get {
                return (string) GetProperty ("model-number").Val;
            }
        }

        public string FirmwareVersion {
            get {
                return (string) GetProperty ("firmware-version").Val;
            }
        }

        public string VolumeUuid {
            get {
                return (string) GetProperty ("volume-uuid").Val;
            }
        }

        public Equalizer[] Equalizers {
            get {
                lock (this) {
                    if (equalizers == null)
                        LoadEqualizers ();

                    return (Equalizer[]) equalizers.ToArray (typeof (Equalizer));
                }
            }
        }

        public SongDatabase SongDatabase {
            get {
                if (!IsIPod) {
                    throw new DeviceException (this, "Cannot get song database, as this device is not an iPod");
                }

                if (songs == null)
                    songs = new SongDatabase (this);

                return songs;
            }
        }

        public static new GLib.GType GType { 
            get {
                IntPtr raw_ret = ipod_device_get_type();
                GLib.GType ret = new GLib.GType(raw_ret);
                return ret;
            }
        }
        
        static Device () {
            Initializer.Init ();
        }

        protected Device (IntPtr ptr) : base (ptr) {
            if (Raw == IntPtr.Zero) {
                throw new DeviceException (this, "Failed to create device");
            }
        }

        public Device (string mountOrDevice) : this (ipod_device_new (mountOrDevice)) {
        }

        private void EmitChanged () {
            if (Changed != null) {
                Changed (this, new EventArgs ());
            }
        }

        private void LoadEqualizers () {
            equalizers = new ArrayList ();
            
            using (BinaryReader reader = new BinaryReader (File.Open (EqDbPath, FileMode.Open))) {
                eqsrec = new EqualizerContainerRecord ();
                eqsrec.Read (reader);

                foreach (EqualizerRecord eqrec in eqsrec.EqualizerRecords) {
                    Equalizer eq = new Equalizer (eqrec);
                    equalizers.Add (eq);
                }
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
                if (error != IntPtr.Zero) {
                    GLib.GException exc = new GLib.GException (error);
                    throw new DeviceException (this, exc.Message, exc);
                } else {
                    throw new DeviceException (this, "Failed to eject device");
                }
            case EjectResult.Busy:
                throw new DeviceBusyException (this);
            }
        }

        public void Save () {
            IntPtr error = IntPtr.Zero;

            if (!ipod_device_save (Raw, out error)) {
                if (!error.Equals (IntPtr.Zero)) {
                    GLib.GException exc = new GLib.GException (error);
                    throw new DeviceException (this, exc.Message, exc);
                } else {
                    throw new DeviceException (this, "Failed to save device");
                }
            }

            // nothing more to do
            if (equalizers == null)
                return;

            try {
                // Back up the eq db
                File.Copy (EqDbPath, EqDbPath + ".bak", true);
                
                // Save the eq db
                using (BinaryWriter writer = new BinaryWriter (new FileStream (EqDbPath, FileMode.Create))) {
                    eqsrec.Save (writer);
                }
            } catch (Exception e) {
                // restore the backup
                File.Copy (EqDbPath + ".bak", EqDbPath, true);

                throw e;
            }
        }

        public void Debug () {
            ipod_device_debug (Raw);
        }

        public Equalizer CreateEqualizer () {
            lock (this) {
                if (equalizers == null)
                    LoadEqualizers ();
                
                EqualizerRecord rec = new EqualizerRecord ();
                Equalizer eq = new Equalizer (rec);

                eqsrec.Add (rec);
                equalizers.Add (eq);

                return eq;
            }
        }

        public void RemoveEqualizer (Equalizer eq) {
            lock (this) {
                equalizers.Remove (eq);
                eqsrec.Remove (eq.EqualizerRecord);
            }
        }
        
        public static Device[] ListDevices () {
            GLib.List list = new GLib.List (ipod_device_list_devices ());

            ArrayList alist = new ArrayList (list);
            return (Device[]) alist.ToArray (typeof (Device));
        }
    }
}
