using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoundationEngine.Math
{
    /// <summary>
    /// A custom vector 3 value type we can use so we don't have to rely on 3rd party libraries.
    /// </summary>
    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public static Vector3 Zero = new Vector3();

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

    }
}
