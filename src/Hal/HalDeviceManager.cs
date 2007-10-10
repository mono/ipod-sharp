using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NDesk.DBus;

using Hal;

namespace IPod.HalClient {

    internal class HalDeviceManager : DeviceManager {

        private Dictionary<string, HalDevice> devices = new Dictionary<string, HalDevice> ();
        private Dictionary<string, Volume> watchedVolumes = new Dictionary<string, Volume> ();

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

            foreach (Hal.Device device in manager.FindDeviceByCapabilityAsDevice ("volume")) {
                if (IsIPod (device)) {
                    MaybeAddVolume (new Volume (device.Udi));
                }
            }

            manager.DeviceAdded += OnDeviceAdded;
            manager.DeviceRemoved += OnDeviceRemoved;
        }

        private void OnDeviceAdded (object o, DeviceAddedArgs args) {
            if (IsIPod (args.Device)) {
                MaybeAddVolume (new Volume (args.Udi));
            }
        }

        private void OnDeviceRemoved (object o, DeviceRemovedArgs args) {
            RemoveVolume (args.Udi, true);
        }

        private bool IsIPod (Hal.Device device) {
            return device.PropertyExists ("org.podsleuth.version");
        }

        private bool IsMounted (Hal.Device device) {
            try {
                if (!device.PropertyExists ("volume.mount_point"))
                    return false;
            } catch {
                return false;
            }

            string mountPoint = device.GetPropertyString ("volume.mount_point");
            return mountPoint != null && mountPoint != String.Empty;
        }

        private void OnVolumeModified (object o, PropertyModifiedArgs args) {
            Volume volume = o as Volume;
            
            foreach (PropertyModification mod in args.Modifications) {
                if (mod.Key == "volume.mount_point") {
                    if (IsMounted (volume)) {
                        AddVolume (volume);
                    } else {
                        RemoveVolume (volume.Udi, false);
                    }
                }
            }
        }
        
        private void WatchForMount (Volume volume) {
            volume.PropertyModified += OnVolumeModified;
            watchedVolumes[volume.Udi] = volume;
        }

        private void UnwatchForMount (string udi) {
            watchedVolumes[udi].PropertyModified -= OnVolumeModified;
            watchedVolumes.Remove (udi);
        }

        private void MaybeAddVolume (Volume volume) {
            if (!IsIPod (volume))
                return;

            if (devices.ContainsKey (volume.Udi)) {
                return;
            }

            WatchForMount (volume);
            if (IsMounted (volume)) {
                AddVolume (volume);
            }
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

        private void RemoveVolume (string udi, bool unwatch) {
            if (devices.ContainsKey (udi)) {
                HalDevice device = devices[udi];
                devices.Remove (udi);

                if (unwatch)
                    UnwatchForMount (udi);
                
                EmitRemoved (device);
            }
        }
    }
}
