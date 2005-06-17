using System;

namespace IPod {

    public class DeviceBusyException : DeviceException {

        public DeviceBusyException (Device device) : base (device, "The device is busy") {
        }
    }
}
