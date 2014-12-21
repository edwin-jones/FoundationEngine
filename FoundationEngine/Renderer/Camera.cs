using SharpDX;

namespace FoundationEngine.Renderer
{
    /// <summary>
    /// This class represents a camera, or view of the 3D scene.
    /// </summary>
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
    }  
}
