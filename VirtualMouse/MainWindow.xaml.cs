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

        /// <summary>
        /// Variables that sets the area around the hand
        /// </summary>
        private int areaTop = -100;
        private int areaBot = 100;
        private int areaLeft = -100;
        private int areaRight = 100;

        /// <summary>
        /// Boolean variables to help make event handler more robust
        /// </summary>
        private bool b_InitializeEnvironment = false;
        private bool b_DefineSurface = false;
        private bool b_ColorPlaneDepthFrame = false;
        private bool b_TrackFinger = false;

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
        /// Bitmaps that will hold depth info for finger tracking
        /// </summary>
        private WriteableBitmap fingerBitmap;
        
        /// <summary>
        /// Byte array that actually stores the color to draw for finger tracking
        /// </summary>
        private byte[] fingerImageColor;

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
                    new TransformSmoothParameters(){
                        Smoothing = 0.5f,
                        Correction = 0.1f,
                        Prediction = 0.5f,
                        JitterRadius = 0.1f,
                        MaxDeviationRadius = 0.1f
                    });
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                this.sensor.SkeletonStream.EnableTrackingInNearRange = true;
                
                // Allocate space to put the pixels we'll receive
                this.depthImageData = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.depthImageColor = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.depthImageColor_debug = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.fingerImageColor = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
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
                this.fingerBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                                                       this.sensor.DepthStream.FrameHeight,
                                                       96.0,
                                                       96.0,
                                                       PixelFormats.Bgr32,
                                                       null);
                
                // Set the depth image we display to point to the bitmap where we'll put the image data
                this.depthImage.Source = this.depthBitmap;
                this.depthImage_debug.Source = this.depthBitmap_debug;
                this.fingerImage.Source = this.fingerBitmap;
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
                    this.sensor.AllFramesReady += ColorPlaneDepthFrame;
                }
                else
                {
                    // Add an event handler to be called whenever there is a color, depth, or skeleton frame data is ready
                    b_InitializeEnvironment = true;
                    this.sensor.DepthFrameReady += InitializeEnvironment;
                }
                // NOTE: Comment this out when testing surface
                // Add a event handler to perform finger tracking
                //b_TrackFinger = true;
                //this.sensor.AllFramesReady += TrackFingers;
            }

            if (this.sensor == null)
            {
                DebugMsg("No Kinect ready. Please plug in a kinect");
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
                        byte intensity = (byte) (depth >= minDepth && depth <= maxDepth ? depth : 0);
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
                        this.depthBitmap.PixelWidth*sizeof (int),
                        0);

                    DebugMsg("Un-hook InitializeEnvironment");
                    this.sensor.DepthFrameReady -= InitializeEnvironment;
                    b_InitializeEnvironment = false;
                    DebugMsg("Hook  up DefineSurface");
                    this.sensor.SkeletonFrameReady += DefineSurface;
                    b_DefineSurface = true;

                    this.initEnvButton.Visibility = System.Windows.Visibility.Collapsed;
                    this.defineSurfaceButton.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// All frame ready to track fingers (Only identify the contour for now)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void TrackFingers(object sender, AllFramesReadyEventArgs e)
        {
            if (!b_TrackFinger)
            {
                this.sensor.AllFramesReady -= TrackFingers;
                DebugMsg("Blocked TrackFingers");
                return;
            }

            Point handPosition = new Point();
            bool b_joint = false;

            //get skeleton frame to get the hand point data 
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton sk in skeletons)
                    {
                        if (sk.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            handPosition = SkeletonPointToScreen(sk.Joints[JointType.HandLeft].Position);
                            b_joint = true;
//                            Canvas.SetTop(this.indicator, (handPosition.Y + this.indicator.Height) / 2);
//                            Canvas.SetLeft(this.indicator, (handPosition.X + this.indicator.Width) / 2);
                        }
                    }
                }
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

                    // Initialize depth image color
                    for (int i = 0; i < this.fingerImageColor.Length; ++i)
                    {
                        this.fingerImageColor[i] = 0;
                    }

                    bool[] binaryArray = new bool[depthImageData.Length];
                    int binArrIndex = 0;
                    if (b_joint)
                    {
                        // hard coded area around the hand, need to fix this 
                        for (int i = areaLeft; i < areaRight; i++)
                        {
                            for (int j = areaTop; j < areaBot; j++)
                            {
                                // changing (x,y) indeces to array index
                                int index = 4 * (640 * ((int)handPosition.Y + j) + (int)handPosition.X + i);
                                if ((index) / 4 >= depthImageData.Length || (index) / 4 < 0)
                                    continue;

                                if (index < fingerImageColor.Length && index > 0)
                                    if (this.depthImageData[(index) / 4].PlayerIndex > 0)
                                        binaryArray[binArrIndex++] = true;
                                    else
                                        binaryArray[binArrIndex++] = false;
                            }
                        }
                    }

                    fingerTracking.parseBinArray(binaryArray);
                    for (int i = areaLeft; i < areaRight; i++)
                    {
                        for (int j = areaTop; j < areaBot; j++)
                        {
                            // changing (x,y) indices to array index
                            int index = 4 * (640 * ((int)handPosition.Y + j) + (int)handPosition.X + i);
                            if ((index) / 4 >= depthImageData.Length || (index) / 4 < 0)
                                continue;

                            short depth = depthImageData[(index) / 4].Depth;
                            byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                            if (index < fingerImageColor.Length && index > 0)
                                if (this.depthImageData[(index) / 4].PlayerIndex > 0 && fingerTracking.isContour(i - areaLeft, j - areaTop))
                                    fingerImageColor[index + 1] = intensity;
                        }
                    }

                    // Write the pixel data into our bitmap
                    this.fingerBitmap.WritePixels(
                        new Int32Rect(0, 0, this.fingerBitmap.PixelWidth, this.fingerBitmap.PixelHeight),
                        this.fingerImageColor,
                        this.fingerBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// SkeletonFrameReady event handler to define a surface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DefineSurface(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!b_DefineSurface)
            {
                this.sensor.SkeletonFrameReady -= DefineSurface;
                DebugMsg("Blocked DefineSurface");
                return;
            }

            //get skeleton frame to get the hand point data 
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }

                if (skeletons.Length != 0)
                {
                    short playerIndex = 1;
                    foreach (Skeleton sk in skeletons)
                    {
                        if (sk.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            user.playerIndex = playerIndex;
                            user.trackingId = sk.TrackingId;
                            Point pointer = SkeletonPointToScreen(sk.Joints[JointType.HandLeft].Position);
                            surfaceDetection.definitionPoint = pointer;
                            Canvas.SetTop(this.indicator, (pointer.Y + this.indicator.Height) / 2);
                            Canvas.SetLeft(this.indicator, (pointer.X + this.indicator.Width) / 2);
                        }
                        playerIndex++;
                    }
                }
            }
        }

        /// <summary>
        /// Render the surface selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorPlaneDepthFrame(object sender, AllFramesReadyEventArgs e)
        {
            if (!b_ColorPlaneDepthFrame)
            {
                this.sensor.AllFramesReady -= ColorPlaneDepthFrame;
                DebugMsg("Blocked ColorPlaneDepthFrame");
                return;
            }

            //get skeleton frame to get the hand point data 
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }

                if (skeletons.Length != 0)
                {
                    for (int i = 0; i < skeletons.Length; i++)
                    {
                        if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                        {
                            user.playerIndex = (short)(i + 1);
                            user.trackingId = skeletons[i].TrackingId;
                        }
                    }
                }
            }

            if (user.playerIndex == 0)
                return; 

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
                    double x, y, distance;
                    for (int i = 0; i < this.depthImageData.Length; ++i)
                    {
                        // Get the depth for this pixel 
                        short depth = depthImageData[i].Depth;
                        byte intensity = (byte) (depth >= minDepth && depth <= maxDepth ? depth : 0);

                        //bool surf = this.surfaceMatrix[i] >= 0 ? true : false;
                        if (user.playerIndex == depthImageData[i].PlayerIndex)
                        {
                            // Get x,y,z cordiantes
                            x = i % 640;
                            y = (i - x) / 640;
                            distance = surfaceDetection.surface.DistanceToPoint(x, y, (double)depth);
                            if (distance < 10)
                            {
                                this.depthImageColor[colorPixelIndex++] = intensity;  // Write the blue byte
                                this.depthImageColor[colorPixelIndex++] = 0;  // Write the green byte
                                this.depthImageColor[colorPixelIndex++] = 0;  // Write the red byte
                            }
                            else
                            {
                                this.depthImageColor[colorPixelIndex++] = 0;  // Write the blue byte
                                this.depthImageColor[colorPixelIndex++] = 0;  // Write the green byte
                                this.depthImageColor[colorPixelIndex++] = intensity;  // Write the red byte
                            }

                        }
                        else
                        {
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the red byte
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the red byte
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the red byte
                        }

                        // We're otuputting BGR, the last byte in teh 32 bits is unused so skip it 
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    this.depthBitmap.WritePixels(
                        new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                        this.depthImageColor,
                        this.depthBitmap.PixelWidth * sizeof(int),
                        0);

                    // Comment this out if your computer can run real time 
                    //b_ColorPlaneDepthFrame = false;
                    //this.sensor.DepthFrameReady -= ColorPlaneDepthFrame;
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
                                                                                                   DepthImageFormat
                                                                                                       .Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private void InitializeEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            b_ColorPlaneDepthFrame = false;
            this.sensor.AllFramesReady -= ColorPlaneDepthFrame;
            b_InitializeEnvironment = true;
            this.sensor.DepthFrameReady += InitializeEnvironment;            
        }

        private void DefineSurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (surfaceDetection == null || surfaceDetection.emptyFrame == null || surfaceDetection.definitionPoint == null)
            {
                DebugMsg("emptyFrame or playerFrame is null");
                return;
            }
            // toggle buttons
            this.initEnvButton.Visibility = System.Windows.Visibility.Visible;
            this.defineSurfaceButton.Visibility = System.Windows.Visibility.Collapsed;


            // unhook all event handlers
            b_TrackFinger = false;
            this.sensor.AllFramesReady -= TrackFingers;
            b_DefineSurface = false;
            this.sensor.SkeletonFrameReady -= DefineSurface;
            b_ColorPlaneDepthFrame = false;
            this.sensor.AllFramesReady -= ColorPlaneDepthFrame;
            
            // Compute surface
            Plane surface = surfaceDetection.getSurface();
            Helper.SaveSurface(surface);
            DebugMsg("***************************************");
            DebugMsg("Origin   -- " + surfaceDetection.origin.ToString());
            DebugMsg("Sample1  -- " + surfaceDetection.sample1.ToString());
            DebugMsg("Sample2  -- " + surfaceDetection.sample2.ToString());
            DebugMsg("VectorA  -- " + surfaceDetection.vectorA.ToString());
            DebugMsg("VectorB  -- " + surfaceDetection.vectorB.ToString());
            DebugMsg("Surface  -- " + surface.ToString());
            DebugMsg("***************************************");

            // Hook back up event handler
            b_ColorPlaneDepthFrame = true;
            this.sensor.AllFramesReady += ColorPlaneDepthFrame;
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

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            bool surfaceMode = (string)this.modeButton.Content == "Surface Mode" ? true : false;

            b_ColorPlaneDepthFrame = !surfaceMode;
            b_TrackFinger = surfaceMode;

            if (surfaceMode)
            {
                this.sensor.AllFramesReady -= ColorPlaneDepthFrame;
                this.sensor.AllFramesReady += TrackFingers;
                this.modeButton.Content = "Finger Mode";
            }
            else
            {
                this.sensor.AllFramesReady -= TrackFingers;
                this.sensor.AllFramesReady += ColorPlaneDepthFrame;
                this.modeButton.Content = "Surface Mode";
            }
        }

        private void DebugMsg(string msg)
        {
            this.DebugBox.Text = msg + Environment.NewLine + this.DebugBox.Text;
        }
    }
}

