
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

    public enum ArtworkType : int {
        CoverSmall,
        CoverLarge,
        PhotoSmall,
        PhotoLarge,
        PhotoFullScreen,
        PhotoTvScreen
    }

    public class ArtworkFormat {

        private ArtworkType type;
        private short width;
        private short height;
        private short correlationId;

        public ArtworkType Type {
            get { return type; }
        }

        public short Width {
            get { return width; }
        }

        public short Height {
            get { return height; }
        }

        public short CorrelationId {
            get { return correlationId; }
        }
        
        internal ArtworkFormat (ArtworkType type, short width, short height, short correlationId) {
            this.type = type;
            this.width = width;
            this.height = height;
            this.correlationId = correlationId;
        }
    }

    public class Device : GLib.Object {

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_new (string mountOrDevice);

        [DllImport ("ipoddevice")]
        private static extern bool ipod_device_rescan_disk (IntPtr raw);

        [DllImport ("ipoddevice")]
        private static unsafe extern uint ipod_device_eject (IntPtr raw, out IntPtr error);

        [DllImport ("ipoddevice")]
        private static unsafe extern uint ipod_device_reboot (IntPtr raw, out IntPtr error);

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
        private string controlPath;

        public event EventHandler Changed;

        private string EqDbPath {
            get { return ControlPath + "/iTunes/iTunesEQPresets"; }
        }
        
        public DeviceModel Model {
            get {
                uint rawtype = (uint) GetProperty ("device-model").Val;
                return (DeviceModel) rawtype;
            }
        }

        public string ModelString {
            get {
                return (string)GetProperty ("device-model-string").Val;
            }
        }

        public DeviceGeneration Generation {
            get {
                uint rawtype = (uint) GetProperty ("device-generation").Val;
                return (DeviceGeneration) rawtype;
            }
        }

        public string ControlPath {
            get {
                return (string) GetProperty ("control-path").Val;
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
                return (string) GetProperty ("advertised-capacity").Val;
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

        public string VolumeLabel {
            get {
                return (string) GetProperty ("volume-label").Val;
            }
        }

        public ArtworkFormat[] ArtworkFormats {
            get {
                IntPtr array = (IntPtr) GetProperty ("artwork-formats").Val;

                if (array == IntPtr.Zero) {
                    return new ArtworkFormat[0];
                }
                
                ArrayList list = new ArrayList ();
                int offset = 0;

                while (true) {
                    int type = Marshal.ReadInt32 (array, offset);
                    offset += 4;

                    if (type == -1)
                        break;

                    short width = Marshal.ReadInt16 (array, offset);
                    offset += 2;

                    short height = Marshal.ReadInt16 (array, offset);
                    offset += 2;

                    short correlationId = Marshal.ReadInt16 (array, offset);
                    offset += 2;

                    offset += 2; // two bytes of padding after the struct; if you read it, it's always 0xffff

                    list.Add (new ArtworkFormat ((ArtworkType) type, width, height, correlationId));
                }

                return (ArtworkFormat[]) list.ToArray (typeof (ArtworkFormat));
            }
        }

        public Equalizer[] Equalizers {
            get {
                if (equalizers == null)
                    LoadEqualizers ();
                
                return (Equalizer[]) equalizers.ToArray (typeof (Equalizer));
            }
        }

        public SongDatabase SongDatabase {
            get {
                if(songs == null) {
                    LoadSongDatabase ();
                }

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
        
        public void LoadSongDatabase () {
            LoadSongDatabase (false);
        }
        
        public void LoadSongDatabase (bool createFresh) {
            if (!IsIPod) {
                throw new DeviceException (this, "Cannot get song database, as this device is not an iPod");
            }

            if (songs == null)
                songs = new SongDatabase (this, createFresh);
        }
        
        public void CreateEmptySongDatabase () {
            songs = null;
            LoadSongDatabase(true);
        }

        private void EmitChanged () {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
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
            
            EmitChanged();
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
            if (equalizers == null)
                LoadEqualizers ();
                
            EqualizerRecord rec = new EqualizerRecord ();
            Equalizer eq = new Equalizer (rec);
            
            eqsrec.Add (rec);
            equalizers.Add (eq);
            
            return eq;
        }

        public void RemoveEqualizer (Equalizer eq) {
            equalizers.Remove (eq);
            eqsrec.Remove (eq.EqualizerRecord);
        }
        
        public static Device[] ListDevices () {
            GLib.List list = new GLib.List (ipod_device_list_devices ());

            ArrayList alist = new ArrayList (list);
            return (Device[]) alist.ToArray (typeof (Device));
        }
    }
}
