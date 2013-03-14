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
        public Point FingerDownPos;
        public Point MouseDownPos;

        public GestureController()
        {
            Buffer = new Queue<Point>(10);
        }

        public void Add2Buffer(Point pt)
        {
            // If buffer is full
            if (this.Buffer.Count == 10)
            {
                // pop
                this.Buffer.Dequeue();
            }
            if (this.Buffer.Count == 0)
            {
                FingerDownPos = pt;
            }
            this.Buffer.Enqueue(pt);
            Point pos = new Point();
            pos.X = (int)(this.MouseDownPos.X + this.Buffer.Average(k => k.X) - FingerDownPos.X);
            pos.Y = (int)(this.MouseDownPos.Y + this.Buffer.Average(k => k.Y) - FingerDownPos.Y);
            GestureReady(pos);
        }

        public void ResetBuffer()
        {
            this.Buffer.Clear();
        }


    }
}
