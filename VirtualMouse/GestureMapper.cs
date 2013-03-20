using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VirtualMouse
{
    class GestureMapper
    {
        public void MapGesture2Action(int fingers, int clicks, object obj)
        {
            if (obj != null && fingers == 1 && clicks == 0)
                Action.Move((Point)obj);
            else if (obj == null && fingers == 1 && clicks == 1)
                Action.ClickLeft();
            else if (obj == null && fingers == 2 && clicks == 1)
                Action.ClickRight();
        }
    }
}
