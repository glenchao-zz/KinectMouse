using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace VirtualMouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width and Height of the output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Active Kinect Sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Bitmaps that will hold color and depth info
        /// </summary>
        private WriteableBitmap colorBitmap;
        private WriteableBitmap depthBitmap;
        
        /// <summary>
        /// Buffers for the color and depth data received from the camera
        /// </summary>
        private byte[] colorPixels;
        private DepthImagePixel[] depthImagePixels;
        private byte[] depthPixels;

        /// <summary>
        /// Variable settings for skeleton drawing
        /// </summary>
        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;
        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 182, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private readonly JointType[] trackedJoints = {JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ShoulderRight,
                                                      JointType.ElbowLeft, JointType.ElbowRight, 
                                                      JointType.WristLeft, JointType.WristRight, 
                                                      JointType.HandLeft, JointType.HandRight};
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource; 

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Preps the kinect for color, depth, and skeleton if a sensor is detected. 
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing 
            this.drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control 
            this.imageSource = new DrawingImage(this.drawingGroup);
            // Display the drawing using our image contorl 
            this.colorImage.Source = this.imageSource; 


            foreach (var sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    this.sensor = sensor;
                    break;
                }
            }

            if (this.sensor != null)
            {
                // Turn on the color, depth, and skeelton stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.SkeletonStream.Enable();

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.depthImagePixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                // Initialize the bitmap we'll display on-screen 
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                                                       this.sensor.ColorStream.FrameHeight,
                                                       96.0,
                                                       96.0,
                                                       PixelFormats.Bgr32,
                                                       null);
                this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                                                       this.sensor.DepthStream.FrameHeight,
                                                       96.0,
                                                       96.0,
                                                       PixelFormats.Bgr32,
                                                       null);
                
                // Set the color and depth image we display to point to the bitmap where we'll put the image data
                // this.colorImage.Source = this.colorBitmap;
                this.depthImage.Source = this.depthBitmap;

                // Add an event handler to be called whenever there is a color, depth, or skeleton frame data is ready
                this.sensor.ColorFrameReady += sensor_ColorFrameReady;
                this.sensor.DepthFrameReady += sensor_DepthFrameReady;
                this.sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;

                // Start the sensor
                try
                {
                    this.sensor.Start();
                }
                catch (IOException ex)
                {
                    this.sensor = null;
                    this.DebugMsg.Text = ex.Message;
                }
            }

            if (this.sensor == null)
            {
                this.DebugMsg.Text = "No Kinect ready. Please plug in a kinect";
            }
        }

        /// <summary>
        /// Stop the kinect when shutting down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.sensor != null)
                this.sensor.Stop();
        }

        /// <summary>
        /// Event handler for kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temp array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFramReady event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temp array 
                    depthFrame.CopyDepthImagePixelDataTo(this.depthImagePixels);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB 
                    int colorPixelIndex = 0;
                    for (int i = 0; i < this.depthImagePixels.Length; ++i)
                    {
                        // Get the depth for this pixel 
                        short depth = depthImagePixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant 
                        // rather tahn least-significant bits. 
                        // We're preserving detail, although the intensity will 'wrap'. 
                        // Values outside the reliable depth range are mapped to 0 (black).
                        
                        // Note: Using conditionals in this loop could degrade performance. 
                        // Consider using a lookup table instead when writing production code. 
                        // See the KinectDepthViewer class used by the KinectExplorer sample 
                        // for a lookup table example. 
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                        this.depthPixels[colorPixelIndex++] = intensity;  // Write the blue byte
                        this.depthPixels[colorPixelIndex++] = intensity;  // Write the green byte
                        this.depthPixels[colorPixelIndex++] = intensity;  // Write the red byte

                        // We're otuputting BGR, the last byte in teh 32 bits is unused so skip it 
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    this.depthBitmap.WritePixels(
                        new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                        this.depthPixels,
                        this.depthBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrame event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent backgroun to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton sk in skeletons)
                    {
                        RenderClippedEdges(skeletons, dc);
                        if (sk.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(sk, dc);
                        }
                        else if (sk.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(this.centerPointBrush, 
                                           null, 
                                           this.SkeletonPointToScreen(sk.Position),
                                           BodyCenterThickness, 
                                           BodyCenterThickness);
                        }
                    }
                }
                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">The skeleton to draw</param>
        /// <param name="drawingContext">The drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Joints
            foreach (Joint joint in skeleton.Joints)
            {
                if (Array.Exists(trackedJoints, jointType => jointType == joint.JointType))
                {
                    Brush drawBrush = null;
                    if (joint.TrackingState == JointTrackingState.Tracked)
                        drawBrush = this.trackedJointBrush;
                    else if (joint.TrackingState == JointTrackingState.Inferred)
                        drawBrush = this.inferredJointBrush;

                    if (drawBrush != null)
                        drawingContext.DrawEllipse(drawBrush,
                                                   null,
                                                   this.SkeletonPointToScreen(joint.Position),
                                                   JointThickness,
                                                   JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skeletonPoint">Point to map</param>
        /// <returns>Mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skeletonPoint, 
                                                                                                   DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">Skeleton to draw bones from</param>
        /// <param name="drawingContext">Drawing context to draw to</param>
        /// <param name="jointType1">Joint to start drawing from</param>
        /// <param name="jointType2">Joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // Insanity check, if we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
                return; 

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
                return;

            // We assume all drawn bones are inferred unless BOTH joints are tracked 
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked &&
                joint1.TrackingState == JointTrackingState.Tracked)
                drawPen = this.trackedBonePen;

            drawingContext.DrawLine(drawPen,
                                    this.SkeletonPointToScreen(joint0.Position),
                                    this.SkeletonPointToScreen(joint1.Position));
        }

        private void RenderClippedEdges(Skeleton[] skeletons, DrawingContext dc)
        {
            //throw new NotImplementedException();
        }

        private void nearFieldButton_Click(object sender, RoutedEventArgs e)
        {
            // Try to use near mode if possible 
            try
            {
                if (this.sensor.DepthStream.Range == DepthRange.Default)
                {
                    this.sensor.DepthStream.Range = DepthRange.Near;
                    nearFieldButton.Content = "Turn Off";
                }
                else
                {
                    this.sensor.DepthStream.Range = DepthRange.Default;
                    nearFieldButton.Content = "Turn On";
                }
            }
            catch(InvalidCastException ex){
                this.DebugMsg.Text = "Near field mode: " + ex.Message;
            }
        }

    }
}
