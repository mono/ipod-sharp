using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IPod
{
    public abstract class ModelInfo
    {
        private bool is_unknown;
        private bool album_art_supported;
        private bool photos_supported;
        private string device_class;
        private string shell_color;
        private string advertised_capacity;
        private double generation;
        private string icon_name;
        private List<string> capabilities = new List<string> ();
        
        protected void AddCapability (string capability)
        {
            capabilities.Add (capability);
        }
        
        public bool HasCapability (string capability)
        {
            return capabilities.Contains (capability);
        }
        
        public bool IsUnknown {
            get { return is_unknown; }
            protected set { is_unknown = value; }
        }
        
        public bool AlbumArtSupported {
            get { return album_art_supported; }
            protected set { album_art_supported = value; }
        }
        
        public bool PhotosSupported {
            get { return photos_supported; }
            protected set { photos_supported = value; }
        }
        
        public string DeviceClass {
            get { return device_class; }
            protected set { device_class = value; }
        }
        
        public string ShellColor {
            get { return shell_color; }
            protected set { shell_color = value; }
        }
        
        public string AdvertisedCapacity {
            get { return advertised_capacity; }
            protected set { advertised_capacity = value; }
        }
        
        public double Generation {
            get { return generation; }
            protected set { generation = value; }
        }
        
        public string IconName {
            get { return icon_name; }
            protected set { icon_name = value; }
        }
        
        public ReadOnlyCollection<string> Capabilities {
            get { return new ReadOnlyCollection<string> (capabilities); }
        }
        
        public void Dump ()
        {
            Console.WriteLine ("  Is Unknown: {0}", IsUnknown);
            Console.WriteLine ("  Device Class: {0}", DeviceClass);
            Console.WriteLine ("  Shell Color:  {0}", ShellColor);
            Console.WriteLine ("  Generation:   {0}", Generation);
            Console.WriteLine ("  Icon Name:    {0}", IconName);
            Console.WriteLine ("  Advertised Capacity: {0}", AdvertisedCapacity);
            Console.WriteLine ("  Photos Supported: {0}", PhotosSupported);
            Console.WriteLine ("  Album Art Supported: {0}", AlbumArtSupported);
            
            Console.Write ("  Extra Capabilities: ");
            
            if (capabilities == null || capabilities.Count == 0) {
                Console.WriteLine ("None");
            } else {
                foreach (string capability in Capabilities) {
                    Console.Write (capability);
                    Console.Write (", ");
                }
                Console.WriteLine ();
            }
        }
    }
}
