//
// DmgImage.cs: Parses DMG partitions from a DMG image
//   to build an HFS+ ISO image suitable for mounting
//
// Authors:
//   Aaron Bockover (aaron@abock.org)
//
// (C) 2006 Aaron Bockover
//

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Collections;

namespace Dmg
{
    public class Image : IEnumerable
    {
        private static readonly long PLIST_START = 0x1e0;
        private static readonly long PLIST_END = 0x128;

        private Stream dmg_stream;
        private BinaryReader dmg_reader;
        private XmlDocument plist_doc;

        private ArrayList partitions = new ArrayList();

        public Image(string fileName) : this(new FileStream(fileName, FileMode.Open))
        {
        }

        public Image(Stream dmgStream)
        {
            dmg_stream = dmgStream;
            dmg_reader = new BinaryReader(dmg_stream);

            ReadImage();
        }

        public void SavePartitionsXml(Stream stream)
        {
            plist_doc.Save(stream);
        }
        
        public void Extract(string fileName)
        {
            Extract(new FileStream(fileName, FileMode.Create));
        }

        public void Extract(Stream isoStream)
        {
            long out_offset = 0;
            foreach(Partition partition in partitions) {
                partition.Extract(dmg_stream, isoStream, ref out_offset);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return partitions.GetEnumerator();
        }

        private void ReadImage()
        {
            long plist_address = LocateVerifyPlist();
            ExtractPlistXmlDocument(plist_address);
            XmlNode partitions_node = LocatePartitionsNode(plist_doc);

            foreach(XmlNode partition_node in partitions_node) {
                partitions.Add(new Partition(partition_node));
            }
        }

        private long LocateVerifyPlist()
        {
            dmg_stream.Seek(-PLIST_START, SeekOrigin.End);
            long address = dmg_reader.ReadInt64();
            dmg_stream.Seek(-PLIST_END, SeekOrigin.End);
            if(dmg_reader.ReadInt64() != address) {
                throw new IOException("Could not verify plist addresses");
            }

            return IPAddress.HostToNetworkOrder(address);
        }

        private void ExtractPlistXmlDocument(long address)
        {
            dmg_stream.Seek(address, SeekOrigin.Begin);
            byte [] tail_bytes = dmg_reader.ReadBytes((int)(dmg_stream.Length - address));
            
            string raw_xml = Encoding.UTF8.GetString(tail_bytes);
            raw_xml = raw_xml.Substring(0, raw_xml.LastIndexOf("</plist>") + 8);
            
            plist_doc = new XmlDocument();
            plist_doc.LoadXml(raw_xml);    
        }

        private XmlNode LocatePartitionsNode(XmlDocument plistDoc)
        {
            foreach(XmlNode key_node in plistDoc.SelectNodes("/plist/dict/key")) {
                if(key_node.InnerText != "resource-fork") {
                    continue;
                }

                XmlNode dict_node = key_node.NextSibling;
            
                foreach(XmlNode node in dict_node.ChildNodes) {
                    if(node.Name == "key" && node.InnerText == "blkx") {
                        return node.NextSibling;
                    }
                }
        
                break;
            }
            
            throw new ApplicationException("Could not read partition table from plist");
        }
    }
}
