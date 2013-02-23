using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualMouse
{
    class FingerTracking
    {
        private bool[,] handMatrix = new bool[120, 90];
        private bool[,] contourMatrix = new bool[120, 90];
        private int[] indexArray;

        public FingerTracking(bool[] binArray, int[] indexArray)
        {
            this.indexArray = indexArray;

            for (int i = 0; i < 120; i++)
            {
                for (int j = 0; j < 90; j++)
                {
                    handMatrix[i, j] = binArray[i * j + j];
                }
            }
            drawHandContour();
        }

        public void drawHandContour()
        {
            for (int i = 0; i < 120; i++)
            {
                for (int j = 0; j < 90; j++)
                {
                    if (handMatrix[i, j])
                    {
                        if (i == 0 || j == 0 || i == 119 || j == 89)
                        {
                            contourMatrix[i, j] = true;
                            continue;
                        }
                        if (handMatrix[i - 1, j] && handMatrix[i + 1, j] && handMatrix[i, j - 1] && handMatrix[i, j + 1])
                            contourMatrix[i, j] = false;
                        else
                            contourMatrix[i, j] = true;
                    }
                }
            }
        }

        public bool isContour(int x, int y)
        {
            return contourMatrix[x, y];
        }
    }
}
