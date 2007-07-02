using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using IPod.Win32.WinAPI;

namespace IPod.Win32
{
    internal class Device : IDevice
    {
        #region Instance Variables

        private Dictionary<int, ArtworkFormat> formats = new Dictionary<int, ArtworkFormat> ();
        private DriveInfo drive;
        private SysInfo sysinfo;
        private string control_path;
        private string mount_point;
        private string serial_number;
        private string device_path;
        private IPod.Device host;

        #endregion

        public Device (string driveOrMountPoint)
        {
            drive = new DriveInfo (driveOrMountPoint);
            control_path = driveOrMountPoint.ToString () + "iPod_Control\\";
            mount_point = driveOrMountPoint.ToString ().Substring (0, 2);

            if (!Directory.Exists (control_path)) {
                control_path = driveOrMountPoint.ToString () + "iTunes_Control\\";

                if (!Directory.Exists (control_path))
                    throw new DirectoryNotFoundException ("iPod_Control directory does not exist on the selected Drive");
            }

            RescanDisk ();
        }

        #region Public Properties

        public IPod.Device Host
        {
            get { return host; }
            set { host = value; }
        }

        public Dictionary<int, ArtworkFormat> ArtworkFormats
        {
            get { return formats; }
        }

        public DriveInfo Drive
        {
            get { return drive; }
        }

        public string ControlPath
        {
            get { return control_path; }
        }

        public string MountPoint
        {
            get { return mount_point; }
        }

        public string DevicePath
        {
            get { return device_path; }
        }

        /// <summary>
        /// Always returns true with this implementation because constructor checks for it
        /// </summary>
        public bool IsIPod
        {
            get { return true; }
        }

        public DeviceModel Model
        {
            get { return sysinfo.DeviceModel; }
        }

        public DeviceGeneration Generation
        {
            get { return sysinfo.DeviceGeneration; }
        }

        public string ModelNumber
        {
            get { return sysinfo.ModelString; }
        }

        public string ModelString
        {
            get { return sysinfo.ModelString; }
        }

        public string SerialNumber
        {
            get { return serial_number; }
        }

        public ulong VolumeSize
        {
            get { return (ulong)drive.TotalSize; }
        }

        public ulong VolumeUsed
        {
            get { return (ulong)(drive.TotalSize - drive.TotalFreeSpace); }
        }

        public ulong VolumeAvailable
        {
            get { return (ulong)drive.TotalFreeSpace; }
        }

        public string VolumeLabel
        {
            get { return drive.VolumeLabel; }
        }

        #endregion

        #region Public Functions

        public void Eject ()
        {
            string driveName = "\\\\.\\" + drive.ToString ().Substring (0, 2);
            SafeFileHandle hDrive = ApiFunctions.CreateFile (driveName,
                AccessMask.GENERIC_READ, System.IO.FileShare.ReadWrite, 0,
                System.IO.FileMode.Open, 0, IntPtr.Zero);

            if (hDrive.IsInvalid)
                throw new DeviceException (host, "Failed to eject device, could not open drive");

            int retByte;
            NativeOverlapped nativeOverlap = new NativeOverlapped ();

            bool status = ApiFunctions.DeviceIoControl (hDrive, DeviceIOControlCode.StorageEjectMedia,
                IntPtr.Zero, 0, IntPtr.Zero, 0, out retByte, ref nativeOverlap);

            if (!status)
                throw new DeviceException (host, "Failed to eject device, DeviceIoControl returned false");
        }

        public void RescanDisk ()
        {
            sysinfo = new SysInfo (this);

            this.device_path = SysInfoExtended.GetDeviceFromDrive (drive);

            try {
                SysInfoExtended sysinfoExtended = new SysInfoExtended (this);
                this.formats = sysinfoExtended.ArtworkFormats;
                this.serial_number = sysinfoExtended.SerialNumber;
            }
            catch (FileLoadException) {
                this.serial_number = sysinfo.SerialNumber;
            }
        }

        public void Save ()
        {
            //Intentionally left blank (nothing platform specific to do...)
        }

        #endregion

        public static Device [] ListDevices ()
        {
            List<Device> iPodList = new List<Device> ();

            foreach (string drive in Environment.GetLogicalDrives ()) {
                if (Directory.Exists (drive + "iPod_Control"))
                    iPodList.Add (new Device (drive));
            }

            return iPodList.ToArray ();
        }

        #region Unimplemented IDevice Members

        public string UserName
        {
            get { return String.Empty; }
            set { }
        }

        public string HostName
        {
            get { return String.Empty; }
            set { }
        }

        public string VolumeId
        {
            get { return String.Empty; }
        }

        public string AdvertisedCapacity
        {
            get { return String.Empty; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public bool IsNew
        {
            get { return false; }
        }

        public string FirmwareVersion
        {
            get { return String.Empty; }
        }

        public string VolumeUuid
        {
            get { return String.Empty; }
        }

        public string ManufacturerId
        {
            get { return String.Empty; }
        }

        public uint ProductionYear
        {
            get { return 0; }
        }

        public uint ProductionWeek
        {
            get { return 0; }
        }

        public uint ProductionIndex
        {
            get { return 0; }
        }

        #endregion

        public static explicit operator IPod.Win32.Device (IPod.Device dev)
        {
            Win32.Device wDevice = dev.platformDevice as Win32.Device;

            if (wDevice == null)
                throw new ArgumentException ("Device is not a Win32.Device");

            return wDevice;
        }
    }
}
