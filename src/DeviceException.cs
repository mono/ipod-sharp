
using System;
using System.Runtime.Serialization;

namespace IPod {

    [Serializable]
    public class DeviceException : Exception {

        private Device device;

        public Device Device {
            get { return device; }
        }

        public DeviceException (SerializationInfo info, StreamingContext context) {
        }
        
        public DeviceException (Device device, string msg) : this (device, msg, null) {
        }

        public DeviceException (Device device, string msg, Exception root) : base (msg, root) {
            this.device = device;
        }
    }
}
