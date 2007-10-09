using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IPod
{
    public class DeviceArgs : EventArgs 
    {
        private Device device;
        
        public DeviceArgs (Device device) 
        {
            this.device = device;
        }
        
        public Device Device {
            get { return device; }
        }
    }

    public delegate void DeviceHandler (object o, DeviceArgs args);

    public abstract class Device
    {
        #region Database Fields
        private TrackDatabase track_database;
        private PhotoDatabase photo_database;
        private Dictionary<int, ArtworkFormat> artwork_formats = new Dictionary<int, ArtworkFormat> ();
        private SportKitManager sport_kit_manager;
        private List<Equalizer> equalizers;
        private EqualizerContainerRecord equalizer_container_record;
        #endregion
        
        #region Hardware Fields
        private string control_path;
        private string firmware_version;
        private string firewire_id;
        #endregion 
        
        public event EventHandler Changed;
        
        protected Device () 
        {
        }

        #region Database Properties

        public TrackDatabase TrackDatabase {
            get {
                if (track_database == null) {
                    LoadTrackDatabase ();
                }

                return track_database;
            }
        }
        
        public PhotoDatabase PhotoDatabase {
            get {
                if (photo_database == null) {
                    LoadPhotoDatabase ();
                }

                return photo_database;
            }
        }
    
        public ReadOnlyCollection<ArtworkFormat> ArtworkFormats
        {
            get { return new ReadOnlyCollection<ArtworkFormat> (new List<ArtworkFormat> (artwork_formats.Values)); }
            set {
                artwork_formats.Clear ();
                foreach (ArtworkFormat format in value) {
                    artwork_formats[format.CorrelationId] = format;
                }
            }
        }
        
        public SportKitManager SportKitManager {
            get {
                if (sport_kit_manager == null) {
                    LoadSportKitManager ();
                }

                return sport_kit_manager;
            }
        }
        
        public ReadOnlyCollection<Equalizer> Equalizers {
            get {
                if (equalizers == null) {
                    LoadEqualizers ();
                }
                
                return new ReadOnlyCollection<Equalizer> (equalizers);
            }
        }
        
        #endregion

        #region Path Properties
        
        public string ControlPath {
            get { return control_path ?? Path.Combine (VolumeInfo.MountPoint, "iPod_Control"); }
            set { control_path = value; }
        }

        public bool HasTrackDatabase {
            get { return File.Exists(TrackDatabasePath); }
        }
        
        public string TrackDatabasePath {
            get { return String.Format("{0}{1}iTunes{1}iTunesDB", ControlPath, Path.DirectorySeparatorChar); }
        }

        private string EqualizerDatabasePath {
            get { return String.Format ("{0}{1}iTunes{1}iTunesEQPresets", ControlPath, Path.DirectorySeparatorChar); }
        }
        
        private string DoNotAskPath {
            get {
                return String.Format ("{0}{1}.ipod-data-submit-{2}",
                    Environment.GetEnvironmentVariable ("HOME"),
                    Path.DirectorySeparatorChar,
                    ProductionInfo.SerialNumber);
            }
        }
        
        #endregion

        #region Hardware properties

        public string Name {
            get { return TrackDatabase.Name; }
            set {
                TrackDatabase.Name = value;
                OnChanged ();
            }
        }

        public abstract VolumeInfo VolumeInfo { get; }
        public abstract ProductionInfo ProductionInfo { get; }
        public abstract ModelInfo ModelInfo { get; }

        internal bool IsBE { 
            get { return ControlPath.EndsWith ("iTunes_Control"); } 
        }

        public string FirmwareVersion {
            get { return firmware_version; }
            set { firmware_version = value; }
        }
    
        public string FirewireId {
            get { return firewire_id; }
            set { firewire_id = value; }
        }
        
        public bool ShouldAskIfUnknown {
            get {
                return ModelInfo.IsUnknown &&
                    ProductionInfo.SerialNumber != null && ProductionInfo.SerialNumber.Length == 11 &&
                    !File.Exists (DoNotAskPath);
            }
        }
        
        public string UnknownIpodUrl {
            get {
                string serial = ProductionInfo.SerialNumber;
                if (serial == null || serial.Length != 11) {
                    return null;
                }

                return String.Format ("http://banshee-project.org/IpodDataSubmit?serial={0}------{1}", serial.Substring (0, 2), serial.Substring (8));
            }
        }

        #endregion
        
        #region Private Methods
        
        private void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }
        
        internal ArtworkFormat LookupArtworkFormat (int correlationId)
        {
            return artwork_formats [correlationId];
        }

        private void LoadEqualizers ()
        {
            equalizers = new List<Equalizer> ();
            equalizer_container_record = new EqualizerContainerRecord ();

            if (!File.Exists (EqualizerDatabasePath)) {
                return;
            }
            
            using (BinaryReader reader = new BinaryReader (File.Open (EqualizerDatabasePath, FileMode.Open))) {
                equalizer_container_record.Read (reader);

                foreach (EqualizerRecord eqrec in equalizer_container_record.EqualizerRecords) {
                    Equalizer eq = new Equalizer (eqrec);
                    equalizers.Add (eq);
                }
            }
        }
        
        private void LoadSportKitManager ()
        {
            if (sport_kit_manager == null) {
                sport_kit_manager = new SportKitManager (this);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void CreateEmptyTrackDatabase ()
        {
            track_database = null;
            LoadTrackDatabase (true);
        }

        public void DoNotAskIfUnknown ()
        {
            File.Open (DoNotAskPath, FileMode.Create).Close ();
        }
        
        public void LoadTrackDatabase ()
        {
            LoadTrackDatabase (false);
        }
        
        public void LoadTrackDatabase (bool createFresh)
        {
            if (track_database == null)
                track_database = new TrackDatabase (this, createFresh);
        }
        
        public void LoadPhotoDatabase ()
        {
            LoadPhotoDatabase (false);
        }
        
        public void LoadPhotoDatabase (bool createFresh)
        {
            if (photo_database == null && ModelInfo.PhotosSupported)
                photo_database = new PhotoDatabase (this, true, createFresh);
        }

        public ReadOnlyCollection<ArtworkFormat> LookupArtworkFormats (ArtworkUsage usage)
        {
            List<ArtworkFormat> list = new List<ArtworkFormat> ();
            foreach (ArtworkFormat format in artwork_formats.Values) {
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
            if (TrackDatabase != null) {
                TrackDatabase.Save ();
            }
            
            if (PhotoDatabase != null) {
                PhotoDatabase.Save ();
            }
            
            // nothing more to do
            if (equalizers == null) {
                return;
            }
            
            string backup_path = String.Format("{0}.bak", EqualizerDatabasePath);
            
            try {
                // Back up the eq db
                if (File.Exists (EqualizerDatabasePath)) {
                    File.Copy (EqualizerDatabasePath, backup_path, true);
                }
                
                // Save the eq db
                using (BinaryWriter writer = new BinaryWriter (new FileStream (EqualizerDatabasePath, FileMode.Create))) {
                    equalizer_container_record.Save (writer);
                }
            } catch (Exception e) {
                // restore the backup
                File.Copy (backup_path, EqualizerDatabasePath, true);
                throw e;
            }
        }

        public Equalizer CreateEqualizer ()
        {
            if (equalizers == null) {
                LoadEqualizers ();
            }
            
            EqualizerRecord record = new EqualizerRecord ();
            Equalizer equalizer = new Equalizer (record);

            equalizer_container_record.Add (record);
            equalizers.Add (equalizer);

            return equalizer;
        }
        
        public void RemoveEqualizer (Equalizer equalizer)
        {
            equalizers.Remove (equalizer);
            equalizer_container_record.Remove (equalizer.EqualizerRecord);
        }

        public void Dump () 
        {
            Console.WriteLine ("iPod");
            Console.WriteLine (" Firewire ID:      {0}", FirewireId);
            Console.WriteLine (" Firmware Version: {0}", FirmwareVersion);
            Console.WriteLine (" Control Path:     {0}", ControlPath);
            Console.WriteLine ();
            
            
            Console.WriteLine (" Volume Information");
            VolumeInfo.Dump ();
            Console.WriteLine ();
            
            Console.WriteLine (" Model Information");
            ModelInfo.Dump ();
            Console.WriteLine ();
            
            Console.WriteLine (" Production Information");
            ProductionInfo.Dump ();
            Console.WriteLine ();
            
            Console.WriteLine (" Database Information");
            Console.WriteLine ("  Song Count: " + TrackDatabase.Tracks.Count);
            Console.WriteLine ("  Photo Formats:");
            
            foreach (ArtworkFormat format in LookupArtworkFormats (ArtworkUsage.Photo)) {
                Console.WriteLine ("   {0}x{1} using {2}, rotated {3}", format.Width, 
                    format.Height, format.PixelFormat, format.Rotation);
            }
        }
        
        #endregion
    }
}
