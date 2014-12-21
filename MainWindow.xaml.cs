using FoundationEngine.Renderer;
using SharpDX;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        //CTOR
        public MainWindow()
        {
            InitializeComponent();

            var mesh = new Mesh("Cube", 8, 12);
            mesh.Vertices[0] = new Vector3(-1, 1, 1);
            mesh.Vertices[1] = new Vector3(1, 1, 1);
            mesh.Vertices[2] = new Vector3(-1, -1, 1);
            mesh.Vertices[3] = new Vector3(1, -1, 1);
            mesh.Vertices[4] = new Vector3(-1, 1, -1);
            mesh.Vertices[5] = new Vector3(1, 1, -1);
            mesh.Vertices[6] = new Vector3(1, -1, -1);
            mesh.Vertices[7] = new Vector3(-1, -1, -1);

            mesh.Faces[0] = new Face { A = 0, B = 1, C = 2 };
            mesh.Faces[1] = new Face { A = 1, B = 2, C = 3 };
            mesh.Faces[2] = new Face { A = 1, B = 3, C = 6 };
            mesh.Faces[3] = new Face { A = 1, B = 5, C = 6 };
            mesh.Faces[4] = new Face { A = 0, B = 1, C = 4 };
            mesh.Faces[5] = new Face { A = 1, B = 4, C = 5 };

            mesh.Faces[6] = new Face { A = 2, B = 3, C = 7 };
            mesh.Faces[7] = new Face { A = 3, B = 6, C = 7 };
            mesh.Faces[8] = new Face { A = 0, B = 2, C = 7 };
            mesh.Faces[9] = new Face { A = 0, B = 4, C = 7 };
            mesh.Faces[10] = new Face { A = 4, B = 5, C = 6 };
            mesh.Faces[11] = new Face { A = 4, B = 6, C = 7 };

            cube = mesh;
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

            mera.Position = new Vector3(0, 0, 10.0f);
            mera.Target = Vector3.Zero;

            // Registering to the XAML rendering loop. This function should get called 60 times a second on a normal monitor.
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// Rendering loop handler
        /// </summary>
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
