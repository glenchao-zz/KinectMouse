using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace VirtualMouse
{
    static class Helper
    {
        public static System.Drawing.Point Convert2DrawingPoint(Point pt)
        {
            return new System.Drawing.Point((int)pt.X, (int)pt.Y);
        }

        public static void SaveRecognizer(double yMult, double xMult, double relX, double relY)
        {
            Properties.Settings.Default.yMultiplier = yMult;
            Properties.Settings.Default.xMultiplier = xMult;
            Properties.Settings.Default.relativeX = relX;
            Properties.Settings.Default.relativeY = relY;
            Properties.Settings.Default.b_Recognizer = true;
            Properties.Settings.Default.Save();
        }

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

        public static void SaveSurface(Plane surface)
        {
            Properties.Settings.Default.SurfaceX = surface.normal.x;
            Properties.Settings.Default.SurfaceY = surface.normal.y;
            Properties.Settings.Default.SurfaceZ = surface.normal.z;
            Properties.Settings.Default.SurfaceD = surface.d;
            Properties.Settings.Default.b_Surface = true;
            Properties.Settings.Default.Save();
        }

        public static Plane LoadSurface()
        {
            if (!Properties.Settings.Default.b_Surface)
                return null;

            return new Plane(Properties.Settings.Default.SurfaceX,
                                      Properties.Settings.Default.SurfaceY,
                                      Properties.Settings.Default.SurfaceZ,
                                      Properties.Settings.Default.SurfaceD);
        }

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

        public static int Point2Index(Point point)
        {
            return 640 * ((int)point.Y) + (int)point.X;
        }

        public static Point Index2Point(int i)
        {
            Point pt = new Point();
            pt.X = i % 640;
            pt.Y = (i - pt.X) / 640;
            return pt;
        }

        public static void DrawPoint(double X, double Y, Color color, Canvas canvas)
        {
            Ellipse point = new Ellipse();
            point.Height = 6;
            point.Width = 6;
            point.Fill = new SolidColorBrush(color);
            Canvas.SetTop(point, Y / 2 - 3);
            Canvas.SetLeft(point, X / 2 - 3);
            canvas.Children.Add(point);
        }

        public static void DrawLine(double X1, double X2, double Y1, double Y2, Color color, Canvas canvas)
        {
            Line line = new Line();
            line.Stroke = new SolidColorBrush(color);
            line.StrokeThickness = 1;
            line.X1 = X1 / 2;
            line.X2 = X2 / 2;
            line.Y1 = Y1 / 2;
            line.Y2 = Y2 / 2;
            canvas.Children.Add(line);
        }
    }
}
