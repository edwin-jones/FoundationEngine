using FoundationEngine.Renderer;
using SharpDX;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = System.Drawing.Color;
using Camera = FoundationEngine.Renderer.Camera;

namespace FoundationEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device device;
        private DateTime previousRenderTime;
        private Double lowestFps = int.MaxValue;
        private Double highestFps = int.MinValue;
        private Mesh[] meshes = new Mesh[]{};
        private Camera camera = new Camera();

        //CTOR
        public MainWindow()
        {
            InitializeComponent();

            //initialize fps counters
            FPSCounterTextBlock.Text = Convert.ToString(0);
            HighFPSCounterTextBlock.Text = Convert.ToString(0);
            LowFPSCounterTextBlock.Text = Convert.ToString(0);
        }

        /// <summary>
        /// The event triggers on page load.
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int width = Device.ViewPortWidth;
            int height = Device.ViewPortHeight;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * height];

            // Initialize the image with data.
            Random value = new Random();
            value.NextBytes(rawImage);


            // Choose the back buffer resolution here
            BitmapSource bmp = BitmapSource.Create(width, height, width, height, pf, null, rawImage, rawStride);

            // Our XAML Image control. We set it's source to a bitmap so we can hack it to be a rendering context.
            FrontBuffer.Source = bmp;

            device = new Device(bmp, FrontBuffer);

            //load mesh(es) from file.
            meshes = FoundationEngine.IO.MeshLoader.LoadJSONFile("resources\\models\\monkey.babylon");

            //position the camera
            camera.Position = new Vector3D(0, 0, 10.0f);
            camera.Target = new Vector3D();

            // Registering to the XAML rendering loop.
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }


        /// <summary>
        /// Rendering loop handler
        /// </summary>
        void CompositionTarget_Rendering(object sender, object e)
        {
            // Fps
            var now = DateTime.Now;
            var currentFps = 1000.0 / (now - previousRenderTime).TotalMilliseconds;
            previousRenderTime = now;
            if (currentFps > highestFps) highestFps = currentFps;
            if (currentFps < lowestFps) lowestFps = currentFps;

            FPSCounterTextBlock.Text = string.Format("{0:0.00}", currentFps);
            LowFPSCounterTextBlock.Text = string.Format("{0:0.00}", lowestFps);
            HighFPSCounterTextBlock.Text = string.Format("{0:0.00}", highestFps);

            device.Clear(Color.Black);

            foreach (var mesh in meshes)
            {
                // rotating slightly the meshes during each frame rendered
                mesh.Rotation = new Vector3D(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            }

            // Doing the various matrix operations
            device.Render(camera, meshes);

            // Flushing the back buffer into the front buffer
            device.Present();
        }
    }
}
