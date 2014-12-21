using SharpDX;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace FoundationEngine.Renderer
{
    /// <summary>
    /// This class represents a 3d rendering device. It is the most important part of the renderer.
    /// </summary>
    class Device
    {
        public const Int32 ViewPortWidth = 640;
        public const Int32 ViewPortHeight = 480;
        public const Int32 ViewPortDPI = 96;

        private byte[] backBuffer;
        private BitmapSource bmp;
        private System.Windows.Controls.Image renderContext;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="renderContext"></param>
        public Device(BitmapSource bmp, System.Windows.Controls.Image renderContext)
        {
            this.bmp = bmp;
            this.renderContext = renderContext;
            // the back buffer size is equal to the number of pixels to draw
            // on screen (width*height) * 4 (R,G,B & Alpha values). 
            backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
        }

        /// <summary>
        /// This method is called to clear the back buffer with a specific color
        /// </summary>
        /// <param name="color"></param>
        public void Clear(System.Drawing.Color color)
        {
            for (var index = 0; index < backBuffer.Length; index += 4)
            {
                // BGRA is used by Windows instead by RGBA in HTML5
                backBuffer[index] = color.B;
                backBuffer[index + 1] = color.G;
                backBuffer[index + 2] = color.R;
                backBuffer[index + 3] = color.A;
            }
        }

        /// <summary>
        /// Once everything is ready, we can flush the back buffer into the front buffer. 
        /// </summary>
        public void Present()
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pixelFormat = PixelFormats.Bgr32;
            int width = ViewPortWidth;
            int height = ViewPortHeight;
            int rawStride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * height];

            // Create a BitmapSource.
            bmp = BitmapSource.Create(width, height, ViewPortDPI, ViewPortDPI, pixelFormat, null, backBuffer, rawStride);

            //set the rendercontext source to the newly rendered bitmap.
            renderContext.Source = bmp;
        }

        /// <summary>
        /// Called to put a pixel on screen at a specific X,Y coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void PutPixel(int x, int y, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = (x + y * bmp.PixelWidth) * 4;

            backBuffer[index] = (byte)(color.Blue * 255);
            backBuffer[index + 1] = (byte)(color.Green * 255);
            backBuffer[index + 2] = (byte)(color.Red * 255);
            backBuffer[index + 3] = (byte)(color.Alpha * 255);
        }

        /// <summary>
        /// Project takes some 3D coordinates and transform them in 2D coordinates using the transformation matrix
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="transMat"></param>
        /// <returns></returns>
        public Vector2 Project(Vector3 coord, SharpDX.Matrix transMat)
        {
            // transforming the coordinates
            var point = Vector3.TransformCoordinate(coord, transMat);
            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
            var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
            return (new Vector2(x, y));
        }

        // DrawPoint calls PutPixel but does the clipping operation before
        public void DrawPoint(Vector2 point)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            {
                // Drawing a yellow point
                PutPixel((int)point.X, (int)point.Y, new Color4(1.0f, 1.0f, 0.0f, 1.0f));
            }
        }

        /// <summary>
        /// Drawline calculates where a line should be and calls drawpoint for each part of the line. Useful for drawing lines between verts.
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        public void DrawLine(Vector2 point0, Vector2 point1)
        {
            var dist = (point1 - point0).Length();

            // If the distance between the 2 points is less than 2 pixels
            // We're exiting
            if (dist < 2)
                return;

            // Find the middle point between first & second point
            Vector2 middlePoint = point0 + (point1 - point0) / 2;

            // We draw this point on screen
            DrawPoint(middlePoint);

            // Recursive algorithm launched between first & middle point
            // and between middle & second point
            DrawLine(point0, middlePoint);
            DrawLine(middlePoint, point1);
        }

        /// <summary>
        /// improved drawline using Bresenham's line algorithm.
        /// </summary>
        public void DrawBLine(Vector2 point0, Vector2 point1)
        {
            int x0 = (int)point0.X;
            int y0 = (int)point0.Y;
            int x1 = (int)point1.X;
            int y1 = (int)point1.Y;

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = (x0 < x1) ? 1 : -1;
            var sy = (y0 < y1) ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                DrawPoint(new Vector2(x0, y0));

                if ((x0 == x1) && (y0 == y1)) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>
        ///  The main method of the engine that re-compute each vertex projection during each frame
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="meshes"></param>
        public void Render(Camera camera, params Mesh[] meshes)
        {
            // To understand this part, please read the prerequisites resources
            var viewMatrix = SharpDX.Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = SharpDX.Matrix.PerspectiveFovRH(0.78f,
                                                           (float)bmp.PixelWidth / bmp.PixelHeight,
                                                           0.01f, 1.0f);

            foreach (Mesh mesh in meshes)
            {
                // Beware to apply rotation before translation 
                var worldMatrix = SharpDX.Matrix.RotationYawPitchRoll(mesh.Rotation.Y,
                                                              mesh.Rotation.X, mesh.Rotation.Z) *
                                  SharpDX.Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var face in mesh.Faces)
                {
                    var vertexA = mesh.Vertices[face.A];
                    var vertexB = mesh.Vertices[face.B];
                    var vertexC = mesh.Vertices[face.C];

                    var pixelA = Project(vertexA, transformMatrix);
                    var pixelB = Project(vertexB, transformMatrix);
                    var pixelC = Project(vertexC, transformMatrix);

                    DrawBLine(pixelA, pixelB);
                    DrawBLine(pixelB, pixelC);
                    DrawBLine(pixelC, pixelA);
                }
            }
        }
    }
}
