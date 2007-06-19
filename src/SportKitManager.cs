using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace IPod {

    public enum DistanceUnit {
        Kilometers,
        Miles
    }

    public class Workout {

        public string FileName {
            get { return filename; }
        }

        public string Version {
            get { return version; }
        }

        public string Name {
            get { return name; }
        }
        
        public DateTime Time {
            get { return time; }
        }

        public TimeSpan Duration {
            get { return duration; }
        }

        public double Distance {
            get { return distance; }
        }

        public DistanceUnit DistanceUnit {
            get { return distanceUnit; }
        }

        public string Pace {
            get { return pace; }
        }

        public int Calories {
            get { return calories; }
        }

        public string TemplateID {
            get { return templateId; }
        }

        public string TemplateName {
            get { return templateName; }
        }

        public DateTime StartTime {
            get { return startTime; }
        }

        public int IntervalValue {
            get { return intervalValue; }
        }

        public double[] Intervals {
            get { return intervals; }
        }

        internal Workout (string path) {
            this.filename = path;
        }
    
        public static Workout Create (string filename) {
            if (!File.Exists (filename))
                return null;

            Workout workout = null;
            try {
                workout = new Workout (filename);
                workout.Reload ();
            } catch (Exception e) {
                Console.WriteLine (e);
                return null;
            }

            return workout;
        }

        public void Reload () {
            XmlTextReader reader = new XmlTextReader (filename);
            XmlDocument doc = new XmlDocument ();
            XmlNode node;

            doc.Load (reader);

            node = doc.SelectSingleNode ("/sportsData/vers");
            if (node == null)
                throw new FormatException ("No version number found in the sportsData tag.  This doesn't appear to be a iPod Sport formatted file.");

            version = node.InnerText;
            switch (version) {
                case "1":
                case "2":
                    ParseDatabaseVersion1And2 (doc);
                    break;
                default:
                    Console.WriteLine ("Found unsupported sportsData version {0}.  Continuing, but things may break.", node.InnerText);
                    break;
            }

            reader.Close ();
        }

        private void ParseDatabaseVersion1And2 (XmlDocument doc) {
            XmlNode root, node, subnode;

            // Run Summary section
            root = doc.SelectSingleNode ("/sportsData/runSummary");
            if (root == null)
                return;
            
            node = root.SelectSingleNode ("workoutName");
            if (node != null)
                name = node.InnerText;

            node = root.SelectSingleNode ("time");
            if (node != null)
                time = DateTime.Parse (node.InnerText);

            node = root.SelectSingleNode ("duration");
            if (node != null)
                duration = TimeSpan.FromMilliseconds (Convert.ToInt64 (node.InnerText));

            node = root.SelectSingleNode ("distance");
            if (node != null) {
                distance = Convert.ToDouble (node.InnerText);

                subnode = node.Attributes["unit"];
                if (subnode != null) {
                    if (subnode.InnerText == KILOMETERS)
                        distanceUnit = DistanceUnit.Kilometers;
                    else if (subnode.InnerText == MILES)
                        distanceUnit = DistanceUnit.Miles;
                }
            }

            node = root.SelectSingleNode ("pace");
            if (node != null)
                pace = node.InnerText;

            node = root.SelectSingleNode ("calories");
            if (node != null)
                calories = Convert.ToInt32 (node.InnerText);

            // ignore battery (what is this for?)
            // ignore stepCounts (do we need this?)
            // v2: ignore powerSong

            // Template section
            root = doc.SelectSingleNode ("/sportsData/template");
            if (root != null) {
                node = root.SelectSingleNode ("templateID");
                if (node != null)
                    templateId = node.InnerText;

                node = root.SelectSingleNode ("templateName");
                if (node != null)
                    templateName = node.InnerText;
            }

            // TODO: Goal section
            // TODO: UserInfo section
            
            // StartTime section
            root = doc.SelectSingleNode ("/sportsData/startTime");
            if (root != null)
                startTime = DateTime.Parse (root.InnerText);

            // TODO: Snap Shot List section
            
            // Extended Data List section
            root = doc.SelectSingleNode ("/sportsData/extendedDataList");

            // XXX: Can you have more than one of these?
            node = root.SelectSingleNode ("extendedData");
            if (node != null) {
                subnode = node.Attributes["dataType"];
                if (subnode != null && subnode.InnerText != "distance")
                    throw new FormatException ("Got unknown extendedData dataType: " + subnode.InnerText);

                subnode = node.Attributes["intervalType"];
                if (subnode != null && subnode.InnerText != "time")
                    throw new FormatException ("Got unknown extendedData intervalType: " + subnode.InnerText);

                subnode = node.Attributes["intervalUnit"];
                if (subnode != null && subnode.InnerText != "s")
                    throw new FormatException ("Got unknown extendedData intervalUnit: " + subnode.InnerText);

                subnode = node.Attributes["intervalValue"];
                if (subnode != null)
                    intervalValue = Convert.ToInt32 (subnode.InnerText);

                // Now, parse a long, comma + space delimited list of intervals
                string[] intervalTokens = node.InnerText.Split (',');

                intervals = new double[intervalTokens.Length];
                for (int i = 0; i < intervalTokens.Length; i++) {
                    intervals[i] = Convert.ToDouble (intervalTokens[i].Trim ());
                }
            }
        }

        private string filename = String.Empty;
        private string version = String.Empty;
        private string name = String.Empty;
        private DateTime time = DateTime.MinValue;
        private TimeSpan duration = TimeSpan.Zero;
        private double distance = 0;
        private DistanceUnit distanceUnit;
        private string pace = String.Empty;
        private int calories = 0;

        private string templateId = String.Empty;
        private string templateName = String.Empty;

        private DateTime startTime = DateTime.MinValue;

        private int intervalValue = 10;
        private double[] intervals = new double[0];

        private const string KILOMETERS = "km";
        private const string MILES = "mi";
    }

    // TODO: Implement SportKitSettings once we figure out how to publish to Nike+

    public class SportKit {

        public string ID {
            get { return id; }
        }

        public string Path {
            get { return sportKitPath; }
        }

        public Workout[] LatestWorkouts {
            get { return latestWorkouts; }
        }

        public Workout[] SynchedWorkouts {
            get { return synchedWorkouts; }
        }

        internal SportKit (string id, string path) {
            this.id = id;
            this.sportKitPath = path;
        }

        public static SportKit Create (string id, string sportKitPath) {
            if (!Directory.Exists (sportKitPath))
                return null;

            if (!File.Exists (System.IO.Path.Combine (sportKitPath, CANARY_FILE)))
                return null;

            SportKit kit = null;
            try {
                    kit = new SportKit (id, sportKitPath);
                    kit.Reload ();
            } catch {
                return null;
            }

            return kit;
        }

        public void Reload () {
            latestWorkouts = FetchWorkouts (System.IO.Path.Combine (sportKitPath, LATEST_DIR));
            synchedWorkouts = FetchWorkouts (System.IO.Path.Combine (sportKitPath, SYNCHED_DIR));
        }

        private Workout[] FetchWorkouts (string path) {
            List<Workout> workouts = new List<Workout> ();

            foreach (string workout in Directory.GetFiles (path)) {
                Workout w = Workout.Create (workout);
                if (w == null) {
                    // parsing failed, pass it up
                    continue;
                }

                workouts.Add (w);
            }
           
            return workouts.ToArray ();
        }
	
        private string id = String.Empty;
        private string sportKitPath = String.Empty;
        private Workout[] latestWorkouts = new Workout[0];
        private Workout[] synchedWorkouts = new Workout[0];

        private const string LATEST_DIR = "latest";
        private const string SYNCHED_DIR = "synched";
        private const string CANARY_FILE = "settings.plist";
    }

    public class SportKitManager {

        public Device Device {
            get { return device; }
        }

        public SportKit[] SportKits {
            get { return sportKits; }
        }

        internal SportKitManager (Device device) {
            this.device = device;

            Reload ();
        }

        public void Reload () {
            string empedsPath = Path.Combine (device.ControlPath,
                                              EMPEDS_FOLDER.Replace ('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists (empedsPath)) {
                // either this iPod hasn't had a Sport Kit attached to it, or
                // the control files have been nuked
                sportKits = new SportKit[0];
                return;
            }

            // TODO: figure out how linkData works, and what bits to read to
            // get a list of emped ids

            string[] kitDirs = Directory.GetDirectories (empedsPath);
            
            List<SportKit> kits = new List<SportKit> ();
            foreach (string kitDir in kitDirs) {
                string kitId = Path.GetFileName (kitDir);
                
                SportKit kit = SportKit.Create (kitId, kitDir);
                if (kit != null)
                    kits.Add (kit);
            }

            sportKits = kits.ToArray ();
        }

        private Device device;
        private SportKit[] sportKits = new SportKit[0];
        
        private const string EMPEDS_FOLDER = "Device/Trainer/Workouts/Empeds";
    }

}
