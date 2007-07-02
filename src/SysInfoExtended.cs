using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using IPod.Win32.WinAPI;

namespace IPod
{
    internal class SysInfoExtended
    {
        string serialNumber;
        private Dictionary<int, ArtworkFormat> formats = new Dictionary<int, ArtworkFormat> ();

        public string SerialNumber { get { return serialNumber; } }
        public Dictionary<int, ArtworkFormat> ArtworkFormats { get { return formats; } }

        public SysInfoExtended (IDevice Ipod)
        {
            byte [] buffer = null;

            if (Environment.OSVersion.Platform != PlatformID.Unix) //Windows
                buffer = GetWin32XML (Ipod);
            else {
                //TODO: write Unix code to pull buffer
            }

            if (buffer == null)
                throw new FileLoadException ("SCSI_INQUIRY returned null, older iPod?");

            string XmlStr = Encoding.UTF8.GetString (buffer);
            XmlDocument sysInfoXml = new XmlDocument ();
            sysInfoXml.LoadXml (XmlStr);

            XmlNode serialNode = sysInfoXml.SelectSingleNode ("/plist/dict/key[text()='SerialNumber']/following-sibling::*[1]");
            serialNumber = serialNode.InnerText;

            XmlNodeList photoNodes = sysInfoXml.SelectNodes ("/plist/dict/key[text()='ImageSpecifications']/following-sibling::*[1]//dict");
            if (photoNodes != null)
                ParseArtwork (photoNodes, ArtworkUsage.Photo);

            XmlNodeList coverNodes = sysInfoXml.SelectNodes ("/plist/dict/key[text()='AlbumArt']/following-sibling::*[1]//dict");
            if (coverNodes != null)
                ParseArtwork (coverNodes, ArtworkUsage.Photo);
        }

        private void ParseArtwork (XmlNodeList photoNodes, ArtworkUsage artworkUsage)
        {
            foreach (XmlNode formatNode in photoNodes) {
                ArtworkUsage usage = artworkUsage;
                short width = 0;
                short height = 0;
                short correlationId = 0;
                int size = 0;
                PixelFormat pformat = PixelFormat.Unknown;
                short rotation = 0;

                string key = null;
                for (XmlNode child = formatNode.FirstChild; child != null; child = child.NextSibling) {
                    if (child.Name == "key") {
                        key = child.InnerText;
                        continue;
                    }

                    if (key != null)
                        switch (key) {
                        case "FormatId":
                            correlationId = short.Parse (child.InnerText);
                            break;
                        case "RenderWidth":
                            width = short.Parse (child.InnerText);
                            break;
                        case "RenderHeight":
                            height = short.Parse (child.InnerText);
                            break;
                        case "PixelFormat":
                            switch (child.InnerText) {
                            case "4C353635":
                                pformat = PixelFormat.Rgb565;
                                break;
                            case "42353635":
                                pformat = PixelFormat.Rgb565BE;
                                break;
                            case "32767579":
                                pformat = PixelFormat.IYUV;
                                break;
                            default:
                                pformat = PixelFormat.Unknown;
                                break;
                            }
                            break;
                        case "Rotation":
                            rotation = short.Parse (child.InnerText);
                            break;
                        }
                }

                size = width * height * 2;

                if (!(correlationId == 0 || width == 0 || height == 0 || size == 0))
                    formats.Add (correlationId, new ArtworkFormat (usage, width, height, correlationId, size, pformat, rotation));
            }
        }

        #region Win32

        [StructLayout (LayoutKind.Sequential)]
        struct ScsiPassThroughWithBuffers
        {
            public SCSI_PASS_THROUGH spt;
            public UInt32 Filler;
            [MarshalAs (UnmanagedType.ByValArray, SizeConst = 32)]
            public byte [] ucSenseBuf;
            [MarshalAs (UnmanagedType.ByValArray, SizeConst = 255)]
            public byte [] ucDataBuf;
        }

        private byte [] GetWin32XML (IDevice Ipod)
        {
            IntPtr pSptwb = IntPtr.Zero;
            SafeFileHandle driveHandle = null;

            try {
                ScsiPassThroughWithBuffers sptwb;
                int returned, length;
                string device = GetDeviceFromDrive (((Win32.Device)Ipod).Drive);
                bool status;

                byte [] buffer = new byte [102400];
                int bufferIdx = 0;

                byte page_code, page_start, page_end = 0;

                driveHandle = ApiFunctions.CreateFile (device, AccessMask.GENERIC_READ, 
                    System.IO.FileShare.ReadWrite, 0, System.IO.FileMode.Open, 0, IntPtr.Zero);

                if (driveHandle.IsInvalid)
                    throw new FileLoadException ("Device invalid");

                sptwb = new ScsiPassThroughWithBuffers ();
                sptwb.spt.Length = (ushort)Marshal.SizeOf (typeof (SCSI_PASS_THROUGH));
                sptwb.spt.PathId = 0;
                sptwb.spt.TargetId = 1;
                sptwb.spt.Lun = 0;
                sptwb.spt.CdbLength = 6;
                sptwb.spt.SenseInfoLength = 32;
                sptwb.spt.DataIn = 1; //SCSI_IOCTL_DATA_IN
                sptwb.spt.DataTransferLength = 255;
                sptwb.spt.TimeOutValue = 2;
                sptwb.spt.DataBufferOffset = Marshal.OffsetOf (typeof (ScsiPassThroughWithBuffers), "ucDataBuf");
                sptwb.spt.SenseInfoOffset = Marshal.OffsetOf (typeof (ScsiPassThroughWithBuffers), "ucSenseBuf");
                sptwb.spt.Cdb = new byte [16];
                sptwb.spt.Cdb [0] = 0x12; //SCSI_INQUIRY
                sptwb.spt.Cdb [1] |= 1;
                sptwb.spt.Cdb [2] = 0xC0;
                sptwb.spt.Cdb [4] = 255;

                length = Marshal.OffsetOf (typeof (ScsiPassThroughWithBuffers), "ucDataBuf").ToInt32 () 
                    + (int)sptwb.spt.DataTransferLength;

                pSptwb = Marshal.AllocHGlobal (Marshal.SizeOf (sptwb));
                Marshal.StructureToPtr (sptwb, pSptwb, true);

                System.Threading.NativeOverlapped nativeOverlapped = new System.Threading.NativeOverlapped ();
                status = ApiFunctions.DeviceIoControl (driveHandle, DeviceIOControlCode.ScsiPassThrough, 
                    pSptwb, Marshal.SizeOf (typeof (SCSI_PASS_THROUGH)), pSptwb, length, out returned, ref nativeOverlapped);

                if (!status)
                    throw new FileLoadException ("DeviceIoControl Error");

                sptwb = (ScsiPassThroughWithBuffers)Marshal.PtrToStructure (pSptwb, typeof (ScsiPassThroughWithBuffers));

                page_start = sptwb.ucDataBuf [4];
                page_end = sptwb.ucDataBuf [3 + sptwb.ucDataBuf [3]];

                for (page_code = page_start; page_code <= page_end; page_code++) {
                    sptwb.spt.Length = (ushort)Marshal.SizeOf (typeof (SCSI_PASS_THROUGH));
                    sptwb.spt.PathId = 0;
                    sptwb.spt.TargetId = 1;
                    sptwb.spt.Lun = 0;
                    sptwb.spt.CdbLength = 6;
                    sptwb.spt.SenseInfoLength = 32;
                    sptwb.spt.DataIn = 1; //SCSI_IOCTL_DATA_IN
                    sptwb.spt.DataTransferLength = 255;
                    sptwb.spt.TimeOutValue = 2;
                    sptwb.spt.DataBufferOffset = Marshal.OffsetOf (typeof (ScsiPassThroughWithBuffers), "ucDataBuf");
                    sptwb.spt.SenseInfoOffset = Marshal.OffsetOf (typeof (ScsiPassThroughWithBuffers), "ucSenseBuf");
                    sptwb.spt.Cdb = new byte [16];
                    sptwb.spt.Cdb [0] = 0x12; //SCSI_INQUIRY
                    sptwb.spt.Cdb [1] |= 1;
                    sptwb.spt.Cdb [2] = page_code;
                    sptwb.spt.Cdb [4] = 255;

                    length = Marshal.OffsetOf (typeof (ScsiPassThroughWithBuffers), "ucDataBuf").ToInt32 () 
                        + (int)sptwb.spt.DataTransferLength;
                    returned = 0;
                    Marshal.StructureToPtr (sptwb, pSptwb, true);
                    status = ApiFunctions.DeviceIoControl (driveHandle, DeviceIOControlCode.ScsiPassThrough, 
                        pSptwb, Marshal.SizeOf (typeof (SCSI_PASS_THROUGH)), pSptwb, length, out returned, ref nativeOverlapped);

                    if (!status)
                        throw new FileLoadException ("DeviceIoControl Error");

                    sptwb = (ScsiPassThroughWithBuffers)Marshal.PtrToStructure (pSptwb, typeof (ScsiPassThroughWithBuffers));

                    while (bufferIdx + sptwb.ucDataBuf [3] > buffer.Length)
                        Array.Resize<byte> (ref buffer, buffer.Length * 2);

                    Array.Copy (sptwb.ucDataBuf, 4, buffer, bufferIdx, sptwb.ucDataBuf [3]);
                    bufferIdx += sptwb.ucDataBuf [3];
                }

                if (bufferIdx == 0)
                    return null;

                buffer [bufferIdx] = 0;

                Array.Resize<byte> (ref buffer, bufferIdx + 1);

                return buffer;
            }
            finally {
                if (pSptwb != IntPtr.Zero)
                    Marshal.FreeHGlobal (pSptwb);

                if (driveHandle != null)
                    driveHandle.Close ();
            }
        }
        public static string GetDeviceFromDrive (System.IO.DriveInfo driveInfo)
        {
            IntPtr pStorageDeviceNumber = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (STORAGE_DEVICE_NUMBER)));
            SafeFileHandle hDrive = null;
            try {
                string driveName = "\\\\.\\" + driveInfo.ToString ().Substring (0, 2);
                hDrive = ApiFunctions.CreateFile (driveName, AccessMask.GENERIC_READ, 
                    System.IO.FileShare.ReadWrite, 0, System.IO.FileMode.Open, 0, IntPtr.Zero);

                if (hDrive.IsInvalid)
                    throw new FileLoadException ("Drive handle invalid");

                bool status;
                int retByte;
                System.Threading.NativeOverlapped nativeOverlap = new System.Threading.NativeOverlapped ();
                status = ApiFunctions.DeviceIoControl (hDrive, DeviceIOControlCode.StorageGetDeviceNumber, IntPtr.Zero, 0,
                    pStorageDeviceNumber, Marshal.SizeOf (typeof (STORAGE_DEVICE_NUMBER)), out retByte, ref nativeOverlap);

                if (!status)
                    throw new FileLoadException ("DeviceIoControl error");

                STORAGE_DEVICE_NUMBER storDevNum = (STORAGE_DEVICE_NUMBER)Marshal.PtrToStructure (pStorageDeviceNumber, typeof (STORAGE_DEVICE_NUMBER));

                return "\\\\.\\PhysicalDrive" + storDevNum.DeviceNumber;
            }
            finally {
                Marshal.FreeHGlobal (pStorageDeviceNumber);
                if (hDrive != null)
                    hDrive.Close ();
            }
        }

        #endregion
    }
}
