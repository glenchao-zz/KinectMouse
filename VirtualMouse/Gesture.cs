using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VirtualMouse
{
    class Gesture
    {
        [Flags]
        public enum MouseEventFlags : uint
        {
            MOVE        = 0x00000001,
            LEFTDOWN    = 0x00000002,
            LEFTUP      = 0x00000004, 
            RIGHTDOWN   = 0x00000008, 
            RIGHTUP     = 0x00000010,
            WHEEL       = 0x00000800
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        public static void Move(int x, int y)
        {
            mouse_event((int)(MouseEventFlags.MOVE), (uint)x, (uint)y, 0, 0);
        }

        public static void LeftClick()
        {
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0); 
        }
    }
}
