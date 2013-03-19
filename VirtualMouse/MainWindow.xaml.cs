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
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Forms;

namespace VirtualMouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 
        /// </summary>
        User user = new User();

        // finger stuff
        System.Drawing.Point oldMousePos;
        GestureController gestureController = new GestureController();

        /// <summary>
        /// Boolean variables to help make event handler more robust
        /// </summary>
        private bool b_InitializeEnvironment = false;
        private bool b_ColorPlaneDepthFrame = false;
        private bool b_ActionSurfaceConfirmed = false;

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
        /// Bitmaps that will hold depth info
        /// </summary>
        private WriteableBitmap depthBitmap;
        private WriteableBitmap depthBitmap_debug;
        /// <summary>
        /// Buffers for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthImageData;

        /// <summary>
        /// Byte array that actually stores the color to draw
        /// </summary>
        private byte[] depthImageColor;
        private byte[] depthImageColor_debug;

        /// <summary>
        /// Surface detection object that handles all the detection methods
        /// </summary>
        private SurfaceDetection surfaceDetection = new SurfaceDetection();

        /// <summary>
        /// Finger tracking object
        /// </summary>
        private FingerTracking fingerTracking = new FingerTracking();

        /// <summary>
        /// Binary matrix for the surface
        /// </summary>
        private int[] surfaceMatrix;

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
                // Kinect settings
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.DepthStream.Range = DepthRange.Near;

                this.sensor.SkeletonStream.Enable(
                    new TransformSmoothParameters()
                    {
                        Smoothing = 0.5f,
                        Correction = 0.1f,
                        Prediction = 0.5f,
                        JitterRadius = 0.1f,
                        MaxDeviationRadius = 0.1f
                    });
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                this.sensor.SkeletonStream.EnableTrackingInNearRange = true;

                // Set up GestureController
                this.gestureController.GestureReady += gestureController_GestureReady;
                this.oldMousePos = System.Windows.Forms.Cursor.Position;
                Console.WriteLine("Old Mouse: " + oldMousePos);

                // Set up ActionArea
                this.actionArea.maxLength = this.sensor.DepthStream.FramePixelDataLength;
                this.actionArea.ConfirmCallBack += actionArea_ConfirmCallBack;

                // Allocate space to put the pixels we'll receive
                this.depthImageData = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.depthImageColor = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.depthImageColor_debug = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.surfaceMatrix = new int[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];

                // Initialize the bitmap we'll display on-screen 
                this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                                                       this.sensor.DepthStream.FrameHeight,
                                                       96.0,
                                                       96.0,
                                                       PixelFormats.Bgr32,
                                                       null);
                this.depthBitmap_debug = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                                                       this.sensor.DepthStream.FrameHeight,
                                                       96.0,
                                                       96.0,
                                                       PixelFormats.Bgr32,
                                                       null);

                // Set the depth image we display to point to the bitmap where we'll put the image data
                this.depthImage.Source = this.depthBitmap;
                this.depthImage_debug.Source = this.depthBitmap_debug;
                // Start the sensor
                try
                {
                    this.sensor.Start();
                }
                catch (IOException ex)
                {
                    this.sensor = null;
                    DebugMsg(ex.Message);
                }


                Plane surface = Helper.LoadSurface();
                if (surface != null)
                {
                    surfaceDetection.surface = surface;
                    DebugMsg("Save surface settings loaded");
                    DebugMsg(surface.ToString());
                    b_ColorPlaneDepthFrame = true;
                    this.sensor.DepthFrameReady += ColorPlaneDepthFrame;
                }
                else
                {
                    // Add an event handler to be called whenever there is a color, depth, or skeleton frame data is ready
                    b_InitializeEnvironment = true;
                    this.sensor.DepthFrameReady += InitializeEnvironment;
                }
            }

            if (this.sensor == null)
            {
                DebugMsg("No Kinect ready. Please plug in a kinect");
            }
        }

        void gestureController_GestureReady(System.Drawing.Point pt)
        {
            System.Windows.Forms.Cursor.Position = pt;
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
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// DepthFrameReady event handler to initialize the environment to get ready for surface detection. 
        /// User should be away from the surface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InitializeEnvironment(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (!b_InitializeEnvironment)
            {
                this.sensor.DepthFrameReady -= InitializeEnvironment;
                DebugMsg("Blocked InitializeEnvironment");
                return;
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    surfaceDetection.emptyFrame = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                    depthFrame.CopyDepthImagePixelDataTo(surfaceDetection.emptyFrame);
                    DebugMsg("Background depth frame captured");

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB 
                    int colorPixelIndex = 0;
                    for (int i = 0; i < this.sensor.DepthStream.FramePixelDataLength; ++i)
                    {
                        // Get the depth for this pixel 
                        short depth = surfaceDetection.emptyFrame[i].Depth;
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                        this.depthImageColor_debug[colorPixelIndex++] = intensity; // Write the blue byte
                        this.depthImageColor_debug[colorPixelIndex++] = intensity; // Write the green byte
                        this.depthImageColor_debug[colorPixelIndex++] = intensity; // Write the red byte

                        // We're otuputting BGR, the last byte in teh 32 bits is unused so skip it 
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    this.depthBitmap_debug.WritePixels(
                        new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                        this.depthImageColor_debug,
                        this.depthBitmap.PixelWidth * sizeof(int),
                        0);

                    DebugMsg("Un-hook InitializeEnvironment");
                    this.sensor.DepthFrameReady -= InitializeEnvironment;
                    b_InitializeEnvironment = false;
                    DebugMsg("Hook  up DefineSurface");
                }
            }
        }

        /// <summary>
        /// Render the surface selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorPlaneDepthFrame(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (!b_ColorPlaneDepthFrame)
            {
                this.sensor.DepthFrameReady -= ColorPlaneDepthFrame;
                DebugMsg("Blocked ColorPlaneDepthFrame");
                return;
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temp array 
                    depthFrame.CopyDepthImagePixelDataTo(this.depthImageData);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB 
                    int colorPixelIndex = 0;

                    double percentDiff;
                    short depth;
                    byte intensity;

                    // Get the binary array to indicate whether a pixel is part of the hand
                    bool[] binaryArray = new bool[depthImageData.Length];

                    for (int i = 0; i < this.depthImageData.Length; ++i)
                    {
                        // Get the depth for this pixel 
                        depth = depthImageData[i].Depth;
                        intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        if (this.actionArea.ValidIndeces[i] == 1)
                        {
                            percentDiff = Math.Abs(2 * this.surfaceDetection.emptyFrame[i].Depth - depth + 0.0001) /
                                                     (this.surfaceDetection.emptyFrame[i].Depth + 0.0001);

                            if (percentDiff > 1.008) // sketchy numbers... need to tweek 
                                // Is the hand
                                binaryArray[i] = true;

                            this.depthImageColor[colorPixelIndex++] = 0;
                            this.depthImageColor[colorPixelIndex++] = 100;
                            this.depthImageColor[colorPixelIndex++] = 0;
                        }
                        else
                        {
                            // Not within the action area
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the blue byte
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the green byte
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the red byte
                        }
                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it 
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Get the smallest rectangle that contains the actionArea
                    Point topLeft = actionArea.cornerPoints[(int)ActionArea.corners.topLeft];
                    Point topRight = actionArea.cornerPoints[(int)ActionArea.corners.topRight];
                    Point botLeft = actionArea.cornerPoints[(int)ActionArea.corners.botLeft];
                    Point botRight = actionArea.cornerPoints[(int)ActionArea.corners.botRight];
                    
                    double minX = Math.Max(0, Math.Min(topLeft.X, botLeft.X));
                    double maxX = Math.Min((RenderWidth / 2 - 1), Math.Max(topRight.X, botRight.X));
                    double minY = Math.Max(0, Math.Min(topLeft.Y, topRight.Y));
                    double maxY = Math.Min((RenderHeight / 2 - 1), Math.Min(botLeft.Y, botRight.Y));
                    
                    Hand hand = fingerTracking.parseBinArray(binaryArray, minX, minY, maxX, maxY);
                    
                    // Highlight contour
                    List<Point> contourPoints = hand.contourPoints;
                    List<Point> fingers = hand.fingertips;
                    Point palm = hand.palm;
                    if (fingerTracking.hasPalm)//contourPoints.Count != 0 && fingers.Count != 0 && fingerTracking.hasPalm())
                    {
                        // Highlight contour
                        foreach (Point p in contourPoints)
                        {
                            int index = 4 * Helper.Point2Index(new Point(p.X, p.Y));
                            this.depthImageColor[index] = 255;
                            this.depthImageColor[index + 1] = 255;
                            this.depthImageColor[index + 2] = 255;
                        }
                        double distance;
                        int fingerIndex;
                        byte[] fingerColors = { 0, 0, 0 };
                        int coloringRange = 3;
                        double xMultiplier = Screen.PrimaryScreen.Bounds.Height / (maxX - minX);
                        double yMultiplier = Screen.PrimaryScreen.Bounds.Height / (maxY - minY);
                        // Highlight fingers
                        foreach (Point finger in fingers)
                        {
                            fingerIndex = Helper.Point2Index(finger);
                            depth = depthImageData[fingerIndex].Depth;
                            distance = this.surfaceDetection.surface.DistanceToPoint(finger.X, finger.Y, (double)depth);
                            if (distance < 18)
                            {
                                // fingers are touching the surface
                                fingerColors[0] = 0;
                                fingerColors[1] = 0;
                                fingerColors[2] = 255;

                                if (fingers.Count == 1)
                                {

                                    int posX = (int)((finger.X - minX * 2) * xMultiplier * 1.1);
                                    int posY = (int)((maxY * 2 - finger.Y) * yMultiplier * 1.1);

                                    gestureController.Add2Buffer(new System.Drawing.Point(posX, posY));
                                }
                            }
                            else
                            {
                                // fingers aren't touching the surface
                                fingerColors[0] = 0;
                                fingerColors[1] = 255;
                                fingerColors[2] = 0;

                                gestureController.MouseDownPos = System.Windows.Forms.Cursor.Position;
                                gestureController.ResetBuffer();
                            }
                            for (int i = -coloringRange; i < coloringRange; i++)
                            {
                                for (int j = -coloringRange; j < coloringRange; j++)
                                {
                                    fingerIndex = 4 * Helper.Point2Index(new Point(finger.X + i, finger.Y + j));
                                    this.depthImageColor[fingerIndex] = fingerColors[0];
                                    this.depthImageColor[fingerIndex + 1] = fingerColors[1];
                                    this.depthImageColor[fingerIndex + 2] = fingerColors[2];
                                }
                            }
                        }

                        // Highlight palm
                        for (int i = -coloringRange; i < coloringRange; i++)
                        {
                            for (int j = -coloringRange; j < coloringRange; j++)
                            {
                                int index = 4 * Helper.Point2Index(new Point(palm.X + i, palm.Y + j));
                                this.depthImageColor[index] = 0;
                                this.depthImageColor[index + 1] = 0;
                                this.depthImageColor[index + 2] = 255;
                            }
                        }
                    }

                    // Write the pixel data into our bitmap
                    this.depthBitmap.WritePixels(
                        new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                        this.depthImageColor,
                        this.depthBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void DefineSurface(Point point)
        {
            if (this.surfaceDetection.emptyFrame == null || this.actionArea.ValidIndeces == null)
            {
                DebugMsg("Initialize Environment First");
                return;
            }

            b_ColorPlaneDepthFrame = false;
            this.sensor.DepthFrameReady -= ColorPlaneDepthFrame;

            point.X *= 2;
            point.Y *= 2;
            DebugMsg("X: " + point.X + " Y: " + point.Y);

            surfaceDetection.definitionPoint = point;

            Point topRight = actionArea.cornerPoints[(int)ActionArea.corners.topRight];
            Point botLeft = actionArea.cornerPoints[(int)ActionArea.corners.botLeft];
            Point botRight = actionArea.cornerPoints[(int)ActionArea.corners.botRight];
            double maxX = Math.Min((Width / 2 - 1), Math.Max(topRight.X, botRight.X));
            double maxY = Math.Min((Height / 2 - 1), Math.Min(botLeft.Y, botRight.Y));
            Plane surface = surfaceDetection.getSurface((int)maxX, (int)maxY);
            if (surface == null)
            {
                DebugMsg("Unknown depth. Please try another point");
                return;
            }
            Helper.SaveSurface(surface);
            DebugMsg("***************************************");
            DebugMsg("Origin   -- " + surfaceDetection.origin.ToString());
            DebugMsg("Sample1  -- " + surfaceDetection.sample1.ToString());
            DebugMsg("Sample2  -- " + surfaceDetection.sample2.ToString());
            DebugMsg("VectorA  -- " + surfaceDetection.vectorA.ToString());
            DebugMsg("VectorB  -- " + surfaceDetection.vectorB.ToString());
            DebugMsg("Surface  -- " + surface.ToString());
            DebugMsg("***************************************");

            b_ColorPlaneDepthFrame = true;
            this.sensor.DepthFrameReady += ColorPlaneDepthFrame;
        }



        /*****************************************************************************************************
         * 
         * Button Click Listeners Below  
         * 
         ******************************************************************************************************/


        private void InitializeEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.sensor == null)
                return;

            this.actionArea.Visibility = System.Windows.Visibility.Visible;

            b_ColorPlaneDepthFrame = false;
            this.sensor.DepthFrameReady -= ColorPlaneDepthFrame;
            b_InitializeEnvironment = true;
            this.sensor.DepthFrameReady += InitializeEnvironment;
        }


        private void NearFieldButton_Click(object sender, RoutedEventArgs e)
        {
            // Try to use near mode if possible 
            try
            {
                if (this.sensor.DepthStream.Range == DepthRange.Default)
                {
                    this.sensor.SkeletonStream.EnableTrackingInNearRange = true;
                    this.sensor.DepthStream.Range = DepthRange.Near;
                    nearFieldButton.Content = "Turn Off";
                }
                else
                {
                    this.sensor.SkeletonStream.EnableTrackingInNearRange = false;
                    this.sensor.DepthStream.Range = DepthRange.Default;
                    nearFieldButton.Content = "Turn On";
                }
            }
            catch (InvalidCastException ex)
            {
                DebugMsg("Near field mode: " + ex.Message);
            }
        }

        void actionArea_ConfirmCallBack()
        {
            b_ActionSurfaceConfirmed = true;
        }

        private void canvas_debug_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (b_ActionSurfaceConfirmed)
            {
                Point point = Mouse.GetPosition(canvas_debug);
                DefineSurface(point);
                this.actionArea.Visibility = System.Windows.Visibility.Collapsed;
            }
            b_ActionSurfaceConfirmed = false;
        }

        private void DebugMsg(string msg)
        {
            this.DebugBox.Text = msg + Environment.NewLine + this.DebugBox.Text;
        }
    }
}

