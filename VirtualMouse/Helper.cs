using System;
using System.Linq;
using System.Windows;
using Microsoft.Kinect;

namespace VirtualMouse
{
    static class Helper
    {
        /// <summary>
        /// Converts different points 
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static System.Drawing.Point Convert2DrawingPoint(Point pt)
        {
            return new System.Drawing.Point((int)pt.X, (int)pt.Y);
        }

        /// <summary>
        /// Save recognizer object
        /// </summary>
        /// <param name="yMult"></param>
        /// <param name="xMult"></param>
        /// <param name="relX"></param>
        /// <param name="relY"></param>
        public static void SaveRecognizer(double yMult, double xMult, double relX, double relY)
        {
            Properties.Settings.Default.yMultiplier = yMult;
            Properties.Settings.Default.xMultiplier = xMult;
            Properties.Settings.Default.relativeX = relX;
            Properties.Settings.Default.relativeY = relY;
            Properties.Settings.Default.b_Recognizer = true;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Loads recognizer object and restore
        /// </summary>
        /// <param name="gr"></param>
        public static void LoadRecognizer(GestureRecognizer gr)
        {
            if (Properties.Settings.Default.b_Recognizer)
            {
                gr.yMultiplier = Properties.Settings.Default.yMultiplier;
                gr.xMultiplier = Properties.Settings.Default.xMultiplier;
                gr.relativeX = Properties.Settings.Default.relativeX;
                gr.relativeY = Properties.Settings.Default.relativeY;
            }
        }

        /// <summary>
        /// Save ActionArea object data
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="botLeft"></param>
        /// <param name="botRight"></param>
        /// <param name="topRight"></param>
        public static void SaveActionArea(Point topLeft, Point botLeft, Point botRight, Point topRight)
        {
            Properties.Settings.Default.topLeftX = topLeft.X;
            Properties.Settings.Default.topLeftY = topLeft.Y;
            Properties.Settings.Default.botLeftX = botLeft.X;
            Properties.Settings.Default.botLeftY = botLeft.Y;
            Properties.Settings.Default.botRightX = botRight.X;
            Properties.Settings.Default.botRightY = botRight.Y;
            Properties.Settings.Default.topRightX = topRight.X;
            Properties.Settings.Default.topRightY = topRight.Y;
            Properties.Settings.Default.b_ActionArea = true;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Load ActionArea object and restore
        /// </summary>
        /// <param name="actionArea"></param>
        public static void LoadActionArea(ActionArea actionArea)
        {
            if (Properties.Settings.Default.b_ActionArea)
                actionArea.LoadActionArea(new Point(Properties.Settings.Default.topLeftX,
                                                    Properties.Settings.Default.topLeftY),
                                          new Point(Properties.Settings.Default.botLeftX,
                                                    Properties.Settings.Default.botLeftY),
                                          new Point(Properties.Settings.Default.botRightX,
                                                    Properties.Settings.Default.botRightY),
                                          new Point(Properties.Settings.Default.topRightX,
                                                    Properties.Settings.Default.topRightY));
        }

        /// <summary>
        /// Save surface object
        /// </summary>
        /// <param name="surface"></param>
        public static void SaveSurface(Plane surface)
        {
            Properties.Settings.Default.SurfaceX = surface.normal.x;
            Properties.Settings.Default.SurfaceY = surface.normal.y;
            Properties.Settings.Default.SurfaceZ = surface.normal.z;
            Properties.Settings.Default.SurfaceD = surface.d;
            Properties.Settings.Default.b_Surface = true;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Loads surface object and restore
        /// </summary>
        /// <returns></returns>
        public static Plane LoadSurface()
        {
            if (!Properties.Settings.Default.b_Surface)
                return null;

            return new Plane(Properties.Settings.Default.SurfaceX,
                                      Properties.Settings.Default.SurfaceY,
                                      Properties.Settings.Default.SurfaceZ,
                                      Properties.Settings.Default.SurfaceD);
        }

        /// <summary>
        /// Calculate distribution of depth over an axis and pick the most common one to 
        /// reduce the chance of error
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static double GetMostCommonDepthImagePixel(DepthImagePixel[] data, int start, int length)
        {
            if (start + length > data.Length)
                throw new InvalidOperationException();

            DepthImagePixel[] ret = new DepthImagePixel[length];
            for (int i = 0; i < length; i++)
            {
                if(data[i+start].IsKnownDepth)
                    ret[i] = data[i + start];
            }
            var temp = ret.GroupBy(x => x.Depth).OrderByDescending(x => x.Count()).First().Key;
            return (double)temp;
        }

        /// <summary>
        /// Converts 2D (X,Y) indices to 1D index base on image resolution
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static int Point2Index(Point point)
        {
            return 640 * ((int)point.Y) + (int)point.X;
        }

        /// <summary>
        /// Converts 1D index to 2D (X,Y) indices base on image resolution
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static Point Index2Point(int i)
        {
            Point pt = new Point();
            pt.X = i % 640;
            pt.Y = (i - pt.X) / 640;
            return pt;
        }
    }
}
