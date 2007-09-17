using System;
using System.Collections;
using System.Collections.Generic;

using NDesk.DBus;

namespace IPod.Hal
{
    [Interface("org.freedesktop.Hal.Device.Volume")]
    internal interface IVolumeIPod
    {
        void Mount(string [] args);
        void Unmount(string [] args);
        void Eject(string [] args);
    }
    
    internal class Volume : Device
    {
        public Volume(string udi) : base(udi)
        {
        }

        public void Mount()
        {
            Mount(new string [] { String.Empty });
        }
        
        public void Mount(params string [] args)
        {
            CastDevice<IVolumeIPod>().Mount(args);
        }
        
        public void Unmount()
        {
            Unmount(new string [] { String.Empty });
        }
        
        public void Unmount(params string [] args)
        {
            CastDevice<IVolumeIPod>().Unmount(args);
        }
        
        public void Eject()
        {
            Eject(new string [] { String.Empty });
        }
        
        public void Eject(params string [] args)
        {
            CastDevice<IVolumeIPod>().Eject(args);
        }
    }
}
