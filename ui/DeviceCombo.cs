
using System;
using System.Collections;
using Gtk;

namespace IPod {

    public class DeviceCombo : ComboBox {

        private ListStore store;
        private DeviceEventListener listener;

        private ArrayList devices = new ArrayList ();
        private ArrayList addedUdis = new ArrayList ();
        private ArrayList removedUdis = new ArrayList ();
        private ArrayList changedDevices = new ArrayList ();
        private ThreadNotify notify;

        public Device ActiveDevice {
            get {
                TreeIter iter = TreeIter.Zero;

                if (!GetActiveIter (out iter))
                    return null;
                
                return (Device) store.GetValue (iter, 1);
            }
        }

        public DeviceCombo () : base () {
            store = new ListStore (typeof (string), typeof (Device));
            this.Model = store;

            CellRendererText renderer = new CellRendererText ();
            this.PackStart (renderer, false);
            
            this.AddAttribute (renderer, "text", 0);

            notify = new ThreadNotify (new ReadyEvent (OnNotify));
                        
            Refresh ();

            string paths = Environment.GetEnvironmentVariable ("IPOD_DEVICE_IMAGES");
            if (paths != null) {
                foreach (string path in paths.Split (':')) {
                    try {
                        Device device = new Device (path);
                        AddDevice (device);
                    } catch (Exception e) {
                        Console.Error.WriteLine ("Failed to load device at: " + path);
                    }
                }

                SetActive ();
            }
            
            listener = new DeviceEventListener ();
            listener.DeviceAdded += OnDeviceAdded;
            listener.DeviceRemoved += OnDeviceRemoved;
        }

        private void OnDeviceAdded (object o, DeviceAddedArgs args) {
            lock (this) {
                addedUdis.Add (args.Udi);
                notify.WakeupMain ();
            }
        }

        private void OnDeviceRemoved (object o, DeviceRemovedArgs args) {
            lock (this) {
                removedUdis.Add (args.Udi);
                notify.WakeupMain ();
            }
        }

        private void OnDeviceChanged (object o, EventArgs args) {
            lock (this) {
                changedDevices.Add (o);
                notify.WakeupMain ();
            }
        }

        private void OnNotify () {
            lock (this) {
                
                foreach (string udi in addedUdis) {
                    try {
                        Device device = new Device (udi);
                        AddDevice (device);
                        
                        if (ActiveDevice == null) {
                            SetActive ();
                        }
                    } catch (Exception e) {
                        Console.Error.WriteLine ("Error creating new device ({0}): {1}", udi, e);
                    }
                }

                foreach (string udi in removedUdis) {
                    RemoveDevice (udi);
                }

                foreach (Device device in changedDevices) {
                    TreeIter iter = FindDevice (device.MountPoint);

                    if (!iter.Equals (TreeIter.Zero)) {
                        store.SetValue (iter, 0, device.Name);
                        store.EmitRowChanged (store.GetPath (iter), iter);
                    }
                }

                addedUdis.Clear ();
                removedUdis.Clear ();
                changedDevices.Clear ();
            }
        }

        private TreeIter FindDevice (string mount) {
            TreeIter iter = TreeIter.Zero;

            if (!store.GetIterFirst (out iter))
                return TreeIter.Zero;
            
            do {
                Device device = (Device) store.GetValue (iter, 1);

                if (device != null) {
                    
                    if (device.MountPoint == mount)
                        return iter;
                }

            } while (store.IterNext (ref iter));

            return TreeIter.Zero;
        }

        private void ClearPlaceholder () {
            TreeIter iter = TreeIter.Zero;

            if (!store.GetIterFirst (out iter))
                return;
            
            do {
                Device device = (Device) store.GetValue (iter, 1);
                if (device == null) {
                    store.Remove (ref iter);
                    break;
                }
            } while (store.IterNext (ref iter));
        }

        private void Refresh () {
            string current = null;
            bool haveActive = false;
            
            if (ActiveDevice != null)
                current = ActiveDevice.MountPoint;
            
            store.Clear ();

            foreach (Device device in Device.ListDevices ()) {
                TreeIter iter = AddDevice (device);
                
                if (device.MountPoint == current) {
                    SetActiveIter (iter);
                    haveActive = true;
                }
            }

            if (!haveActive) {
                SetActive ();
            }
        }

        private void SetActive () {
            if (store.IterNChildren () == 0) {
                store.AppendValues ("No iPod Found", null);
                this.Active = 0;
            } else {
                this.Active = 0;
            }
        }

        private TreeIter AddDevice (Device device) {
            ClearPlaceholder ();

            device.Changed += OnDeviceChanged;

            // gtk-sharp doesn't ensure that I will get the same managed
            // instance when pulling this out of the store, so hold a
            // ref to it here
            devices.Add (device);

            return store.AppendValues (device.Name, device);
        }

        private void RemoveDevice (string udi) {
            TreeIter iter = FindDevice (udi);

            if (iter.Equals (TreeIter.Zero))
                return;

            Device device = (Device) store.GetValue (iter, 1);

            device.Changed -= OnDeviceChanged;
            devices.Remove (device);
            
            if (!iter.Equals (TreeIter.Zero)) {
                store.Remove (ref iter);
            }

            SetActive ();
        }
    }
}
