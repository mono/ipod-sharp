using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NDesk.DBus;

namespace IPod.Hal {

    public class HalDeviceManager : DeviceManager {

        private Dictionary<string, HalDevice> devices = new Dictionary<string, HalDevice> ();

        public override ReadOnlyCollection<IPod.Device> Devices {
            get {
                List<IPod.Device> ret = new List<IPod.Device> ();
                foreach (HalDevice dev in devices.Values) {
                    ret.Add (dev);
                }

                return new ReadOnlyCollection<IPod.Device> (ret);
            }
        }

        public HalDeviceManager () {
            Manager manager = new Manager ();

            foreach (Device device in manager.FindDeviceByCapabilityAsDevice ("volume")) {
                if (IsIPod (device)) {
                    AddVolume (new Volume (device.Udi));
                }
            }

            manager.DeviceAdded += OnDeviceAdded;
            manager.DeviceRemoved += OnDeviceRemoved;
        }

        private void OnDeviceAdded (object o, IPod.Hal.DeviceAddedArgs args) {
            if (IsIPod (args.Device)) {
                AddVolume (new Volume (args.Udi));
            }
        }

        private void OnDeviceRemoved (object o, IPod.Hal.DeviceRemovedArgs args) {
            RemoveVolume (args.Udi);
        }

        private bool IsIPod (Device device) {
            if (!device.PropertyExists ("volume.mount_point"))
                return false;

            string mountPoint = device.GetPropertyString ("volume.mount_point");
            return device.PropertyExists ("org.banshee-project.podsleuth.version") &&
                mountPoint != null && mountPoint != String.Empty;
        }
        
        private void AddVolume (Volume volume) {
            try {
                HalDevice device = new HalDevice (volume);
                
                devices[volume.Udi] = device;
                EmitAdded (device);
            } catch (Exception e) {
                Console.Error.WriteLine ("ipod-sharp: Failed to add device: " + e);
            }
        }

        private void RemoveVolume (string udi) {
            if (devices.ContainsKey (udi)) {
                HalDevice device = devices[udi];
                devices.Remove (udi);
                EmitRemoved (device);
            }
        }
    }
}
