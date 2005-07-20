
using System;
using System.Collections;
using Gtk;

namespace IPod {

    public class DeviceCombo : ComboBox {

        private ListStore store;
        private DeviceEventListener listener;

        private ArrayList addedUdis = new ArrayList ();
        private ArrayList removedUdis = new ArrayList ();
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
            store = new ListStore (GLib.GType.String, Device.GType);
            this.Model = store;

            CellRendererText renderer = new CellRendererText ();
            this.PackStart (renderer, false);
            
            this.AddAttribute (renderer, "text", 0);

            Refresh ();

            notify = new ThreadNotify (new ReadyEvent (OnNotify));
            
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
                    TreeIter iter = FindDevice (udi);
                    
                    if (!iter.Equals (TreeIter.Zero)) {
                        store.Remove (ref iter);
                    }
                    
                    if (ActiveDevice == null) {
                        SetActive ();
                    }
                }

                addedUdis.Clear ();
                removedUdis.Clear ();
            }
        }

        

        private TreeIter FindDevice (string udi) {
            TreeIter iter = TreeIter.Zero;

            if (!store.GetIterFirst (out iter))
                return TreeIter.Zero;
            
            do {
                Device device = (Device) store.GetValue (iter, 1);

                if (device != null) {
                    
                    if (device.VolumeId == udi)
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
            return store.AppendValues (device.Name, device);
        }

        private void RemoveDevice (Device dev) {
            TreeIter iter = TreeIter.Zero;

            if (!store.IterChildren (out iter))
                return;

            do {
                Device dev2 = (Device) store.GetValue (iter, 1);
                if (dev2 == dev) {
                    store.Remove (ref iter);
                    break;
                }
            } while (store.IterNext (ref iter));

            SetActive ();
        }

        public void EjectActive () {
            Device device = ActiveDevice;

            if (device == null)
                return;

            device.Eject ();
            RemoveDevice (device);
        }
    }
}
