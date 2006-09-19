
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace IPod {

    internal enum EjectResult : uint {
        Ok,
        Error,
        Busy
    };

    public enum ArtworkUsage : int {
        Unknown = -1,
        Photo,
        Cover
    }

    public enum PixelFormat : int {
        Unknown = -1,
        Rgb565,
        Rgb565BE,
        IYUV
    }

    public class ArtworkFormat {

        private ArtworkUsage usage;
        private short width;
        private short height;
        private short correlationId;
        private int size;
        private PixelFormat pformat;
        private short rotation;

        public ArtworkUsage Usage {
            get { return usage; }
        }

        public short Width {
            get { return width; }
        }

        public short Height {
            get { return height; }
        }

        public int Size {
            get { return size; }
        }

        public PixelFormat PixelFormat {
            get { return pformat; }
        }

        public short Rotation {
            get { return rotation; }
        }

        internal short CorrelationId {
            get { return correlationId; }
        }

        internal ArtworkFormat (ArtworkUsage usage, short width, short height, short correlationId,
                                int size, PixelFormat pformat, short rotation) {
            this.usage = usage;
            this.width = width;
            this.height = height;
            this.correlationId = correlationId;
            this.size = size;
            this.pformat = pformat;
            this.rotation = rotation;
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
        private static unsafe extern bool ipod_device_save (IntPtr raw, out IntPtr error);

        [DllImport ("ipoddevice")]
        private static extern void ipod_device_debug (IntPtr raw);

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_list_devices ();

        [DllImport ("ipoddevice")]
        private static extern IntPtr ipod_device_get_type ();
        
        private ArrayList equalizers;
        private EqualizerContainerRecord eqsrec;
        private TrackDatabase tracks;
        private PhotoDatabase photos;
        private Dictionary<int, ArtworkFormat> formats = new Dictionary<int, ArtworkFormat> ();

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
                return TrackDatabase.Name;
            } set {
                TrackDatabase.Name = value;
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

        public string ManufacturerId {
            get {
                return (string) GetProperty ("manufacturer-id").Val;
            }
        }

        public uint ProductionYear {
            get {
                return (uint) GetProperty ("production-year").Val;
            }
        }

        public uint ProductionWeek {
            get {
                return (uint) GetProperty ("production-week").Val;
            }
        }

        public uint ProductionIndex {
            get {
                return (uint) GetProperty ("production-index").Val;
            }
        }

        public string UnknownIpodUrl {
            get {
                string serial = SerialNumber;
                if(serial == null || serial.Length != 11) {
                    return null;
                }

                return String.Format("http://banshee-project.org/IpodDataSubmit?serial={0}------{1}", serial.Substring(0, 2), serial.Substring(8));
            }
        }

        public ReadOnlyCollection<ArtworkFormat> ArtworkFormats {
            get {
                return new ReadOnlyCollection<ArtworkFormat> (new List<ArtworkFormat> (formats.Values));
            }
        }

        public Equalizer[] Equalizers {
            get {
                if (equalizers == null)
                    LoadEqualizers ();
                
                return (Equalizer[]) equalizers.ToArray (typeof (Equalizer));
            }
        }

        public TrackDatabase TrackDatabase {
            get {
                if (tracks == null) {
                    LoadTrackDatabase ();
                }

                return tracks;
            }
        }

        public PhotoDatabase PhotoDatabase {
            get {
                if (photos == null) {
                    LoadPhotoDatabase ();
                }

                return photos;
            }
        }

        internal bool IsBE {
            get { return ControlPath.EndsWith ("iTunes_Control"); }
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

            // load the artwork formats
            IntPtr array = (IntPtr) GetProperty ("artwork-formats").Val;

            if (array == IntPtr.Zero) {
                return;
            }
                
            int offset = 0;
            
            while (true) {
                int usage = Marshal.ReadInt32 (array, offset);
                offset += 4;
                
                if (usage == -1)
                    break;
                
                short width = Marshal.ReadInt16 (array, offset);
                offset += 2;
                
                short height = Marshal.ReadInt16 (array, offset);
                offset += 2;
                
                short correlationId = Marshal.ReadInt16 (array, offset);
                offset += 4;

                int size = Marshal.ReadInt32 (array, offset);
                offset += 4;

                int pformat = Marshal.ReadInt32 (array, offset);
                offset += 4;

                short rotation = Marshal.ReadInt16 (array, offset);
                offset += 4;
                
                formats[correlationId] = new ArtworkFormat ((ArtworkUsage) usage, width, height, correlationId,
                                                            size, (PixelFormat) pformat, rotation);
            }
        }

        public Device (string mountOrDevice) : this (ipod_device_new (mountOrDevice)) {
        }

        public ReadOnlyCollection<ArtworkFormat> LookupArtworkFormats (ArtworkUsage usage) {
            List<ArtworkFormat> list = new List<ArtworkFormat> ();
            foreach (ArtworkFormat format in formats.Values) {
                if (format.Usage == usage) {
                    list.Add (format);
                }
            }

            return new ReadOnlyCollection<ArtworkFormat> (list);
        }

        internal ArtworkFormat LookupArtworkFormat (int correlationId) {
            return formats[correlationId];
        }

        public void LoadPhotoDatabase () {
            LoadPhotoDatabase (false);
        }

        public void LoadPhotoDatabase (bool createFresh) {
            //FIXME: refuse if the device lacks photo capability

            if (photos == null)
                photos = new PhotoDatabase (this, true, createFresh);
        }
        
        public void LoadTrackDatabase () {
            LoadTrackDatabase (false);
        }
        
        public void LoadTrackDatabase (bool createFresh) {
            if (!IsIPod) {
                throw new DeviceException (this, "Cannot get song database, as this device is not an iPod");
            }

            if (tracks == null)
                tracks = new TrackDatabase (this, createFresh);
        }
        
        public void CreateEmptyTrackDatabase () {
            tracks = null;
            LoadTrackDatabase (true);
        }

        private void EmitChanged () {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        private void LoadEqualizers () {
            equalizers = new ArrayList ();

            eqsrec = new EqualizerContainerRecord ();

            if (!File.Exists (EqDbPath))
                return;
            
            using (BinaryReader reader = new BinaryReader (File.Open (EqDbPath, FileMode.Open))) {
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
                if (File.Exists (EqDbPath))
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
