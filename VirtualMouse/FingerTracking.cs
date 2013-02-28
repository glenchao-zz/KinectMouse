using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    internal class FingerTracking
    {
        private bool[,] handMatrix = new bool[120,120];
        private bool[,] contourMatrix = new bool[120,120];

        public bool isContour(int x, int y)
        {
            return contourMatrix[x, y];
        }

        public void parseBinArray(bool[] binaryArray)
        {
            // Conver binaryArray to a binary handMatrix
            int k = 0;
            for (int i = 0; i < 120; i++)
            {
                for (int j = 0; j < 120; j++)
                {
                    handMatrix[i, j] = binaryArray[k++];
                }
            }

            for (int i = 0; i < 120; i++)
            {
                for (int j = 0; j < 120; j++)
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
                        if (i < 119)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i + 1, j] ? validPointCount + 1 : validPointCount;
                        }
                        if (j > 0)
                        {
                            adjPointCount++;
                            validPointCount = handMatrix[i, j - 1] ? validPointCount + 1 : validPointCount;
                        }
                        if (j < 119)
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