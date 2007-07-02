using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using IPod.Win32.WinAPI;

namespace IPod.Win32
{
    internal class DeviceWatcherWindow : NativeWindow, IDisposable
    {
        const int WM_DEVICECHANGE = 0x219;

        public event EventHandler<DeviceEventArgs> DeviceArrived;
        public event EventHandler<DeviceEventArgs> DeviceRemoved;

        public DeviceWatcherWindow(string Caption)
        {
            CreateParams cp = new CreateParams();
            cp.Caption = Caption;

            this.CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)WM_DEVICECHANGE)
            {
                if (m.LParam != IntPtr.Zero)
                {
                    DEV_BROADCAST_HDR BroadcastHdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));
                    if (BroadcastHdr.dbch_devicetype == DeviceType.DBT_DEVTYP_VOLUME)
                    {
                        DEV_BROADCAST_VOLUME VolumeHdr = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
                        if (VolumeHdr.dbcv_flags != VolumeType.DBTF_NET)
                        {
                            switch ((WmDeviceChangeEvent)m.WParam.ToInt32())
                            {
                                case WmDeviceChangeEvent.DBT_DEVICEARRIVAL:
                                    if (DeviceArrived != null)
                                        DeviceArrived(this, new DeviceEventArgs(VolumeHdr.dbcv_unitmask));
                                    break;
                                case WmDeviceChangeEvent.DBT_DEVICEREMOVECOMPLETE:
                                    if (DeviceRemoved != null)
                                        DeviceRemoved(this, new DeviceEventArgs(VolumeHdr.dbcv_unitmask));
                                    break;
                            }
                        }
                    }
                }
            }

            base.WndProc(ref m);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
                this.ReleaseHandle();
        }

        ~DeviceWatcherWindow ()
        {
            Dispose(false);
        }

        #endregion
    }

    internal class DeviceEventArgs : EventArgs
    {
        List<char> drives;

        public List<char> Drives { get { return drives; } }

        public DeviceEventArgs(int BroadcastVolMask)
        {
            drives = ConvertMaskToChars(BroadcastVolMask);
        }

        private List<char> ConvertMaskToChars(int BroadcastVolMask)
        {
            int lValue = 0;
            List<char> drives = new List<char>();

            if (BroadcastVolMask > 0)
            {
                for (; BroadcastVolMask != 0; BroadcastVolMask >>= 1)
                {
                    if ((BroadcastVolMask & 1) != 0)
                    {
                        drives.Add((char)(65 + lValue));
                    }
                    lValue++;
                }
            }
            return drives;
        }
    }
}
