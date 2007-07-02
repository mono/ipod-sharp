#if !DOTNET

namespace IPod.Unix {

    using System;

    public delegate void DeviceRemovedHandler(object o, DeviceRemovedArgs args);

    public class DeviceRemovedArgs : GLib.SignalArgs {
        public string Udi {
            get {
                return (string) Args[0];
            }
        }

    }
}

#endif