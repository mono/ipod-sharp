//
// DmgFirmwareExtract.cs: Extracts iPod firmware from 
//   iPodUpdater DMG image
//
// Authors:
//   Aaron Bockover (aaron@abock.org)
//
// (C) 2006 Aaron Bockover
//

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;

using Dmg;
using ICSharpCode.SharpZipLib.GZip;

namespace IPod.Firmware
{
    public class DmgFirmwareExtract : IDisposable, IEnumerable
    {
        private string work_base;
        private string dmg_mount;
        private string iso_path;
        private string firmware_export_path;
        private bool should_unmount = false;
        private ArrayList images = new ArrayList();
        
        public DmgFirmwareExtract(string dmgFile, string extractPath)
        {
            firmware_export_path = extractPath;
            
            CreateWorkArea();
            
            try {
                ExtractDmgImage(dmgFile);
                MountIsoImage();
                ExtractFirmwareArchive();
                ExtractLicenseAgreements();
            } finally {
                UnmountIsoImage();
                File.Delete(iso_path);
                Directory.Delete(dmg_mount);
            }
        }
        
        private void CreateWorkArea()
        {
            work_base = Path.DirectorySeparatorChar + Path.Combine("tmp", "ipod-update");
            dmg_mount = Path.Combine(work_base, "dmg-mount");
            iso_path = Path.Combine(work_base, "updater.iso");
        
            Directory.CreateDirectory(work_base);
            Directory.CreateDirectory(dmg_mount);
        }
        
        public void Dispose()
        {
            if(Directory.Exists(work_base)) {
                Directory.Delete(work_base, true);
            }
        }
        
        public IEnumerator GetEnumerator()
        {
            return images.GetEnumerator();
        }
        
        private void ExtractDmgImage(string dmgFile)
        {
            Image image = new Image(dmgFile);
            image.Extract(iso_path);
        }
        
        private void MountIsoImage()
        {
            ExecProcess("mount", String.Format("-t hfsplus -o loop,ro {0} {1}",
                iso_path, dmg_mount));
            should_unmount = true;
        }
        
        private void UnmountIsoImage()
        {
            if(should_unmount) {
                ExecProcess("umount", dmg_mount);
                should_unmount = false;
            }
        }
        
        private void ExtractLicenseAgreements()
        {
            foreach(DirectoryInfo directory in new DirectoryInfo(ResourcesPath).GetDirectories("*.lproj")) {
                string license_extract_name = String.Format("{0}.rtf", 
                    directory.Name.Substring(0, directory.Name.Length - 6));
                File.Copy(Path.Combine(directory.FullName, "License.rtf"), 
                    Path.Combine(firmware_export_path, license_extract_name));
            }
        }
        
        private void ExtractFirmwareArchive()
        {
            string ipod_package = null;
            
            foreach(DirectoryInfo directory in new DirectoryInfo(ResourcesPath).GetDirectories("iPod*.pkg")) {
                ipod_package = directory.FullName;
                break;
            }
            
            string cpio_out = Path.Combine(work_base, "firmware.cpio");
            string gzcpio_in = Path.Combine(ipod_package, 
                "Contents" + Path.DirectorySeparatorChar + "Archive.pax.gz");
            
            // this sucks, it seems necessary to create an extracted CPIO archive
            // because GZipInputStream can't do seeking... CPIO needs seeking
            
            FileStream in_stream = new FileStream(gzcpio_in, FileMode.Open, FileAccess.Read);
            FileStream out_stream = new FileStream(cpio_out, FileMode.Create);
            GZipInputStream gzstream = new GZipInputStream(in_stream);
            
            byte [] block = new byte[4096];
            int size = block.Length;
            
            while(true) {
                size = gzstream.Read(block, 0, size);
                if(size > 0) {
                    out_stream.Write(block, 0, size);
                } else {
                    break;
                }
            }
            
            in_stream.Close();
            out_stream.Close();
            
            CpioArchive archive = new CpioArchive(cpio_out);
            foreach(CpioFileEntry entry in archive) {
                string filename = Path.GetFileName(entry.FileName);
                if(filename.StartsWith("Firmware-")) {
                    string image_path = Path.Combine(firmware_export_path, filename);
                    archive.ExtractEntry(entry, image_path);
                    images.Add(image_path);
                }
            }
            
            File.Delete(cpio_out);
        } 
        
        private static void ExecProcess(string proc, string args)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo();
            process.StartInfo.FileName = proc;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            
            process.Start();
            process.WaitForExit();
            
            if(process.ExitCode != 0) {
                string message = process.StandardError.ReadToEnd().Trim();
                throw new ApplicationException(message);
            }
        }
        
        public string ResourcesPath { 
            get { 
                return Path.Combine(dmg_mount, String.Format("iPod.mpkg{0}Contents{0}Resources", 
                    Path.DirectorySeparatorChar));
            }
        }
    }
}
