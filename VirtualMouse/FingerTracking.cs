using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    internal class FingerTracking
    {
        private int range = 200;
        private bool[,] handMatrix;
        private bool[,] contourMatrix;

        public FingerTracking()
        {
            handMatrix = new bool[range, range];
            contourMatrix = new bool[range, range];
        }

        public bool isContour(int x, int y)
        {
            return contourMatrix[x, y];
        }

        public void parseBinArray(bool[] binaryArray)
        {
            // Conver binaryArray to a binary handMatrix
            int k = 0;
            for (int i = 0; i < range; i++)
            {
                for (int j = 0; j < range; j++)
                {
                    handMatrix[i, j] = binaryArray[k++];
                }
            }

            for (int i = 0; i < range; i++)
            {
                for (int j = 0; j < range; j++)
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
                        if (i < range-1)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i + 1, j] ? validPointCount + 1 : validPointCount;
                        }
                        if (j > 0)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i, j - 1] ? validPointCount + 1 : validPointCount;
                        }
                        if (j < range-1)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i, j + 1] ? validPointCount + 1 : validPointCount;
                        }

                        // A point is an inside point if all its adjacent point is part of the hand, 
                        // otherwise it is a contour point
                        if (validPointCount == adjPointCount)
                            contourMatrix[i, j] = false;
                        else
                            contourMatrix[i, j] = true;
                    }
                }
            }
        }
    }
}