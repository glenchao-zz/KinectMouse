using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VirtualMouse
{
    /// <summary>
    /// Interaction logic for ActionArea.xaml
    /// </summary>
    public partial class ActionArea : UserControl, INotifyPropertyChanged
    {
        // Call back on resize
        public delegate void ResizeEvent();
        public event ResizeEvent ResizeCallBack;
        // Call back on confirm
        public delegate void ConfirmEvent();
        public event ConfirmEvent ConfirmCallBack;

        // General var
        private const int ellipseWidth = 10;
        public event PropertyChangedEventHandler PropertyChanged;

        private const int ellipseIndex = 2;

        private int _maxLength = 0;
        public int maxLength
        {
            get { return this._maxLength; }
            set
            {
                this._maxLength = value;
                this.ValidIndeces = new int[this._maxLength];
            }
        }

        /// <summary>
        /// Selected ellipse when mouse down, cleared when mouse up
        /// </summary>
        private object selectedShape = null;

        /// <summary>
        /// Position where hand first touch surface
        /// </summary>
        private Point downPos = new Point();

        /// <summary>
        /// Variables related to corners of the quad
        /// </summary>
        private PointCollection cornerDelta;
        public enum corners { topLeft, botLeft, botRight, topRight };
        private PointCollection _cornerPoints;
        public PointCollection cornerPoints
        {
            get { return this._cornerPoints; }
            set
            {
                if (this._cornerPoints != value)
                {
                    this._cornerPoints = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("cornerPoints"));
                    }
                }
            }
        }

        public enum borders {leftBorder = 0, botBorder, rightBorder, topBorder};
        public lineEq[] borderEqs { get; set; }

        /// <summary>
        /// Indices of the frame where ActionArea is defined 
        /// </summary>
        public int[] ValidIndeces;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActionArea()
        {
            InitializeComponent();
            borderEqs = new lineEq[4];
            this.cornerPoints = new PointCollection();
            this.cornerPoints.Add(cPoint(0, 0)); // top left
            this.cornerPoints.Add(cPoint(0, 200)); // bot left
            this.cornerPoints.Add(cPoint(140, 200)); // bot right
            this.cornerPoints.Add(cPoint(140, 0)); // top right
            
            for (int i = 0; i < this.cornerPoints.Count; i++)
            {
                Canvas.SetLeft(this.actionCanvas.Children[i + ellipseIndex], this.cornerPoints[i].X - ellipseWidth / 2);
                Canvas.SetTop(this.actionCanvas.Children[i + ellipseIndex], this.cornerPoints[i].Y - ellipseWidth / 2);
            }

            this.DataContext = this;
        }

        /// <summary>
        /// Loads the action area if there's one saved from previous definition
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="botLeft"></param>
        /// <param name="botRight"></param>
        /// <param name="topRight"></param>
        public void LoadActionArea(Point topLeft, Point botLeft, Point botRight, Point topRight)
        {
            borderEqs = new lineEq[4];
            this.cornerPoints = new PointCollection();
            this.cornerPoints.Add(topLeft); // top left
            this.cornerPoints.Add(botLeft); // bot left
            this.cornerPoints.Add(botRight); // bot right
            this.cornerPoints.Add(topRight); // top right

            for (int i = 0; i < this.cornerPoints.Count; i++)
            {
                Canvas.SetLeft(this.actionCanvas.Children[i + ellipseIndex], this.cornerPoints[i].X - ellipseWidth / 2);
                Canvas.SetTop(this.actionCanvas.Children[i + ellipseIndex], this.cornerPoints[i].Y - ellipseWidth / 2);
            }

            this.DataContext = this;
        }

        /// <summary>
        /// Initialization
        /// </summary>
        public void InitActionArea()
        {

            GetBorderEquations();
            GetValidIndices();
        }

        /// <summary>
        /// Calculates the midpoint of the ActionArea. Used by the SurfaceDetection as the origin
        /// </summary>
        /// <returns></returns>
        public Point MidPoint()
        {
            return new Point(new[] { _cornerPoints[(int)corners.topLeft].X, 
                                     _cornerPoints[(int)corners.topRight].X }.Average(),
                             new[] { _cornerPoints[(int)corners.topRight].Y, 
                                     _cornerPoints[(int)corners.botRight].Y }.Average());
        }

        /// <summary>
        /// UI Indicator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HighLightCorner(object sender, MouseEventArgs e)
        {
            if (sender.GetType() == typeof(Ellipse))
            {
                Ellipse el = sender as Ellipse;
                el.Fill = new SolidColorBrush(Colors.Red);
            }
            else if (sender.GetType() == typeof(Polygon))
            {
                Polygon poly = sender as Polygon;
                poly.Fill = new SolidColorBrush(Colors.Red);
            }

        }
        
        /// <summary>
        /// UI Indicator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnHighLightCorner(object sender, MouseEventArgs e)
        {
            if (sender.GetType() == typeof(Ellipse))
            {
                Ellipse el = sender as Ellipse;
                el.Fill = new SolidColorBrush(Colors.Blue);
            }
            else if (sender.GetType() == typeof(Polygon))
            {
                Polygon poly = sender as Polygon;
                poly.Fill = new SolidColorBrush(Colors.Blue);
            }
        }

        /// <summary>
        /// MouseDown event on an ellipse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectShape(object sender, MouseButtonEventArgs e)
        {
            this.selectedShape = sender;
            if (sender.GetType() == typeof(Polygon))
            {
                this.downPos = Mouse.GetPosition(this.area);
                this.cornerDelta = new PointCollection();
                foreach (Point pt in this.cornerPoints)
                {
                    this.cornerDelta.Add(new Point(this.downPos.X - pt.X, this.downPos.Y - pt.Y));
                }
            }
        }

        /// <summary>
        /// Binding ellipses to polygon when dragging corner to resize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeArea(object sender, MouseEventArgs e)
        {
            Ellipse selectedEllipse = this.selectedShape as Ellipse;
            Point mouse = Mouse.GetPosition(this.actionCanvas);
            Canvas.SetLeft(selectedEllipse, mouse.X - ellipseWidth / 2);
            Canvas.SetTop(selectedEllipse, mouse.Y - ellipseWidth / 2);
            Point[] pointArray = new Point[4];
            this.cornerPoints.CopyTo(pointArray, 0);
            int index = actionCanvas.Children.IndexOf(selectedEllipse) - ellipseIndex;
            pointArray[index] = new Point(mouse.X, mouse.Y);
            this.cornerPoints = new PointCollection(pointArray);

            InitActionArea();
        }

        /// <summary>
        /// Binding polygon to corner ellipses when moving 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MovePolygon(object sender, MouseEventArgs e)
        {
            Point mouse = Mouse.GetPosition(this.actionCanvas);
            Point[] pointArray = new Point[4];
            this.cornerPoints.CopyTo(pointArray, 0);
            for (int i = 0; i < pointArray.Length; i++)
            {
                pointArray[i].X = mouse.X - cornerDelta[i].X;
                pointArray[i].Y = mouse.Y - cornerDelta[i].Y;
                Canvas.SetLeft(this.actionCanvas.Children[i + ellipseIndex], pointArray[i].X - ellipseWidth / 2);
                Canvas.SetTop(this.actionCanvas.Children[i + ellipseIndex], pointArray[i].Y - ellipseWidth / 2);
            }
            this.cornerPoints = new PointCollection(pointArray);
            InitActionArea();
        }

        /// <summary>
        /// Get valid indices
        /// </summary>
        private void GetValidIndices()
        {
            // Calculate valid indices 
            for (int i = 0; i < maxLength; i++)
            {
                Point pt = Helper.Index2Point(i);
                if (this.borderEqs[(int)borders.leftBorder].IsLeftOf(pt.X, pt.Y) &&
                    this.borderEqs[(int)borders.rightBorder].IsRightOf(pt.X, pt.Y) &&
                    this.borderEqs[(int)borders.topBorder].IsAbove(pt.X, pt.Y) &&
                    this.borderEqs[(int)borders.botBorder].IsBelow(pt.X, pt.Y))
                {
                    this.ValidIndeces[i] = 1;
                }
                else
                    this.ValidIndeces[i] = 0;
            }
        }

        /// <summary>
        /// Calculate the border equations for faster computation when checking valid indices
        /// </summary>
        private void GetBorderEquations()
        {
            // Calculate border euqations
            for (int i = 0; i < cornerPoints.Count; i++)
            {
                Point p1 = cornerPoints[i];
                Point p2 = cornerPoints[(i + 1) % cornerPoints.Count];
                borderEqs[i] = new lineEq(p1, p2);
            }
        }

        /// <summary>
        /// Mouse Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void actionCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            this.DeselectShape(null, null);
        }

        private void actionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.selectedShape == null || this.ValidIndeces == null)
                return;

            if (this.selectedShape.GetType() == typeof(Ellipse))
                ResizeArea(sender, e);
            else if (this.selectedShape.GetType() == typeof(Polygon))
                MovePolygon(sender, e);
        }

        private Point cPoint(double x, double y)
        {
            return new Point(x + ellipseWidth / 2, y + ellipseWidth / 2);
        }

        private void DeselectShape(object sender, MouseButtonEventArgs e)
        {
            this.selectedShape = null;
        }


        /// <summary>
        /// callback functionwhen clicking!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmCallBack();
        }
    }

    /// <summary>
    /// A line equation where y = m*x + b
    /// </summary>
    public class lineEq
    {
        public double m { get; set; }
        public double b { get; set; }
        public Point p1 { get; set; }
        public Point p2 { get; set; }

        public lineEq(Point p1, Point p2)
        {
            this.p1 = new Point(p1.X, p1.Y);
            this.p2 = new Point(p2.X, p2.Y);
            this.m = (this.p2.Y - this.p1.Y) / (this.p2.X - this.p1.X);
            this.b = -this.p1.X * this.m + this.p1.Y;
        }

        /// <summary>
        /// The border is above the point (x,y)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsAbove(double x, double y)
        {

            return this.m * x + this.b < y;
        }

        /// <summary>
        /// The border is below the point (x,y)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsBelow(double x, double y)
        {
            return this.m * x + this.b > y;
        }

        /// <summary>
        /// The border is left of the point (x,y)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsRightOf(double x, double y)
        {
            if (double.IsInfinity(this.m))
                return this.p1.X > x;
            else 
                return (y - this.b) / this.m > x;
        }

        /// <summary>
        /// The border is right of the point (x,y)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsLeftOf(double x, double y)
        {
            if (double.IsInfinity(this.m))
                return this.p1.X < x;
            else
                return (y - this.b) / this.m < x;
        }
    }
}
