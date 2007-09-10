using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace IPod.Unix {

    #if !WINDOWS
    public class Device : GLib.Object, IDevice {

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

        private Dictionary<int, ArtworkFormat> formats = new Dictionary<int, ArtworkFormat>();
		private IPod.Device host;
		
        public IPod.Device Host {
            get { return host; }
            set { host = value; }
        }

        public Dictionary<int,ArtworkFormat> ArtworkFormats {
            get { return formats; } 
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
            get { return (string) GetProperty ("user-name").Val; } 
			set { SetProperty ("user-name", new GLib.Value (value)); }
        }

        public string HostName {
            get { return (string) GetProperty ("host-name").Val; } 
			set { SetProperty ("host-name", new GLib.Value (value)); }
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

        internal Device (IntPtr ptr) : base (ptr) {
            if (Raw == IntPtr.Zero) {
                throw new DeviceException (host, "Failed to create device");
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
 
        public void RescanDisk () {
            if (!ipod_device_rescan_disk (Raw)) {
                throw new DeviceException (host, "Failed to rescan disk");
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
                    throw new DeviceException (host, exc.Message, exc);
                } else {
                    throw new DeviceException (host, "Failed to eject device");
                }
            case EjectResult.Busy:
                throw new DeviceBusyException (host);
            }
        }

        public void Save () {
            IntPtr error = IntPtr.Zero;

            if (!ipod_device_save (Raw, out error)) {
                if (!error.Equals (IntPtr.Zero)) {
                    GLib.GException exc = new GLib.GException (error);
                    throw new DeviceException (host, exc.Message, exc);
                } else {
                    throw new DeviceException (host, "Failed to save device");
                }
            }
        }

        public void Debug () {
            ipod_device_debug (Raw);
        }

        public static explicit operator IPod.Unix.Device (IPod.Device dev)
        {
            Unix.Device uDevice = dev.platformDevice as Unix.Device;

            if (uDevice == null)
                throw new InvalidCastException ("Device is not a Unix.Device");

            return uDevice;
        }
        
        public static IPod.Device[] ListDevices () {
            GLib.List list = new GLib.List (ipod_device_list_devices ());

            ArrayList alist = new ArrayList (list);
            Unix.Device[] uDevices = (Unix.Device[])alist.ToArray(typeof(Unix.Device));

            List<IPod.Device> devices = new List<IPod.Device>();
            foreach (Unix.Device uDev in uDevices)
                devices.Add(new IPod.Device(uDev));

            return devices.ToArray();
        }
    }
#endif
}

