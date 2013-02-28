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
        /// Variables that sets the area around the hand
        /// </summary>
        private int areaTop = -60;
        private int areaBot = 60;
        private int areaLeft = -60;
        private int areaRight = 60;

        bool bool_allFramesReady = true; 
        
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
        SurfaceDetection surfaceDetection = new SurfaceDetection();

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
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                
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

                // Add an event handler to be called whenever there is a color, depth, or skeleton frame data is ready
                //this.sensor.AllFramesReady += sensor_AllFramesReady;
                this.sensor.DepthFrameReady += sensor_DepthFrameReady;
                
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

                NearFieldButton_Click(null, null);
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
        }

        /// <summary>
        /// Initialized the environment to get ready for surface detection. User should be away from the surface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (surfaceDetection.emptyFrame != null)
                return; 

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
                        this.depthImageColor_debug[colorPixelIndex++] = intensity;  // Write the blue byte
                        this.depthImageColor_debug[colorPixelIndex++] = intensity;  // Write the green byte
                        this.depthImageColor_debug[colorPixelIndex++] = intensity;  // Write the red byte

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
                }
            }
            this.sensor.DepthFrameReady -= sensor_DepthFrameReady;
            this.sensor.AllFramesReady += sensor_AllFramesReady;
            bool_allFramesReady = true;
        }

        /// <summary>
        /// Select the surface by user the user's hand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (!bool_allFramesReady)
                return; 

            //get skeleton frame to get the hand point data 
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
            Point jointPoint = new Point();
            bool bool_joint = false;
            if (skeletons.Length != 0)
            {
                foreach (Skeleton sk in skeletons)
                {
                    if (sk.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        jointPoint = SkeletonPointToScreen(sk.Joints[JointType.HandLeft].Position);
                        bool_joint = true;
                    }
                }
            }

            //get frame data for drawing the area around your hand 
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
                    for (int i = 0; i < this.depthImageColor.Length; i++)
                    {
                        this.depthImageColor[i] = 0;
                    }

                    if (bool_joint)
                    {
                        // store area of interest in current frame 
                        surfaceDetection.jointPoint = jointPoint;
                        // hard coded area around the hand, need to fix this 
                        for (int i = areaLeft; i < areaRight; i++)
                        {
                            for (int j = areaTop; j < areaBot; j++)
                            {
                                // changing (x,y) indeces to array index
                                int index = 4 * (640 * ((int)jointPoint.Y + j) + (int)jointPoint.X + i);
                                if ((index) / 4 >= depthImageData.Length || (index)/4 < 0)
                                    continue; 

                                short depth = depthImageData[(index) / 4].Depth;
                                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                                
                                if(index < depthImageColor.Length && index > 0)
                                    if (this.depthImageData[(index) / 4].PlayerIndex == 1)
                                    {
                                        this.depthImageColor[index] = intensity;
                                    }
                                    else
                                    {
                                        this.depthImageColor[index] = intensity;
                                        this.depthImageColor[index+1] = intensity;
                                        this.depthImageColor[index+2] = intensity; 
                                    }
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

        /// <summary>
        /// Render the surface selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sensor_ColorPlaneDepthFrame(object sender, DepthImageFrameReadyEventArgs e)
        {
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
                    for (int i = 0; i < this.depthImageData.Length; ++i)
                    {
                        // Get the depth for this pixel 
                        short depth = depthImageData[i].Depth;
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                        bool surf = this.surfaceMatrix[i] >= 0 ? true : false;
                        if (surf)
                        {
                            this.depthImageColor[colorPixelIndex++] = (byte)(255 - this.surfaceMatrix[i]);  // Write the blue byte
                            this.depthImageColor[colorPixelIndex++] = 0;  // Write the green byte
                            this.depthImageColor[colorPixelIndex++] = 0;  // Write the red byte
                        }
                        else
                        {
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the blue byte
                            this.depthImageColor[colorPixelIndex++] = intensity;  // Write the green byte
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
                }
            }
            //this.sensor.DepthFrameReady -= sensor_ColorPlaneDepthFrame;
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

        private void GetSurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (surfaceDetection == null || surfaceDetection.emptyFrame == null || surfaceDetection.jointPoint == null)
            {
                DebugMsg("emptyFrame or playerFrame is null");
                return;
            }
            this.sensor.DepthFrameReady -= sensor_ColorPlaneDepthFrame;
            Plane surface = surfaceDetection.getSurface();
            this.surfaceMatrix = surfaceDetection.getSurfaceFrame();
            Vector origin = surfaceDetection.origin;
            Vector vA = surfaceDetection.vectorA; 
            Vector vB = surfaceDetection.vectorB;
            Helper.DrawLine(origin.x, (origin.x + vA.x), origin.y, (origin.y + vA.y), Colors.Red, this.canvas_debug);
            Helper.DrawLine(origin.x, (origin.x + vB.x), origin.y, (origin.y + vB.y), Colors.Red, this.canvas_debug);

            DebugMsg("Origin   -- " + origin.ToString());
            DebugMsg("Sample1  -- " + surfaceDetection.sample1.ToString());
            DebugMsg("Sample2  -- " + surfaceDetection.sample2.ToString());
            DebugMsg("VectorA  -- " + vA.ToString());
            DebugMsg("VectorB  -- " + vB.ToString());
            DebugMsg("Surface  -- " + surface.ToString());

            this.sensor.AllFramesReady -= sensor_AllFramesReady;
            bool_allFramesReady = false;
            this.sensor.DepthFrameReady += sensor_ColorPlaneDepthFrame;
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
            catch(InvalidCastException ex){
                DebugMsg("Near field mode: " + ex.Message);
            }
        }

        private void DebugMsg(string msg)
        {
            this.DebugBox.Text = msg + Environment.NewLine + this.DebugBox.Text;
        }
    }
}
