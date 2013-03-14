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
        public List<Point> fingertips { get; set; }
        public bool b_Palm { get; set; }
        public Point palm { get; set; }

        public Hand()
        {
            contourPoints = new List<Point>();
            insidePoints = new List<Point>(); 
            fingertips = new List<Point>();
        }

        public void reset()
        {
            contourPoints.Clear();
            insidePoints.Clear();
            fingertips.Clear();
        }
    }
}
