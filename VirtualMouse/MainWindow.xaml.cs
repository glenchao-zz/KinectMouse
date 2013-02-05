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
        /// Bitmap that will hold color info
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Buffer for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

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
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                // Initialize the bitmap we'll display on-screen 
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                                                       this.sensor.ColorStream.FrameHeight,
                                                       96.0,
                                                       96.0,
                                                       PixelFormats.Bgr32,
                                                       null);
                // Set the image we display to point to the bitmap where we'll put the image data
                this.colorImage.Source = this.colorBitmap;
                // Add an event handler to be called whenever there is a new color frame data
                this.sensor.ColorFrameReady += sensor_ColorFrameReady;

                // Start the sensor
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
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
    }
}
