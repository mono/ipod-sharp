using System;
using System.IO;
using Dmg;

public static class DmgIsoExtract
{
    public static int Main(string [] args)
    {
        string dmg_file = null;
        string iso_file = null;
        string plist_file = null;
        bool list_partitions = false;
    
        if(args.Length < 1) {
            ShowHelp();
            return 1;
        }
        
        for(int i = 0; i < args.Length; i++) {
            switch(args[i]) {
                case "--list-partitions":
                    list_partitions = true;
                    break;
                case "--dump-plist":
                    plist_file = args[++i];
                    break;
                case "--help":
                    ShowHelp();
                    return 1;
                default:
                    if(dmg_file == null) {
                        dmg_file = args[i];
                    } else if(iso_file == null) {
                        iso_file = args[i];
                    } else {
                        Console.Error.WriteLine("Invalid argument: `{0}'", args[0]);
                        return 1;
                    }
                    break;
            }
        }
        
        if(!File.Exists(dmg_file)) {
            Console.Error.WriteLine("DMG file `{0}' does not exist", dmg_file);
            return 1;
        }
        
        FileStream dmg_stream = new FileStream(dmg_file, FileMode.Open);
        Image image = new Image(dmg_stream);
        
        if(list_partitions) {
            foreach(Partition partition in image) {
                Console.WriteLine("ID = {0}, Name = {1}, Attributes = 0x{2:x2}", 
                    partition.ID, partition.Name, partition.AttributesNumeric);
            }
        }
        
        if(plist_file != null) {
            image.SavePartitionsXml(new FileStream(plist_file, FileMode.Create));
        }
        
        if(iso_file != null) {
            image.Extract(new FileStream(iso_file, FileMode.Create));
        }
    
        return 0;
    }
    
    private static void ShowHelp()
    {
        Console.Error.WriteLine("Usage: dmg-iso-extract <dmg-file> [<iso-file>] [--list-partitions]");
        Console.Error.WriteLine("         [--dump-plist <plist-file>]\n");
        Console.Error.WriteLine("   <dmg-file>                 DMG input file to read (required)");
        Console.Error.WriteLine("   <iso-file>                 HFS+ ISO file to output (optional)");
        Console.Error.WriteLine("   --list-partitions          Show the partitions in the DMG image");
        Console.Error.WriteLine("   --dump-plist <plist-file>  Dump the raw plist XML to file\n");
        Console.Error.WriteLine("   --help                     Show this help\n");
    }
}
