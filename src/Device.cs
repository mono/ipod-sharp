using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;

namespace IPod
{
    internal enum OS
    {
        Win32,
        Unix
    }

    internal enum EjectResult : uint
    {
        Ok,
        Error,
        Busy
    };

    public enum ArtworkUsage : int
    {
        Unknown = -1,
        Photo,
        Cover
    }

    public enum PixelFormat : int
    {
        Unknown = -1,
        Rgb565,
        Rgb565BE,
        IYUV
    }

    public class ArtworkFormat
    {

        private ArtworkUsage usage;
        private short width;
        private short height;
        private short correlationId;
        private int size;
        private PixelFormat pformat;
        private short rotation;

        public ArtworkUsage Usage
        {
            get { return usage; }
        }

        public short Width
        {
            get { return width; }
        }

        public short Height
        {
            get { return height; }
        }

        public int Size
        {
            get { return size; }
        }

        public PixelFormat PixelFormat
        {
            get { return pformat; }
        }

        public short Rotation
        {
            get { return rotation; }
        }

        internal short CorrelationId
        {
            get { return correlationId; }
        }

        internal ArtworkFormat (ArtworkUsage usage, short width, short height, short correlationId,
                                int size, PixelFormat pformat, short rotation)
        {
            this.usage = usage;
            this.width = width;
            this.height = height;
            this.correlationId = correlationId;
            this.size = size;
            this.pformat = pformat;
            this.rotation = rotation;
        }
    }

    public class Device
    {
        internal static readonly OS OS;

        static Device ()
        {
            int env = (int)Environment.OSVersion.Platform;
            if (env == 4 || env == 128)
                OS = OS.Unix;
            else
                OS = OS.Win32;
        }

        private ArrayList equalizers;
        private EqualizerContainerRecord eqsrec;
        private TrackDatabase tracks;
        private PhotoDatabase photos;
        private SportKitManager sportKitManager;

        internal IDevice platformDevice;

        public event EventHandler Changed;

        #region Properties

        public ReadOnlyCollection<ArtworkFormat> ArtworkFormats
        {
            get
            {
                return new ReadOnlyCollection<ArtworkFormat> (
                    new List<ArtworkFormat> (platformDevice.ArtworkFormats.Values));
            }
        }

        public string ControlPath { get { return platformDevice.ControlPath; } }
        public string DevicePath { get { return platformDevice.DevicePath; } }
        public string MountPoint { get { return platformDevice.MountPoint; } }
        private string EqDbPath
        {
            get { return ControlPath + "/iTunes/iTunesEQPresets"; }
        }

        public Equalizer [] Equalizers
        {
            get
            {
                if (equalizers == null)
                    LoadEqualizers ();

                return (Equalizer [])equalizers.ToArray (typeof (Equalizer));
            }
        }
        public PhotoDatabase PhotoDatabase
        {
            get
            {
                if (photos == null) {
                    LoadPhotoDatabase ();
                }

                return photos;
            }
        }
        public TrackDatabase TrackDatabase
        {
            get
            {
                if (tracks == null) {
                    LoadTrackDatabase ();
                }

                return tracks;
            }
        }
        public SportKitManager SportKitManager
        {
            get
            {
                if (sportKitManager == null) {
                    LoadSportKitManager ();
                }

                return sportKitManager;
            }
        }

        public DeviceModel Model { get { return platformDevice.Model; } }
        public string ModelString { get { return platformDevice.ModelString; } }
        public DeviceGeneration Generation { get { return platformDevice.Generation; } }

        public string UserName
        {
            get { return platformDevice.UserName; }
            set
            {
                platformDevice.UserName = value;
                EmitChanged ();
            }
        }
        public string HostName
        {
            get { return platformDevice.HostName; }
            set
            {
                platformDevice.HostName = value;
                EmitChanged ();
            }
        }
        public string Name
        {
            get { return TrackDatabase.Name; }
            set
            {
                TrackDatabase.Name = value;
                EmitChanged ();
            }
        }

        public string AdvertisedCapacity { get { return platformDevice.AdvertisedCapacity; } }
        public string VolumeID { get { return platformDevice.VolumeId; } }
        public UInt64 VolumeSize { get { return platformDevice.VolumeSize; } }
        public UInt64 VolumeUsed { get { return platformDevice.VolumeUsed; } }
        public UInt64 VolumeAvailable { get { return platformDevice.VolumeAvailable; } }

        public bool CanWrite { get { return platformDevice.CanWrite; } }
        internal bool IsBE { get { return ControlPath.EndsWith ("iTunes_Control"); } }
        public bool IsIPod { get { return platformDevice.IsIPod; } }
        public bool IsShuffle
        {
            get
            {
                return Model == DeviceModel.Shuffle || (
                    Model >= DeviceModel.ShuffleSilver &&
                    Model <= DeviceModel.ShuffleOrange);
            }
        }

        public string ManufacturerId { get { return platformDevice.ManufacturerId; } }
        public string FirmwareVersion { get { return platformDevice.FirmwareVersion; } }
        public string ModelNumber { get { return platformDevice.ModelNumber; } }
        public uint ProductionIndex { get { return platformDevice.ProductionIndex; } }
        public uint ProductionWeek { get { return platformDevice.ProductionWeek; } }
        public uint ProductionYear { get { return platformDevice.ProductionYear; } }
        public string SerialNumber { get { return platformDevice.SerialNumber; } }
        public string VolumeLabel { get { return platformDevice.VolumeLabel; } }
        public string VolumeUuid { get { return platformDevice.VolumeUuid; } }

        private string DoNotAskPath
        {
            get
            {
                return String.Format ("{0}/.ipod-data-submit-{1}",
                    Environment.GetEnvironmentVariable ("HOME"),
                    SerialNumber);
            }
        }
        public bool ShouldAskIfUnknown
        {
            get
            {
                return Model == DeviceModel.Unknown &&
                    SerialNumber != null && SerialNumber.Length == 11 &&
                    !File.Exists (DoNotAskPath);
            }
        }
        public string UnknownIpodUrl
        {
            get
            {
                string serial = SerialNumber;
                if (serial == null || serial.Length != 11) {
                    return null;
                }

                return String.Format ("http://banshee-project.org/IpodDataSubmit?serial={0}------{1}", serial.Substring (0, 2), serial.Substring (8));
            }
        }

        #endregion

        #region Constructors

        public Device (string mountPathOrDrive)
        {
#if !DOTNET
            if (OS == OS.Unix)
                platformDevice = new Unix.Device(mountPathOrDrive);
            else //Win32
#endif
            platformDevice = new Win32.Device (mountPathOrDrive);
            platformDevice.Host = this;
        }

        internal Device (IDevice device)
        {
            platformDevice = device;
            platformDevice.Host = this;
        }

#if !DOTNET
        internal Device (IntPtr ptr)
        {
            if (OS == OS.Win32)
                throw new NotSupportedException ("This constructor is only valid on Unix systems using libipoddevice.");

            platformDevice = new Unix.Device (ptr);
            platformDevice.Host = this;
        }
#endif

        #endregion

        public void CreateEmptyTrackDatabase ()
        {
            tracks = null;
            LoadTrackDatabase (true);
        }

        public void DoNotAskIfUnknown ()
        {
            File.Open (DoNotAskPath, FileMode.Create).Close ();
        }

        private void EmitChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        internal ArtworkFormat LookupArtworkFormat (int correlationId)
        {
            return platformDevice.ArtworkFormats [correlationId];
        }

        private void LoadEqualizers ()
        {
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

        public void LoadPhotoDatabase ()
        {
            LoadPhotoDatabase (false);
        }
        public void LoadPhotoDatabase (bool createFresh)
        {
            //FIXME: refuse if the device lacks photo capability

            if (photos == null)
                photos = new PhotoDatabase (this, true, createFresh);
        }

        private void LoadSportKitManager ()
        {
            if (sportKitManager == null)
                sportKitManager = new SportKitManager (this);
        }

        public void LoadTrackDatabase ()
        {
            LoadTrackDatabase (false);
        }
        public void LoadTrackDatabase (bool createFresh)
        {
            if (!IsIPod) {
                throw new DeviceException (this, "Cannot get song database, as this device is not an iPod");
            }

            if (tracks == null)
                tracks = new TrackDatabase (this, createFresh);
        }

        public ReadOnlyCollection<ArtworkFormat> LookupArtworkFormats (ArtworkUsage usage)
        {
            List<ArtworkFormat> list = new List<ArtworkFormat> ();
            foreach (ArtworkFormat format in platformDevice.ArtworkFormats.Values) {
                if (format.Usage == usage) {
                    list.Add (format);
                }
            }

            return new ReadOnlyCollection<ArtworkFormat> (list);
        }

        public void RescanDisk ()
        {
            platformDevice.RescanDisk ();

            EmitChanged ();
        }
        public void Eject () { platformDevice.Eject (); }

        public void Save ()
        {
            // do platform specific save first
            platformDevice.Save ();

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
            }
            catch (Exception e) {
                // restore the backup
                File.Copy (EqDbPath + ".bak", EqDbPath, true);

                throw e;
            }
        }

        public Equalizer CreateEqualizer ()
        {
            if (equalizers == null)
                LoadEqualizers ();

            EqualizerRecord rec = new EqualizerRecord ();
            Equalizer eq = new Equalizer (rec);

            eqsrec.Add (rec);
            equalizers.Add (eq);

            return eq;
        }
        public void RemoveEqualizer (Equalizer eq)
        {
            equalizers.Remove (eq);
            eqsrec.Remove (eq.EqualizerRecord);
        }

        public static Device [] ListDevices ()
        {
#if !DOTNET
            if (OS == OS.Unix)
                return Unix.Device.ListDevices();
            else
#endif
            return Win32.Device.ListDevices ();
        }
    }
}
