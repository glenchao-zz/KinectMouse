using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualMouse
{
    class Player
    {
        public Player(int index)
        {
            this.playerIndex = index;
        }

        public int playerIndex { get; set; }
    }
}
