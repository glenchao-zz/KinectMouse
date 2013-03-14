using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VirtualMouse
{
    class GestureController
    {
        public delegate void GestureEvent(Point pt);
        public event GestureEvent GestureReady;

        private Queue<Point> Buffer;



        public GestureController()
        {
            Buffer = new Queue<Point>(10);
        }

        public void AddFrame(Point pt)
        {
            // If buffer is full
            if (this.Buffer.Count == 10)
            {
                // pop
                this.Buffer.Dequeue();
            }
            this.Buffer.Enqueue(pt);
            Point ret = new Point();
            ret.X = (int)this.Buffer.Average(k => k.X);
            ret.Y = (int)this.Buffer.Average(k => k.Y);
            GestureReady(ret);
        }


    }
}
