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
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Preps the kinect if one is detected. 
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
                // Turn on the color and depth stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

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
                this.colorImage.Source = this.colorBitmap;
                this.depthImage.Source = this.depthBitmap;

                // Add an event handler to be called whenever there is a new color frame data
                this.sensor.ColorFrameReady += sensor_ColorFrameReady;
                this.sensor.DepthFrameReady += sensor_DepthFrameReady;

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
                
                // Try to use near mode if possible 
                try
                {
                    this.sensor.DepthStream.Range = DepthRange.Near;
                }
                catch (InvalidOperationException ex)
                {
                    this.DebugMsg.Text = "Near field mode: " + ex.Message;
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
        private void WindowColosing(object sender, System.ComponentModel.CancelEventArgs e)
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
        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
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

    }
}
