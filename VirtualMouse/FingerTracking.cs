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
        private const int PalmInsideJump = 5;
        private const double PalmContourJumpPerc = 0.05f;
        // Size of the jump after find a valid fingertips (Percentage over the total)
        private const double FingerJumpPerc = 0.10f;

        /*
         * K-curvature parameters
         */
        // Num of points away between three sample points
        private const int K = 30;
        // Angle formed by three sample points
        private const double Theta = 40 * (Math.PI / 180);

        private bool[,] handMatrix;
        private bool[,] contourMatrix;

        private Hand trackedHand = new Hand();

        private bool b_Palm;

        public bool isContour(int x, int y)
        {
            return contourMatrix[x, y];
        }

        public Hand getHand()
        {
            return trackedHand;
        }

        public bool hasPalm()
        {
            return b_Palm;
        }

        public void parseBinArray(bool[] binaryArray, double minX, double minY, double maxX, double maxY)
        {
            // Initialize local var
            handMatrix = new bool[Width, Height];
            contourMatrix = new bool[Width, Height];
            trackedHand.reset();

            // Conver binaryArray to a binary handMatrix
            int index;
            for (int i = (int)minX * 2; i < (int)maxX * 2; i++)
            {
                for (int j = (int)minY * 2; j < (int)maxY * 2; j++)
                {
                    index = Helper.Point2Index(new Point(i, j));
                    handMatrix[i, j] = binaryArray[index];
                }
            }


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
                            trackedHand.insidePoints.Add(new Point(i, j));
                        }

                    }
                }
            }

            // Get a sorted list of contour points
            List<Point> maxFrontier = new List<Point>();
            while (potentialPoints.Count > 0)
            {
                List<Point> frontier = calculateFrontier(ref potentialPoints);
                if (frontier.Count > maxFrontier.Count)
                {
                    maxFrontier = frontier;
                }
            }
            trackedHand.contourPoints = maxFrontier;

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
            b_Palm = false;
            float minDistToContour, largestRadius, distance;
            int contourJump = (int)(PalmContourJumpPerc * trackedHand.contourPoints.Count) + 1;
            List<Point> possiblePalm = new List<Point>();
            largestRadius = float.MinValue;

            bool validInside;
            for (int j = 0; j < trackedHand.insidePoints.Count; j += PalmInsideJump)
            {
                validInside = true;
                minDistToContour = float.MaxValue;
                for (int k = 0; k < trackedHand.contourPoints.Count; k += contourJump)
                {
                    distance = distanceEuclidean(trackedHand.insidePoints[j], trackedHand.contourPoints[k]);
                    if (distance < 25)
                    {
                        validInside = false;
                        break;
                    }
                    if (!isCircleInside(trackedHand.insidePoints[j], distance)) continue;
                    if (distance < minDistToContour) minDistToContour = distance;
                }

                if (validInside && largestRadius < minDistToContour && minDistToContour != float.MaxValue)
                {
                    largestRadius = minDistToContour;
                    possiblePalm.Add(trackedHand.insidePoints[j]);
                    b_Palm = true;
                }
            }
            if (possiblePalm.Count > 0)
                trackedHand.palm = new Point(possiblePalm.Average(k => k.X), possiblePalm.Average(k => k.Y));
            else
                trackedHand.palm = new Point();
        }

        private void findFingers()
        {
            int numPoints = trackedHand.contourPoints.Count;
            Point p1, p2, p3, palm;
            double angle, dp2;

            // Skip if not enough points in contour
            if (K > numPoints) return;// || !b_Palm) return;

            // Find the fingertips
            for (int i = K; i < numPoints - K; i++)
            {
                p1 = trackedHand.contourPoints[i - K];
                p2 = trackedHand.contourPoints[i];
                p3 = trackedHand.contourPoints[i + K];

                angle = calculateAngle(p1 - p2, p3 - p2);

                if (angle > 0 && angle < Theta && p2.Y > trackedHand.palm.Y)
                {
                    // Skip if p2 is closer to the palm than p1 & p3
                    dp2 = distanceEuclideanSquared(p2, trackedHand.palm);
                    palm = trackedHand.palm;
                    if (dp2 < distanceEuclideanSquared(p1, palm) &&
                        dp2 < distanceEuclideanSquared(p3, palm))
                        continue;

                    trackedHand.fingertips.Add(p2);
                    i += (int)(FingerJumpPerc * numPoints);
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
            float x = (float)(p1.X - p2.X);
            float y = (float)(p1.Y - p2.Y);
            return (float)Math.Sqrt(x * x + y * y);
        }

        private int distanceEuclideanSquared(Point p1, Point p2)
        {
            int x = (int)(p1.X - p2.X);
            int y = (int)(p1.Y - p2.Y);
            return (x * x + y * y);
        }

        private double calculateAngle(System.Windows.Vector r1, System.Windows.Vector r2)
        {
            return Math.Acos((r1.X * r2.X + r1.Y * r2.Y) / (r1.Length * r2.Length));
        }
    }
}
