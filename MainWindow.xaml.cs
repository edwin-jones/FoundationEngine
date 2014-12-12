using FoundationEngine.Renderer;
using SharpDX;
using System.Drawing;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;

using Color = System.Drawing.Color;

namespace FoundationEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device device;

      
        Mesh cube;
        Camera mera = new Camera();

        public MainWindow()
        {
            InitializeComponent();

            Vector3[] cubeVerts = { 
                                      new Vector3(-1, 1, 1),
                                      new Vector3(1, 1, 1),
                                      new Vector3(-1, -1, 1),
                                      new Vector3(-1, -1, -1),
                                      new Vector3(-1, 1, -1),
                                      new Vector3(1, 1, -1),
                                      new Vector3(1, -1, 1),
                                      new Vector3(1, -1, -1)
                                  };

            cube = new Mesh("cube", cubeVerts);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int width = 640;
            int height = 480;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * height];

            // Initialize the image with data.
            Random value = new Random();
            value.NextBytes(rawImage);


            // Choose the back buffer resolution here
            BitmapSource bmp = BitmapSource.Create(width, height, width, height, pf, null, rawImage, rawStride);

            // Our XAML Image control
            FrontBuffer.Source = bmp;

            device = new Device(bmp, FrontBuffer);

            mera.Position = new Vector3(0, 0, 10.0f);
            mera.Target = Vector3.Zero;

            // Registering to the XAML rendering loop
            CompositionTarget.Rendering += CompositionTarget_Rendering;            
        }

        // Rendering loop handler
        void CompositionTarget_Rendering(object sender, object e)
        {
            device.Clear(Color.Black);

            // rotating slightly the cube during each frame rendered
            cube.Rotation = new Vector3(cube.Rotation.X + 0.01f, cube.Rotation.Y + 0.01f, cube.Rotation.Z);

            // Doing the various matrix operations
            device.Render(mera, cube);
            // Flushing the back buffer into the front buffer

            device.Present();
        }
    }
}
