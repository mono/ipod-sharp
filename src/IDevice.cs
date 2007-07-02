using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace IPod
{
    internal interface IDevice
    {
        IPod.Device Host { get;set;}
        Dictionary<int, ArtworkFormat> ArtworkFormats { get;}
        DeviceModel Model { get;}
        string ModelString { get;}
        DeviceGeneration Generation { get;}
        string ControlPath { get;}
        string DevicePath { get;}
        string MountPoint { get;}
        string UserName { get;set;}
        string HostName { get;set;}
        string VolumeId { get;}
        string AdvertisedCapacity { get;}
        UInt64 VolumeSize { get;}
        UInt64 VolumeUsed { get;}
        UInt64 VolumeAvailable { get;}
        bool IsIPod { get;}
        bool CanWrite { get;}
        bool IsNew { get;}
        string SerialNumber { get;}
        string ModelNumber { get;}
        string FirmwareVersion { get;}
        string VolumeUuid { get;}
        string VolumeLabel { get;}
        string ManufacturerId { get;}
        uint ProductionYear { get;}
        uint ProductionWeek { get;}
        uint ProductionIndex { get;}

        void RescanDisk ();
        void Eject ();
        void Save ();
    }
}
