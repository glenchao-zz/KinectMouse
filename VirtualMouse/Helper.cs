using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    static class Helper
    {
        public static int Point2DepthIndex(Point point)
        {
            return 640 * ((int)point.Y) + (int)point.X;
        }
    }
}
