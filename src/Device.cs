using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;

namespace IPod
{
    public enum ArtworkUsage : int
    {
        Unknown = -1,
        Photo,
        Cover,
        Chapter,
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

        public short CorrelationId
        {
            get { return correlationId; }
        }

        public ArtworkFormat (ArtworkUsage usage, short width, short height, short correlationId,
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

    public class DeviceArgs : EventArgs {

        private Device device;
        
        public Device Device {
            get { return device; }
        }
        
        public DeviceArgs (Device device) {
            this.device = device;
        }
    }

    public delegate void DeviceHandler (object o, DeviceArgs args);

    public abstract class Device
    {
        private ArrayList equalizers;
        private EqualizerContainerRecord eqsrec;
        private TrackDatabase tracks;
        private PhotoDatabase photos;
        private SportKitManager sportKitManager;
        private Dictionary<int, ArtworkFormat> artFormats = new Dictionary<int, ArtworkFormat> ();
        private string mountPoint;
        private string controlPath;
        private string modelClass;
        private string advertisedCapacity;
        private string modelColor;
        private double generation;
        private string manuId;
        private string firmwareVersion;
        private uint prodIndex;
        private uint prodWeek;
        private uint prodYear;
        private string serialNumber;
        private string volumeId;
        private bool canWrite;
        private string volumeLabel;
        private string volumeUuid;
        private string firewireId;
        
        public event EventHandler Changed;

        #region Properties

        public ReadOnlyCollection<ArtworkFormat> ArtworkFormats
        {
            get {
                return new ReadOnlyCollection<ArtworkFormat> (new List<ArtworkFormat> (artFormats.Values));
            } set {
                artFormats.Clear ();
                foreach (ArtworkFormat format in value) {
                    artFormats[format.CorrelationId] = format;
                }
            }
        }

        public string ControlPath {
            get {
                if (controlPath == null) {
                    return Path.Combine (MountPoint, "iPod_Control");
                } else {
                    return controlPath;
                }
            }
            set { controlPath = value; }
        }
        
        public string MountPoint {
            get { return mountPoint; }
            set { mountPoint = value; }
        }
        
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

        public string ModelClass {
            get { return modelClass; }
            set { modelClass = value; }
        }
                
        public string AdvertisedCapacity {
            get { return advertisedCapacity; }
            set { advertisedCapacity = value; }
        }
        
        public string ModelColor {
            get { return modelColor; }
            set { modelColor = value; }
        }
        
        public double Generation {
            get { return generation; }
            set { generation = value; }
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

        public string VolumeID {
            get { return volumeId; }
            set { volumeId = value; }
        }

        public bool CanWrite {
            get { return canWrite; }
            set { canWrite = value; }
        }
        
        public string VolumeLabel {
            get { return volumeLabel; }
            set { volumeLabel = value; }
        }
        
        public string VolumeUuid {
            get { return volumeUuid; }
            set { volumeUuid = value; }
        }
        
        public abstract UInt64 VolumeSize { get; }
        public abstract UInt64 VolumeUsed { get; }
        public abstract UInt64 VolumeAvailable { get; }

        public bool AlbumArtSupported {
            get {
                return LookupArtworkFormats (ArtworkUsage.Cover).Count > 0;
            }
        }
        
        public bool PhotosSupported {
            get {
                return LookupArtworkFormats (ArtworkUsage.Photo).Count > 0;
            }
        }

        internal bool IsBE { get { return ControlPath.EndsWith ("iTunes_Control"); } }
        public bool IsShuffle
        {
            get
            {
                return ModelClass == "shuffle";
            }
        }

        public string ManufacturerID {
            get { return manuId; }
            set { manuId = value; }
        }
                
        public string FirmwareVersion {
            get { return firmwareVersion; }
            set { firmwareVersion = value; }
        }
        
        public uint ProductionIndex {
            get { return prodIndex; }
            set { prodIndex = value; }
        }
        
        public uint ProductionWeek {
            get { return prodWeek; }
            set { prodWeek = value; }
        }
        
        public uint ProductionYear {
            get { return prodYear; }
            set { prodYear = value; }
        }
        
        public string SerialNumber {
            get { return serialNumber; }
            set { serialNumber = value; }
        }

        public string FirewireID {
            get { return firewireId; }
            set { firewireId = value; }
        }
        
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
                return ModelClass == "unknown" &&
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

        internal Device (string mountPointOrDrive, List<ArtworkFormat> artFormats,
                         string modelClass) {
            this.mountPoint = mountPointOrDrive;
            this.modelClass = modelClass;

            this.artFormats = new Dictionary<int, ArtworkFormat> ();
            foreach (ArtworkFormat format in artFormats) {
                this.artFormats[format.CorrelationId] = format;
            }
        }

        #endregion

        internal Device () {
        }
        
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
            return artFormats [correlationId];
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
            if (photos == null && PhotosSupported)
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
            if (tracks == null)
                tracks = new TrackDatabase (this, createFresh);
        }

        public ReadOnlyCollection<ArtworkFormat> LookupArtworkFormats (ArtworkUsage usage)
        {
            List<ArtworkFormat> list = new List<ArtworkFormat> ();
            foreach (ArtworkFormat format in artFormats.Values) {
                if (format.Usage == usage) {
                    list.Add (format);
                }
            }

            return new ReadOnlyCollection<ArtworkFormat> (list);
        }

        public abstract void RescanDisk ();
        public abstract void Eject ();

        public void Save ()
        {
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

        public void Dump () {
            Console.WriteLine ("Class: " + ModelClass);
            Console.WriteLine ("Generation: " + Generation);
            Console.WriteLine ("Advertised Capacity: " + AdvertisedCapacity);
            Console.WriteLine ("Serial Number: " + SerialNumber);
            Console.WriteLine ("Volume Size: " + VolumeSize);
            Console.WriteLine ("Volume Available: " + VolumeAvailable);
            Console.WriteLine ("Firewire ID: " + FirewireID);
            Console.WriteLine ("Song Count: " + TrackDatabase.Tracks.Count);
            Console.WriteLine ("\nPhoto Formats:");
            foreach (ArtworkFormat format in LookupArtworkFormats (ArtworkUsage.Photo)) {
                Console.WriteLine ("{0}x{1} using {2}, rotated {3}", format.Width, format.Height,
                                   format.PixelFormat, format.Rotation);
            }
        }
    }
}
