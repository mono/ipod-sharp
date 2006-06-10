//
// DmgPartition.cs: Parses a DMG partition from the image 
//   plist XML and can perform data extraction for generating
//   an HFS+ ISO image partition
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
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Dmg
{
    public class Partition
    {
        private enum BlockType : uint
        {
            Copy = 0x00000001,
            Zero = 0x00000002,
            End = 0xffffffff,
            Zlib = 0x80000005,
            Unknown = 0x7ffffffe
        }

        private Hashtable table = new Hashtable();

        private Partition()
        {
        }

        internal Partition(XmlNode node)
        {
            ParsePartition(node);
        }

        private void ParsePartition(XmlNode node)
        {
            foreach(XmlNode key_node in node.SelectNodes("key")) {
                XmlNode value_node = key_node.NextSibling;
                if(value_node == null) {
                    continue;
                }
                
                object value = ParseValue(value_node);
                if(value != null) {
                    table.Add(key_node.InnerText.ToLower(), value);
                }
            }
        }

        private object ParseValue(XmlNode node)
        {
            if(node.Name == "string") {
                return node.InnerText.Trim();
            } else if(node.Name == "data") {
                return Convert.FromBase64String(node.InnerText);
            }

            return null;
        }

        internal void Extract(Stream dmgStream, Stream isoStream, ref long lastOutOffset)
        {
            MemoryStream stream = new MemoryStream(Data);
            BinaryReader reader = new BinaryReader(stream);
            
            byte [] temp = new byte[0x40000];
            byte [] otemp = new byte[0x40000];
            byte [] zeroblock = new byte[4096];
            
            int offset = 0xcc;
            long total_size = 0;
            long block_count = 0;
            
            BlockType block_type = BlockType.Zero;
        
            while(block_type != BlockType.End) {
                stream.Seek(offset, SeekOrigin.Begin);
                block_type = (BlockType)EndianSwap(reader.ReadInt32());
                
                stream.Seek(offset + 12, SeekOrigin.Begin);
                long out_offset = EndianSwap(reader.ReadInt32()) * 0x200;
                
                stream.Seek(offset + 20, SeekOrigin.Begin);
                long out_size = EndianSwap(reader.ReadInt32()) * 0x200;

                stream.Seek(offset + 28, SeekOrigin.Begin);
                long in_offset = EndianSwap(reader.ReadInt32());

                stream.Seek(offset + 36, SeekOrigin.Begin);
                long in_size = EndianSwap(reader.ReadInt32());

                out_offset += lastOutOffset;

                if(block_type == BlockType.Zlib) {
                    dmgStream.Seek(in_offset, SeekOrigin.Begin);
                    if(temp.Length < in_size) {
                        temp = new byte[in_size];
                    }

                    long total_bytes_read = 0;
                    while(total_bytes_read < in_size) {
                        total_bytes_read += dmgStream.Read(temp, 0, 
                            Math.Min((int)(in_size - total_bytes_read), temp.Length));
                    }

                    Inflater inflater = new Inflater();
                    inflater.SetInput(temp, 0, (int)total_bytes_read);

                    if(otemp.Length < out_size + 4) {
                        otemp = new byte[out_size + 4];
                    }

                    inflater.Inflate(otemp);
                    if(inflater.RemainingInput > 0) {
                        throw new ApplicationException("Could not inflate entire block");
                    }

                    isoStream.Write(otemp, 0, (int)out_size);
                } else if(block_type == BlockType.Copy) {
                    dmgStream.Seek(in_offset, SeekOrigin.Begin);
                    
                    int bytes_read = dmgStream.Read(temp, 0, Math.Min((int)in_size, temp.Length));
                    long total_bytes_read = bytes_read;
                    
                    while(bytes_read != -1) {
                        isoStream.Write(temp, 0, bytes_read);
                        if(total_bytes_read >= in_size) {
                            break;
                        }
                        
                        bytes_read = dmgStream.Read(temp, 0, 
                            Math.Min((int)(in_size - total_bytes_read), temp.Length));

                        if(bytes_read > 0) {
                            total_bytes_read += bytes_read;
                        }
                    }
                } else if(block_type == BlockType.Zero) {    
                    long zero_block_count = out_size / zeroblock.Length;
                    
                    for(int i = 0; i < zero_block_count; ++i) {
                        isoStream.Write(zeroblock, 0, zeroblock.Length);
                    }
                    
                    isoStream.Write(zeroblock, 0, (int)(out_size % zeroblock.Length));
                } else if(block_type == BlockType.End) {
                    lastOutOffset = out_offset;
                }

                offset += 0x28;
                block_count++;
                total_size += out_size;
            }
        }

        private static int EndianSwap(int input)
        {
            return IPAddress.HostToNetworkOrder(input);
        }

        public object this[string key] {
            get { return table[key]; }
        }

        public string ID {
            get { return table["id"] as string; }
        }
        
        public string Name {
            get { return table["name"] as string; }
        }

        public string Attributes {
            get { return table["attributes"] as string; }
        }

        public int AttributesNumeric {
            get { 
                if(Attributes.StartsWith("0x")) {
                    return Int32.Parse(Attributes.Substring(2),
                        System.Globalization.NumberStyles.AllowHexSpecifier);
                }

                return 0;
            }
        }

        public byte [] Data {
            get { return table["data"] as byte []; }
        }
    }
}
