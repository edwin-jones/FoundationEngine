using SharpDX;
using System;
using System.Linq;
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
        private readonly float[] depthBuffer;

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

            depthBuffer = new float[bmp.PixelWidth * bmp.PixelHeight];
        }

        // Clamping values to keep them between 0 and 1
        float Clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        // Interpolating the value between 2 vertices 
        // min is the starting point, max the ending point
        // and gradient the % between the 2 points
        float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
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

            // Clearing Depth Buffer
            for (var index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
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

        // Called to put a pixel on screen at a specific X,Y coordinates
        public void PutPixel(int x, int y, float z, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = (x + y * Device.ViewPortWidth);
            var index4 = index * 4;

            if (depthBuffer[index] < z)
            {
                return; // Discard
            }

            depthBuffer[index] = z;

            backBuffer[index4] = (byte)(color.Blue * 255);
            backBuffer[index4 + 1] = (byte)(color.Green * 255);
            backBuffer[index4 + 2] = (byte)(color.Red * 255);
            backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
        }

        // Project takes some 3D coordinates and transform them
        // in 2D coordinates using the transformation matrix
        public Vector3 Project(Vector3 coord, SharpDX.Matrix transMat)
        {
            // transforming the coordinates
            var point = Vector3.TransformCoordinate(coord, transMat);
            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
            var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
            return (new Vector3(x, y, point.Z));
        }

        // DrawPoint calls PutPixel but does the clipping operation before
        public void DrawPoint(Vector3 point, Color4 color)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            {
                // Drawing a point
                PutPixel((int)point.X, (int)point.Y, point.Z, color);
            }
        }

        // drawing line between 2 points from left to right
        // papb -> pcpd
        // pa, pb, pc, pd must then be sorted before
        void ProcessScanLine(int y, Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd, Color4 color)
        {
            // Thanks to current Y, we can compute the gradient to compute others values like
            // the starting X (sx) and ending X (ex) to draw between
            // if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1
            var gradient1 = pa.Y != pb.Y ? (y - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (y - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int)Interpolate(pa.X, pb.X, gradient1);
            int ex = (int)Interpolate(pc.X, pd.X, gradient2);

            // starting Z & ending Z
            float z1 = Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = Interpolate(pc.Z, pd.Z, gradient2);

            // drawing a line from left (sx) to right (ex) 
            for (var x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float)(ex - sx);

                var z = Interpolate(z1, z2, gradient);
                DrawPoint(new Vector3(x, y, z), color);
            }
        }

        public void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color4 color)
        {
            // Sorting the points in order to always have this order on screen p1, p2 & p3
            // with p1 always up (thus having the Y the lowest possible to be near the top screen)
            // then p2 between p1 & p3
            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            if (p2.Y > p3.Y)
            {
                var temp = p2;
                p2 = p3;
                p3 = temp;
            }

            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            // inverse slopes
            float dP1P2, dP1P3;

            // http://en.wikipedia.org/wiki/Slope
            // Computing inverse slopes
            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

            // First case where triangles are like that:
            // P1
            // -
            // -- 
            // - -
            // -  -
            // -   - P2
            // -  -
            // - -
            // -
            // P3
            if (dP1P2 > dP1P3)
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                    {
                        ProcessScanLine(y, p1, p3, p1, p2, color);
                    }
                    else
                    {
                        ProcessScanLine(y, p1, p3, p2, p3, color);
                    }
                }
            }
            // First case where triangles are like that:
            //       P1
            //        -
            //       -- 
            //      - -
            //     -  -
            // P2 -   - 
            //     -  -
            //      - -
            //        -
            //       P3
            else
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                    {
                        ProcessScanLine(y, p1, p2, p1, p3, color);
                    }
                    else
                    {
                        ProcessScanLine(y, p2, p3, p1, p3, color);
                    }
                }
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

                var faceIndex = 0;

                foreach (var face in mesh.Faces)
                {
                    DrawFace(mesh, face, transformMatrix, ref faceIndex);
                }
            }
        }
        Object lockObject = new Object();
        void DrawFace(Mesh mesh, Face face, SharpDX.Matrix transformMatrix, ref Int32 faceIndex)
        {
            var vertexA = mesh.Vertices[face.A];
            var vertexB = mesh.Vertices[face.B];
            var vertexC = mesh.Vertices[face.C];

            var pixelA = Project(vertexA, transformMatrix);
            var pixelB = Project(vertexB, transformMatrix);
            var pixelC = Project(vertexC, transformMatrix);

            var color = 0.25f + (faceIndex % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
            DrawTriangle(pixelA, pixelB, pixelC, new Color4(color, color, color, 1));


            faceIndex++;
            
        }
    }
}
