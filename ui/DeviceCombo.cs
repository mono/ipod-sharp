
using System;
using Gtk;

namespace IPod {

    public class DeviceCombo : ComboBox {

        private ListStore store;

        public Device ActiveDevice {
            get {
                TreeIter iter = TreeIter.Zero;

                if (!GetActiveIter (out iter))
                    return null;
                
                return (Device) store.GetValue (iter, 1);
            }
        }

        public DeviceCombo () : base () {
            store = new ListStore (GLib.GType.String, Device.GType);
            this.Model = store;

            CellRendererText renderer = new CellRendererText ();
            this.PackStart (renderer, false);
            
            this.AddAttribute (renderer, "text", 0);

            Refresh ();
        }

        public void Refresh () {
            Refresh (null);
        }
        
        private void Refresh (string omit) {
            string current = null;
            bool haveActive = false;
            
            if (ActiveDevice != null)
                current = ActiveDevice.MountPoint;
            
            store.Clear ();

            foreach (Device device in Device.ListDevices ()) {
                if (device.MountPoint != omit) {
                    TreeIter iter = store.AppendValues (device.Name, device);

                    if (device.MountPoint == current) {
                        SetActiveIter (iter);
                        haveActive = true;
                    }
                }
            }

            if (store.IterNChildren () == 0) {
                store.AppendValues ("No iPod Found", null);
                this.Active = 0;
            } else if (!haveActive) {
                this.Active = 0;
            }
        }

        public void EjectActive () {
            Device device = ActiveDevice;

            if (device == null)
                return;

            string omit = device.MountPoint;
            device.Eject ();
            Refresh (omit);
        }
    }
}
