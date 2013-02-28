using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VirtualMouse
{
    static class Helper
    {
        public static int Point2DepthIndex(Point point)
        {
            return 640 * ((int)point.Y) + (int)point.X;
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
