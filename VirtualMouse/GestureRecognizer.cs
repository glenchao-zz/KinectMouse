using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VirtualMouse
{
    class GestureRecognizer
    {
        public double relativeX { get; set; }
        public double relativeY { get; set; }

        public double xMultiplier { get; set; }
        public double yMultiplier { get; set; }


        public delegate void GestureEvent(int fingers, int clicks, object obj);
        public event GestureEvent GestureReady;

        private Queue<Point> MovingBuffer;
        private Queue<double> ClickBuffer;
        private Queue<double> ClickFilter;

        public Point FingerDownPos;
        public Point MouseDownPos;

        private int zeroCount = 0;
        private int clickCount = 0;
        private int numFingers = 0;

        const int mBufferLength = 10;
        const int cBufferLength = 15;
        const int cFilterLength = 4;

        public GestureRecognizer()
        {
            this.MovingBuffer = new Queue<Point>(mBufferLength);
            this.ClickBuffer = new Queue<double>(cBufferLength);
            this.ClickFilter = new Queue<double>(cFilterLength);
        }

        public void Add2Buffer(Hand hand)
        {
            // If buffer is full
            if (this.MovingBuffer.Count == mBufferLength)
                this.MovingBuffer.Dequeue();
            if (this.ClickBuffer.Count == cBufferLength)
                this.ClickBuffer.Dequeue();
            if (this.ClickFilter.Count == cFilterLength)
                this.ClickFilter.Dequeue();

            if (hand.fingertips.Count == 0)
            {
                if (++this.zeroCount == 5)
                {
                    if (this.clickCount > 0)
                    {
                        Console.WriteLine(numFingers + " fingers click " + clickCount + " times");
                        GestureReady(numFingers, clickCount, null);
                    }
                    this.Reset();
                    return;
                }
            }
            else
            {
                this.zeroCount = 0;
                this.numFingers = Math.Max(hand.fingertips.Count, numFingers);
            }

            // Cursor click setup
            ClickFilter.Enqueue(hand.fingertips.Count > 0 ? 1 : 0);
            this.ClickBuffer.Enqueue(ClickFilter.Average());

            bool tooClose = false;
            this.clickCount = 0;
            foreach (double d in this.ClickBuffer)
            {
                if (d == 0.50 && !tooClose)
                {
                    tooClose = true;
                    this.clickCount++;
                }
                else
                    tooClose = false;

            }
            this.clickCount = this.clickCount / 2;

            // Cursor move setup
            if (hand.fingertips.Count == 1)
            {
                Point finger = Helper.Convert2DrawingPoint(hand.fingertips[0].point);
                finger.X = (int)((finger.X - relativeX * 2) * xMultiplier * 1.1);
                finger.Y = (int)((relativeY * 2 - finger.Y) * yMultiplier * 1.1);
                this.MovingBuffer.Enqueue(finger);
                if (this.MovingBuffer.Count == 1)
                {
                    FingerDownPos = this.MovingBuffer.ElementAt(0);
                }
                else if (this.MovingBuffer.Count > mBufferLength * 2 / 3)
                {
                    Point pos = new Point();
                    pos.X = (int)(this.MouseDownPos.X + this.MovingBuffer.Average(k => k.X) - FingerDownPos.X);
                    pos.Y = (int)(this.MouseDownPos.Y + this.MovingBuffer.Average(k => k.Y) - FingerDownPos.Y);
                    GestureReady(1, 0, pos);
                }
            }
        }

            

        public void Reset()
        {
            this.MovingBuffer.Clear();
            this.ClickBuffer.Clear();
            this.ClickFilter = new Queue<double>(new[] { 0.0, 0.0, 0.0, 0.0 });
            this.zeroCount = 0;
            this.clickCount = 0;
            this.numFingers = 0;
            this.MouseDownPos = System.Windows.Forms.Cursor.Position;
        }


    }
}
