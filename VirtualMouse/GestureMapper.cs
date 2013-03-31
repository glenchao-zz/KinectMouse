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
                    Action.DownLeft();
                Action.Move(obj.point);
            }
            else if (obj == null && fingers == 1 && clicks == 1)
                Action.ClickLeft();
            else if (obj == null && fingers == 1 && clicks == 2)
            {
                Action.ClickLeft();
                Action.ClickLeft();
            }
            else if (obj == null && fingers == 2 && clicks == 1)
                Action.ClickRight();
            else if (obj == null && fingers == 0 && clicks == 0)
                Action.ClearAction();
        }
    }

    class MapperObject
    {
        public bool isDragging { get; set; }
        public Point point { get; set; }
        public MapperObject() { }
        public MapperObject(Point point, bool isDragging){
            this.isDragging = isDragging; 
            this.point = point; 
        }
    }
}
