using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    class Hand
    {
        private List<Point> contourPoints = new List<Point>();
        private List<Point> insidePoints = new List<Point>();
        private bool b_Palm;
        private Point palm;
        private List<Point> fingertips = new List<Point>();

        public void reset()
        {
            contourPoints.Clear();
            insidePoints.Clear();
            fingertips.Clear();
        }

        public List<Point> getContour()
        {
            return contourPoints;
        }

        public List<Point> getFingers()
        {
            return fingertips;
        }

        public void addInsidePoints(int i, int j)
        {
            insidePoints.Add(new Point(i, j));
        }

        public void setContourPoints(List<Point> maxFrontier)
        {
            contourPoints = maxFrontier;
        }

        public int numContourPoints()
        {
            return contourPoints.Count;
        }

        public int numInsidePoints()
        {
            return insidePoints.Count;
        }

        public Point getInsidePoint(int i)
        {
            return insidePoints[i];
        }

        public Point getContourPoint(int i)
        {
            return contourPoints[i];
        }

        public void setPalm(Point possiblePalm)
        {
            palm = possiblePalm;
        }

        public Point getPalm()
        {
            return palm;
        }

        public void addFinger(Point p)
        {
            fingertips.Add(p);
        }
    }
}
