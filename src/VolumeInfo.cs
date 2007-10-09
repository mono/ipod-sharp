using System;

namespace IPod
{
    public abstract class VolumeInfo
    {
        private string mount_point;
        private string label;
        private string uuid;
        private bool is_mounted_read_only;
        
        protected VolumeInfo ()
        {
        }
        
        public abstract ulong Size { get; }
        public abstract ulong SpaceUsed { get; }
        
        public ulong SpaceAvailable {
            get { return Size - SpaceUsed; }
        }
        
        public virtual string MountPoint {
            get { return mount_point; }
            protected set { mount_point = value; }
        }
        
        public virtual string Label {
            get { return label; }
            protected set { label = value; }
        }
        
        public virtual string Uuid {
            get { return uuid; }
            protected set { uuid = value; }
        }
        
        public virtual bool IsMountedReadOnly {
            get { return is_mounted_read_only; }
            protected set { is_mounted_read_only = value; }
        }
        
        public void Dump ()
        {
            Console.WriteLine("  Mount Point:       {0}", MountPoint);
            Console.WriteLine("  Mounted Read Only: {0}", IsMountedReadOnly);
            Console.WriteLine("  Label:             {0}", Label);
            Console.WriteLine("  UUID:              {0}", Uuid);
            Console.WriteLine("  Size:              {0}", Size);
            Console.WriteLine("  Space Available:   {0}", SpaceAvailable);
            Console.WriteLine("  Space Used:        {0}", SpaceUsed);
        }
    }
}
