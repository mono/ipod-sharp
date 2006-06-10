//
// ExtractFirmware.cs: Extracts iPod firmware from 
//   iPodUpdater DMG image
//
// Authors:
//   Aaron Bockover (aaron@abock.org)
//
// (C) 2006 Aaron Bockover
//

using System;
using IPod.Firmware;

public class FirmwareExtractTest
{
    public static void Main(string [] args)
    {
        DmgFirmwareExtract extract = new DmgFirmwareExtract(args[0], args[1]);
        foreach(string image in extract) {
            Console.WriteLine(image);
        }
    }
}
