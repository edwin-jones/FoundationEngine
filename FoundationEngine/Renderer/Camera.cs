using System.Windows.Media.Media3D;

namespace FoundationEngine.Renderer
{
    /// <summary>
    /// This class represents a camera, or view of the 3D scene.
    /// </summary>
    public class Camera
    {
        public Vector3D Position { get; set; }
        public Vector3D Target { get; set; }
    }  
}
