using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VirtualMouse
{
    class GestureMapper
    {
        public void MapGesture2Action(int fingers, int clicks, MapperObject obj)
        {
            if (obj != null && fingers == 1 && clicks == 0)
            {
                if (obj.isDragging)
                    MouseAction.DownLeft();
                MouseAction.Move(obj.point);
            }
            else if (obj != null && fingers == 2 && clicks == 0)
            {
                MouseAction.MouseScroll((uint)obj.value);
            }
            else if (obj == null && fingers == 1)
            {
                for(int i = 0; i < clicks; i++)
                    MouseAction.ClickLeft();
            }
            else if (obj == null && fingers == 2 && clicks == 1)
                MouseAction.ClickRight();
            else if (obj == null && fingers == 0 && clicks == 0)
                MouseAction.ClearAction();
        }
    }

    class MapperObject
    {
        public bool isDragging { get; set; }
        public Point point { get; set; }
        public int value { get; set; }
        public MapperObject() { }
        public MapperObject(Point point, bool isDragging, int value)
        {
            this.isDragging = isDragging; 
            this.point = point;
            this.value = value;
        }
    }
}
