using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Helpers
{
    internal static class VectorHelper
    {

        public static Vector2 SafeNormalize(this Vector2 vector)
        {
            if (vector == Vector2.Zero)
                return Vector2.Zero;
            vector.Normalize();
            return vector;
        }
    }
}
