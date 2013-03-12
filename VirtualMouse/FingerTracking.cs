using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    internal class FingerTracking
    {
        private const int Width = 640;
        private const int Height = 480;

        /*
         * Jump parameters used in finding palm and finger (tweek if necessary)
         */
        // Size of the jump after check a possible palm point
        private const int PalmJump = 30;
        // Size of the jump after check a possible fingertip
        private const int FingerJump = 25;
        // Size of the jump after find a valid fingertips (Percentage over the total)
        private const double FingerJumpPerc = 0.15f;

        /*
         * K-curvature parameters
         */
        // Num of points away between three sample points
        private const int K = 30;
        // Angle formed by three sample points
        private const double Theta = 50 * (Math.PI / 180);



        private bool[,] handMatrix;
        private bool[,] contourMatrix;

        private List<Point> contourPoints;
        private List<Point> insidePoints;
        private Point palm;
        private List<Point> fingertips;

        public bool isContour(int x, int y)
        {
            return contourMatrix[x, y];
            //return newPoints.Contains(new Point(x, y));
        }

        public List<Point> getContour()
        {
            return contourPoints;
        }

        public List<Point> getFingers()
        {
            return fingertips;
        }

        public Point getPalm()
        {
            return palm;
        }

        public void parseBinArray(bool[] binaryArray, double minX, double minY, double maxX, double maxY)
        {
            // Initialize local var
            handMatrix = new bool[Width, Height];
            contourMatrix = new bool[Width, Height];
            contourPoints = new List<Point>();
            insidePoints = new List<Point>();
            fingertips = new List<Point>();

            // Conver binaryArray to a binary handMatrix
            int k = 0;
            for (int j = 0; j < Height; j++)
                for (int i = 0; i < Width; i++)
                    handMatrix[i, j] = binaryArray[k++];


            // A point is a inside point if all its adjacent point is part of the hand, 
            // otherwise it is a "potential" contour point
            HashSet<Point> potentialPoints = new HashSet<Point>();
            for (int i = (int)minX * 2; i < (int)maxX * 2; i++)
            {
                for (int j = (int)minY * 2; j < (int)maxY * 2; j++)
                {
                    if (handMatrix[i, j])
                    {
                        int adjPointCount = 0;
                        int validPointCount = 0;

                        if (i > 0)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i - 1, j] ? validPointCount + 1 : validPointCount;
                        }
                        if (i < Width - 1)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i + 1, j] ? validPointCount + 1 : validPointCount;
                        }
                        if (j > 0)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i, j - 1] ? validPointCount + 1 : validPointCount;
                        }
                        if (j < Height - 1)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i, j + 1] ? validPointCount + 1 : validPointCount;
                        }


                        if (validPointCount != adjPointCount)
                        {
                            // Is contour
                            contourMatrix[i, j] = true;
                            potentialPoints.Add(new Point(i, j));
                        }
                        else
                        {
                            // Is inside
                            insidePoints.Add(new Point(i, j));
                        }

                    }
                }
            }

            // Get a sorted list of contour points
            int maxPoints = 0;
            while (potentialPoints.Count > 0)
            {
                List<Point> frontier = calculateFrontier(ref potentialPoints);
                if (frontier.Count > maxPoints)
                {
                    maxPoints = frontier.Count;
                    contourPoints = frontier;
                }
            }

            // Find palm and fingers
            findPalm();
            findFingers();
        }

        /*
         * This function calcute the border of a closed figure starting in one of the contour points.
         * The turtle algorithm is used.
         */
        private List<Point> calculateFrontier(ref HashSet<Point> potentialPoints)
        {
            List<Point> list = new List<Point>();
            Point start = potentialPoints.ElementAt(0);
            Point last = new Point(-1, -1);
            Point current = new Point(start.X, start.Y);
            int dir = 0;

            do
            {
                if (handMatrix[(int)current.X, (int)current.Y])
                {
                    dir = (dir + 1) % 4;
                    if (current != last)
                    {
                        list.Add(new Point(current.X, current.Y));
                        potentialPoints.Remove(current);
                        last = new Point(current.X, current.Y);
                    }
                }
                else
                {
                    dir = (dir + 4 - 1) % 4;
                }

                switch (dir)
                {
                    case 0: current.X += 1; break; // Down
                    case 1: current.Y += 1; break; // Right
                    case 2: current.X -= 1; break; // Up
                    case 3: current.Y -= 1; break; // Left
                }
            } while (current != start);

            return list;
        }

        // Find a largest circle in the hand area and label the center as palm
        private void findPalm()
        {
            float min, max, distance;
            max = float.MinValue;

            for (int j = 0; j < insidePoints.Count; j += PalmJump)
            {
                min = float.MaxValue;
                for (int k = 0; k < contourPoints.Count; k += PalmJump)
                {
                    distance = distanceEuclidean(insidePoints[j], contourPoints[k]);
                    if (!isCircleInside(insidePoints[j], distance)) continue;
                    if (distance < min) min = distance;
                    if (min < max) break;
                }

                if (max < min && min != float.MaxValue)
                {
                    max = min;
                    palm = insidePoints[j];
                }
            }
        }

        private void findFingers()
        {
            fingertips = new List<Point>();

            int numPoints = contourPoints.Count;
            Point p1, p2, p3;
            double angle;
            int i, step = 1;

            // Skip if not enough points in contour
            if (K > numPoints) return;

            // Find the fingertips
            for (i = 0; i < numPoints; i += step)
            {
                p1 = contourPoints[(i - K + numPoints) % numPoints];
                p2 = contourPoints[i];
                p3 = contourPoints[(i + K) % numPoints];

                angle = calculateAngle(p1 - p2, p3 - p2);

                if (angle > 0 && angle < Theta && contourPoints[i].Y > palm.Y)
                {
                    // Skip if p2 is closer to the palm than p1 & p3
                    double dp2 = distanceEuclideanSquared(p2, palm);
                    if (dp2 < distanceEuclideanSquared(p1, palm) &&
                        dp2 < distanceEuclideanSquared(p3, palm))
                        continue;

                    fingertips.Add(contourPoints[i]);
                    i += (int)(FingerJumpPerc * numPoints);
                    //step = FingerJump;
                }
            }
        }

        private bool isCircleInside(Point p, float r)
        {
            if (p.X - r < 0 || !handMatrix[(int)(p.X - r), (int)p.Y])
            {
                return false;
            }
            if (p.X + r >= Width || !handMatrix[(int)(p.X + r), (int)p.Y])
            {
                return false;
            }
            if (p.Y - r < 0 || !handMatrix[(int)p.X, (int)(p.Y - r)])
            {
                return false;
            }
            if (p.Y + r >= Height || !handMatrix[(int)p.X, (int)(p.Y + r)])
            {
                return false;
            }

            return true;
        }

        private float distanceEuclidean(Point p1, Point p2)
        {
            return (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        private float distanceEuclideanSquared(Point p1, Point p2)
        {
            return (float)((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        private double calculateAngle(System.Windows.Vector r1, System.Windows.Vector r2)
        {
            return Math.Acos((r1.X * r2.X + r1.Y * r2.Y) / (r1.Length * r2.Length));
        }
    }
}