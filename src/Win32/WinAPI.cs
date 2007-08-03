using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace IPod.Win32.WinAPI
{
    [Flags]
    public enum AccessMask : uint
    {
        DELETE = 0x00010000,
        FILE_ADD_FILE = 0x0002,    // directory
        FILE_ADD_SUBDIRECTORY = 0x0004,    // directory
        FILE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF),
        FILE_APPEND_DATA = 0x0004,    // file
        FILE_CREATE_PIPE_INSTANCE = 0x0004,    // named pipe
        FILE_DELETE_CHILD = 0x0040,    // directory
        FILE_EXECUTE = 0x0020,    // file
        FILE_READ_ATTRIBUTES = 0x0080,    // all
        FILE_READ_DATA = 0x0001,    // file & pipe
        FILE_READ_EA = 0x0008,    // file & directory
        FILE_TRAVERSE = 0x0020,    // directory
        FILE_WRITE_ATTRIBUTES = 0x0100,    // all
        FILE_WRITE_DATA = 0x0002,    // file & pipe
        FILE_WRITE_EA = 0x0010,    // file & directory
        FILE_GENERIC_EXECUTE = (STANDARD_RIGHTS_EXECUTE | FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE),
        FILE_GENERIC_READ = (STANDARD_RIGHTS_READ | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE),
        FILE_GENERIC_WRITE = (STANDARD_RIGHTS_WRITE | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | SYNCHRONIZE),
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000,
        GENERIC_READ = 0x80000000,
        READ_CONTROL = 0x00020000,
        SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
        STANDARD_RIGHTS_ALL = 0x001F0000,
        STANDARD_RIGHTS_REQUIRED = 0x000F0000,
        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
        SYNCHRONIZE = 0x00100000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
    }
    [Flags]
    public enum DeviceIOControlCode : uint
    {
        // STORAGE
        StorageBase = DeviceIOFileDevice.MassStorage,
        StorageCheckVerify = (StorageBase << 16) | (0x0200 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageCheckVerify2 = (StorageBase << 16) | (0x0200 << 2) | DeviceIOMethod.Buffered | (0 << 14), // FileAccess.Any
        StorageMediaRemoval = (StorageBase << 16) | (0x0201 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageEjectMedia = (StorageBase << 16) | (0x0202 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageLoadMedia = (StorageBase << 16) | (0x0203 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageLoadMedia2 = (StorageBase << 16) | (0x0203 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StorageReserve = (StorageBase << 16) | (0x0204 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageRelease = (StorageBase << 16) | (0x0205 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageFindNewDevices = (StorageBase << 16) | (0x0206 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageEjectionControl = (StorageBase << 16) | (0x0250 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StorageMcnControl = (StorageBase << 16) | (0x0251 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StorageGetMediaTypes = (StorageBase << 16) | (0x0300 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StorageGetMediaTypesEx = (StorageBase << 16) | (0x0301 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StorageResetBus = (StorageBase << 16) | (0x0400 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageResetDevice = (StorageBase << 16) | (0x0401 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        StorageGetDeviceNumber = (StorageBase << 16) | (0x0420 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StoragePredictFailure = (StorageBase << 16) | (0x0440 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        StorageObsoleteResetBus = (StorageBase << 16) | (0x0400 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        StorageObsoleteResetDevice = (StorageBase << 16) | (0x0401 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        // SCSI
        ControllerBase = DeviceIOFileDevice.Controller,
        ScsiPassThrough = (ControllerBase << 16) | (0x0401 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        ScsiMiniport = (ControllerBase << 16) | (0x0402 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        ScsiGetInquiryData = (ControllerBase << 16) | (0x0403 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        ScsiGetCapabilities = (ControllerBase << 16) | (0x0404 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        ScsiPassThroughDirect = (ControllerBase << 16) | (0x0405 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        ScsiGetAddress = (ControllerBase << 16) | (0x0406 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        ScsiRescanBus = (ControllerBase << 16) | (0x0407 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        ScsiGetDumpPointers = (ControllerBase << 16) | (0x0408 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        ScsiIdePassThrough = (ControllerBase << 16) | (0x040a << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        // DISK
        DiskBase = DeviceIOFileDevice.Disk,
        DiskGetDriveGeometry = (DiskBase << 16) | (0x0000 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskGetPartitionInfo = (DiskBase << 16) | (0x0001 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskSetPartitionInfo = (DiskBase << 16) | (0x0002 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskGetDriveLayout = (DiskBase << 16) | (0x0003 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskSetDriveLayout = (DiskBase << 16) | (0x0004 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskVerify = (DiskBase << 16) | (0x0005 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskFormatTracks = (DiskBase << 16) | (0x0006 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskReassignBlocks = (DiskBase << 16) | (0x0007 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskPerformance = (DiskBase << 16) | (0x0008 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskIsWritable = (DiskBase << 16) | (0x0009 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskLogging = (DiskBase << 16) | (0x000a << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskFormatTracksEx = (DiskBase << 16) | (0x000b << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskHistogramStructure = (DiskBase << 16) | (0x000c << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskHistogramData = (DiskBase << 16) | (0x000d << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskHistogramReset = (DiskBase << 16) | (0x000e << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskRequestStructure = (DiskBase << 16) | (0x000f << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskRequestData = (DiskBase << 16) | (0x0010 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskControllerNumber = (DiskBase << 16) | (0x0011 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskSmartGetVersion = (DiskBase << 16) | (0x0020 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskSmartSendDriveCommand = (DiskBase << 16) | (0x0021 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskSmartRcvDriveData = (DiskBase << 16) | (0x0022 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskUpdateDriveSize = (DiskBase << 16) | (0x0032 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskGrowPartition = (DiskBase << 16) | (0x0034 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskGetCacheInformation = (DiskBase << 16) | (0x0035 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskSetCacheInformation = (DiskBase << 16) | (0x0036 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskDeleteDriveLayout = (DiskBase << 16) | (0x0040 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskFormatDrive = (DiskBase << 16) | (0x00f3 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        DiskSenseDevice = (DiskBase << 16) | (0x00f8 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        DiskCheckVerify = (DiskBase << 16) | (0x0200 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskMediaRemoval = (DiskBase << 16) | (0x0201 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskEjectMedia = (DiskBase << 16) | (0x0202 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskLoadMedia = (DiskBase << 16) | (0x0203 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskReserve = (DiskBase << 16) | (0x0204 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskRelease = (DiskBase << 16) | (0x0205 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskFindNewDevices = (DiskBase << 16) | (0x0206 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        DiskGetMediaTypes = (DiskBase << 16) | (0x0300 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        // CHANGER
        ChangerBase = DeviceIOFileDevice.Changer,
        ChangerGetParameters = (ChangerBase << 16) | (0x0000 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerGetStatus = (ChangerBase << 16) | (0x0001 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerGetProductData = (ChangerBase << 16) | (0x0002 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerSetAccess = (ChangerBase << 16) | (0x0004 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        ChangerGetElementStatus = (ChangerBase << 16) | (0x0005 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        ChangerInitializeElementStatus = (ChangerBase << 16) | (0x0006 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerSetPosition = (ChangerBase << 16) | (0x0007 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerExchangeMedium = (ChangerBase << 16) | (0x0008 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerMoveMedium = (ChangerBase << 16) | (0x0009 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerReinitializeTarget = (ChangerBase << 16) | (0x000A << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        ChangerQueryVolumeTags = (ChangerBase << 16) | (0x000B << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        // FILESYSTEM
        FsctlRequestOplockLevel1 = (DeviceIOFileDevice.FileSystem << 16) | (0 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlRequestOplockLevel2 = (DeviceIOFileDevice.FileSystem << 16) | (1 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlRequestBatchOplock = (DeviceIOFileDevice.FileSystem << 16) | (2 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlOplockBreakAcknowledge = (DeviceIOFileDevice.FileSystem << 16) | (3 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlOpBatchAckClosePending = (DeviceIOFileDevice.FileSystem << 16) | (4 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlOplockBreakNotify = (DeviceIOFileDevice.FileSystem << 16) | (5 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlLockVolume = (DeviceIOFileDevice.FileSystem << 16) | (6 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlUnlockVolume = (DeviceIOFileDevice.FileSystem << 16) | (7 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlDismountVolume = (DeviceIOFileDevice.FileSystem << 16) | (8 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlIsVolumeMounted = (DeviceIOFileDevice.FileSystem << 16) | (10 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlIsPathnameValid = (DeviceIOFileDevice.FileSystem << 16) | (11 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlMarkVolumeDirty = (DeviceIOFileDevice.FileSystem << 16) | (12 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlQueryRetrievalPointers = (DeviceIOFileDevice.FileSystem << 16) | (14 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlGetCompression = (DeviceIOFileDevice.FileSystem << 16) | (15 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlSetCompression = (DeviceIOFileDevice.FileSystem << 16) | (16 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        FsctlMarkAsSystemHive = (DeviceIOFileDevice.FileSystem << 16) | (19 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlOplockBreakAckNo2 = (DeviceIOFileDevice.FileSystem << 16) | (20 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlInvalidateVolumes = (DeviceIOFileDevice.FileSystem << 16) | (21 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlQueryFatBpb = (DeviceIOFileDevice.FileSystem << 16) | (22 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlRequestFilterOplock = (DeviceIOFileDevice.FileSystem << 16) | (23 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlFileSystemGetStatistics = (DeviceIOFileDevice.FileSystem << 16) | (24 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlGetNtfsVolumeData = (DeviceIOFileDevice.FileSystem << 16) | (25 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlGetNtfsFileRecord = (DeviceIOFileDevice.FileSystem << 16) | (26 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlGetVolumeBitmap = (DeviceIOFileDevice.FileSystem << 16) | (27 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlGetRetrievalPointers = (DeviceIOFileDevice.FileSystem << 16) | (28 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlMoveFile = (DeviceIOFileDevice.FileSystem << 16) | (29 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlIsVolumeDirty = (DeviceIOFileDevice.FileSystem << 16) | (30 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlGetHfsInformation = (DeviceIOFileDevice.FileSystem << 16) | (31 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlAllowExtendedDasdIo = (DeviceIOFileDevice.FileSystem << 16) | (32 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlReadPropertyData = (DeviceIOFileDevice.FileSystem << 16) | (33 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlWritePropertyData = (DeviceIOFileDevice.FileSystem << 16) | (34 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlFindFilesBySid = (DeviceIOFileDevice.FileSystem << 16) | (35 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlDumpPropertyData = (DeviceIOFileDevice.FileSystem << 16) | (37 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlSetObjectId = (DeviceIOFileDevice.FileSystem << 16) | (38 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlGetObjectId = (DeviceIOFileDevice.FileSystem << 16) | (39 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlDeleteObjectId = (DeviceIOFileDevice.FileSystem << 16) | (40 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlSetReparsePoint = (DeviceIOFileDevice.FileSystem << 16) | (41 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlGetReparsePoint = (DeviceIOFileDevice.FileSystem << 16) | (42 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlDeleteReparsePoint = (DeviceIOFileDevice.FileSystem << 16) | (43 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlEnumUsnData = (DeviceIOFileDevice.FileSystem << 16) | (44 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlSecurityIdCheck = (DeviceIOFileDevice.FileSystem << 16) | (45 << 2) | DeviceIOMethod.Neither | (FileAccess.Read << 14),
        FsctlReadUsnJournal = (DeviceIOFileDevice.FileSystem << 16) | (46 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlSetObjectIdExtended = (DeviceIOFileDevice.FileSystem << 16) | (47 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlCreateOrGetObjectId = (DeviceIOFileDevice.FileSystem << 16) | (48 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlSetSparse = (DeviceIOFileDevice.FileSystem << 16) | (49 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlSetZeroData = (DeviceIOFileDevice.FileSystem << 16) | (50 << 2) | DeviceIOMethod.Buffered | (FileAccess.Write << 14),
        FsctlQueryAllocatedRanges = (DeviceIOFileDevice.FileSystem << 16) | (51 << 2) | DeviceIOMethod.Neither | (FileAccess.Read << 14),
        FsctlEnableUpgrade = (DeviceIOFileDevice.FileSystem << 16) | (52 << 2) | DeviceIOMethod.Buffered | (FileAccess.Write << 14),
        FsctlSetEncryption = (DeviceIOFileDevice.FileSystem << 16) | (53 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlEncryptionFsctlIo = (DeviceIOFileDevice.FileSystem << 16) | (54 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlWriteRawEncrypted = (DeviceIOFileDevice.FileSystem << 16) | (55 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlReadRawEncrypted = (DeviceIOFileDevice.FileSystem << 16) | (56 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlCreateUsnJournal = (DeviceIOFileDevice.FileSystem << 16) | (57 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlReadFileUsnData = (DeviceIOFileDevice.FileSystem << 16) | (58 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlWriteUsnCloseRecord = (DeviceIOFileDevice.FileSystem << 16) | (59 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlExtendVolume = (DeviceIOFileDevice.FileSystem << 16) | (60 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlQueryUsnJournal = (DeviceIOFileDevice.FileSystem << 16) | (61 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlDeleteUsnJournal = (DeviceIOFileDevice.FileSystem << 16) | (62 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlMarkHandle = (DeviceIOFileDevice.FileSystem << 16) | (63 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlSisCopyFile = (DeviceIOFileDevice.FileSystem << 16) | (64 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        FsctlSisLinkFiles = (DeviceIOFileDevice.FileSystem << 16) | (65 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        FsctlHsmMsg = (DeviceIOFileDevice.FileSystem << 16) | (66 << 2) | DeviceIOMethod.Buffered | ((FileAccess.Read | FileAccess.Write) << 14),
        FsctlNssControl = (DeviceIOFileDevice.FileSystem << 16) | (67 << 2) | DeviceIOMethod.Buffered | (FileAccess.Write << 14),
        FsctlHsmData = (DeviceIOFileDevice.FileSystem << 16) | (68 << 2) | DeviceIOMethod.Neither | ((FileAccess.Read | FileAccess.Write) << 14),
        FsctlRecallFile = (DeviceIOFileDevice.FileSystem << 16) | (69 << 2) | DeviceIOMethod.Neither | (0 << 14),
        FsctlNssRcontrol = (DeviceIOFileDevice.FileSystem << 16) | (70 << 2) | DeviceIOMethod.Buffered | (FileAccess.Read << 14),
        // VIDEO
        VideoQuerySupportedBrightness = (DeviceIOFileDevice.Video << 16) | (0x0125 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        VideoQueryDisplayBrightness = (DeviceIOFileDevice.Video << 16) | (0x0126 << 2) | DeviceIOMethod.Buffered | (0 << 14),
        VideoSetDisplayBrightness = (DeviceIOFileDevice.Video << 16) | (0x0127 << 2) | DeviceIOMethod.Buffered | (0 << 14)
    }
    [Flags]
    public enum DeviceIOFileDevice : uint
    {
        Beep = 0x00000001,
        CDRom = 0x00000002,
        CDRomFileSytem = 0x00000003,
        Controller = 0x00000004,
        Datalink = 0x00000005,
        Dfs = 0x00000006,
        Disk = 0x00000007,
        DiskFileSystem = 0x00000008,
        FileSystem = 0x00000009,
        InPortPort = 0x0000000a,
        Keyboard = 0x0000000b,
        Mailslot = 0x0000000c,
        MidiIn = 0x0000000d,
        MidiOut = 0x0000000e,
        Mouse = 0x0000000f,
        MultiUncProvider = 0x00000010,
        NamedPipe = 0x00000011,
        Network = 0x00000012,
        NetworkBrowser = 0x00000013,
        NetworkFileSystem = 0x00000014,
        Null = 0x00000015,
        ParellelPort = 0x00000016,
        PhysicalNetcard = 0x00000017,
        Printer = 0x00000018,
        Scanner = 0x00000019,
        SerialMousePort = 0x0000001a,
        SerialPort = 0x0000001b,
        Screen = 0x0000001c,
        Sound = 0x0000001d,
        Streams = 0x0000001e,
        Tape = 0x0000001f,
        TapeFileSystem = 0x00000020,
        Transport = 0x00000021,
        Unknown = 0x00000022,
        Video = 0x00000023,
        VirtualDisk = 0x00000024,
        WaveIn = 0x00000025,
        WaveOut = 0x00000026,
        Port8042 = 0x00000027,
        NetworkRedirector = 0x00000028,
        Battery = 0x00000029,
        BusExtender = 0x0000002a,
        Modem = 0x0000002b,
        Vdm = 0x0000002c,
        MassStorage = 0x0000002d,
        Smb = 0x0000002e,
        Ks = 0x0000002f,
        Changer = 0x00000030,
        Smartcard = 0x00000031,
        Acpi = 0x00000032,
        Dvd = 0x00000033,
        FullscreenVideo = 0x00000034,
        DfsFileSystem = 0x00000035,
        DfsVolume = 0x00000036,
        Serenum = 0x00000037,
        Termsrv = 0x00000038,
        Ksec = 0x00000039
    }
    [Flags]
    public enum DeviceIOMethod : uint
    {
        Buffered = 0,
        InDirect = 1,
        OutDirect = 2,
        Neither = 3
    }
    public enum DeviceType
    {
        DBT_DEVTYP_DEVICEINTERFACE = 5,
        DBT_DEVTYP_DEVNODE = 1,
        DBT_DEVTYP_HANDLE = 6,
        DBT_DEVTYP_NET = 4,
        DBT_DEVTYP_OEM = 0,
        DBT_DEVTYP_PORT = 3,
        DBT_DEVTYP_VOLUME = 2,
    }
    [Flags]
    public enum FileAttributesAndFlags : uint
    {
        FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
        FILE_ATTRIBUTE_COMPRESSED = 0x00000800,
        FILE_ATTRIBUTE_DEVICE = 0x00000040,
        FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
        FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
        FILE_ATTRIBUTE_HIDDEN = 0x00000002,
        FILE_ATTRIBUTE_NORMAL = 0x00000080,
        FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
        FILE_ATTRIBUTE_OFFLINE = 0x00001000,
        FILE_ATTRIBUTE_READONLY = 0x00000001,
        FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400,
        FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200,
        FILE_ATTRIBUTE_SYSTEM = 0x00000004,
        FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
        FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
        FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
        FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000,
        FILE_FLAG_NO_BUFFERING = 0x20000000,
        FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
        FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
        FILE_FLAG_OVERLAPPED = 0x40000000,
        FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
        FILE_FLAG_RANDOM_ACCESS = 0x10000000,
        FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
        FILE_FLAG_WRITE_THROUGH = 0x80000000,
    }
    public enum VolumeType
    {
        DBTF_MEDIA = 1,
        DBTF_NET = 2,
    }
    public enum WmDeviceChangeEvent
    {
        DBT_CONFIGCHANGECANCELED = 0x0019,
        DBT_CONFIGCHANGED = 0x0018,
        DBT_CUSTOMEVENT = 0x8006,
        DBT_DEVICEARRIVAL = 0x8000,
        DBT_DEVICEQUERYREMOVE = 0x8001,
        DBT_DEVICEQUERYREMOVEFAILED = 0x8002,
        DBT_DEVICEREMOVEPENDING = 0x8003,
        DBT_DEVICEREMOVECOMPLETE = 0x8004,
        DBT_DEVICETYPESPECIFIC = 0x8005,
        DBT_DEVNODES_CHANGED = 0x0007,
        DBT_QUERYCHANGECONFIG = 0x0017,
        DBT_USERDEFINED = 0xFFFF,
    }

    public struct DEV_BROADCAST_HDR
    {
        public int dbch_size;
        public DeviceType dbch_devicetype;
        private int dbch_reserved;
    }
    public struct DEV_BROADCAST_VOLUME
    {
        public int dbcv_size;
        public DeviceType dbcv_devicetype;
        private int dbcv_reserved;
        public int dbcv_unitmask;
        public VolumeType dbcv_flags;
    }

    public struct SCSI_PASS_THROUGH
    {
        public ushort Length;
        public byte ScsiStatus;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte CdbLength;
        public byte SenseInfoLength;
        public byte DataIn;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public IntPtr DataBufferOffset;
        public IntPtr SenseInfoOffset;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 16)]
        public byte [] Cdb;
    }
    public struct STORAGE_DEVICE_NUMBER
    {
        public DeviceIOFileDevice DeviceType;
        public uint DeviceNumber;
        public uint PartitionNumber;
    }

    public static class ApiFunctions
    {
        [DllImport ("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern SafeFileHandle CreateFile (string lpFileName, AccessMask dwDesiredAccess, 
            System.IO.FileShare dwShareMode, uint SecurityAttributes, System.IO.FileMode dwCreationDisposition, FileAttributesAndFlags dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport ("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl (SafeFileHandle hDevice, DeviceIOControlCode dwIoControlCode, 
            IntPtr InBuffer, int nInBufferSize, IntPtr OutBuffer, int nOutBufferSize, out int pBytesReturned, [In] ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport ("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool FlushFileBuffers (SafeFileHandle hFile);
    }
}
