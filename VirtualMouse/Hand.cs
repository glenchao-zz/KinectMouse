using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    class Hand
    {
        public List<Point> contourPoints { get; set; }
        public List<Point> insidePoints { get; set; }
        public List<Fingertip> fingertips { get; set; }
        public bool hasPalm { get; set; }
        public Point palm { get; set; }

        public Hand()
        {
            contourPoints = new List<Point>();
            insidePoints = new List<Point>();
            fingertips = new List<Fingertip>();
        }
    }

    class Fingertip
    {
        public Point point { get; set; }
        public bool isTouching { get; set; }

        public Fingertip(Point pt, bool isTouching)
        {
            this.point = pt;
            this.isTouching = isTouching;
        }
    }
}
