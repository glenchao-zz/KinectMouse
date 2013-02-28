using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualMouse
{
    class User
    {
        public short playerIndex { get; set; }
        public int trackingId { get; set; }

        public User()
        {
            this.playerIndex = 0;
            this.trackingId = 0;
        }
    }
}
