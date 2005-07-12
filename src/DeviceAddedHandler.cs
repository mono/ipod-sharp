namespace IPod {

    using System;

    public delegate void DeviceAddedHandler(object o, DeviceAddedArgs args);

    public class DeviceAddedArgs : GLib.SignalArgs {
        public string Udi {
            get {
                return (string) Args[0];
            }
        }

    }
}
