using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IPod {

    public abstract class DeviceManager {

        public event DeviceHandler DeviceAdded;
        public event DeviceHandler DeviceRemoved;

        public abstract ReadOnlyCollection<Device> Devices { get; }

        internal DeviceManager () {
        }

        public static DeviceManager Create () {
#if WINDOWS
            return new Windows.WindowsDeviceManager ();
#else
            return new HalClient.HalDeviceManager ();
#endif
        }

        protected void EmitAdded (Device device) {
            if (DeviceAdded != null) {
                DeviceAdded (this, new DeviceArgs (device));
            }
        }

        protected void EmitRemoved (Device device) {
            if (DeviceRemoved != null) {
                DeviceRemoved (this, new DeviceArgs (device));
            }
        }
    }
}
