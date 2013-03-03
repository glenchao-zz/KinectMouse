using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VirtualMouse
{
    /// <summary>
    /// Interaction logic for ActionArea.xaml
    /// </summary>
    public partial class ActionArea : UserControl, INotifyPropertyChanged
    {
        private const int ellipseWidth = 10;
        private Ellipse selectedEllipse = null;
        //private enum corners { topLeft, botLeft, botRight, topRight };
        public event PropertyChangedEventHandler PropertyChanged; 
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


        public ActionArea()
        {
            InitializeComponent();
            this.cornerPoints = new PointCollection();
            this.cornerPoints.Add(cPoint(0, 0)); // top left
            this.cornerPoints.Add(cPoint(0, 50)); // bot left
            this.cornerPoints.Add(cPoint(50, 50)); // bot right
            this.cornerPoints.Add(cPoint(50, 0)); // top right
            this.DataContext = this;
        }

        private void HighLightCorner(object sender, MouseEventArgs e)
        {
            Ellipse el = sender as Ellipse;
            el.Fill = new SolidColorBrush(Colors.Red);
        }
        
        private void UnHighLightCorner(object sender, MouseEventArgs e)
        {
            Ellipse el = sender as Ellipse;
            el.Fill = new SolidColorBrush(Colors.Blue);
        }

        /// <summary>
        /// MouseDown event on an ellipse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectEllipse(object sender, MouseButtonEventArgs e)
        {
            selectedEllipse = sender as Ellipse;
        }

        private void ResizeArea(object sender, MouseEventArgs e)
        {
            if (selectedEllipse == null)
                return;
            
            Point mouse = Mouse.GetPosition(actionCanvas);
            Canvas.SetLeft(selectedEllipse, mouse.X);
            Canvas.SetTop(selectedEllipse, mouse.Y);
            Point[] pointArray = new Point[4];
            this.cornerPoints.CopyTo(pointArray, 0);
            int index = actionCanvas.Children.IndexOf(selectedEllipse) - 1;
            pointArray[index] = cPoint(mouse.X, mouse.Y);
            this.cornerPoints = new PointCollection(pointArray);
        }

        private void DeselectEllipse(object sender, MouseButtonEventArgs e)
        {
            selectedEllipse = null;
        }

        private Point cPoint(double x, double y)
        {
            return new Point(x + ellipseWidth / 2, y + ellipseWidth / 2);
        }

        private void actionCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            this.DeselectEllipse(null, null);
        }
    }
}
