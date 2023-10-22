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

        public static Vector2 RotatedBy(this Vector2 vector, float radians)
        {
            Vector2 ret = Vector2.Zero;
            ret.X = (MathF.Cos(radians) * vector.X) - (MathF.Sin(radians) * vector.Y);
            ret.Y = (MathF.Sin(radians) * vector.X) + (MathF.Cos(radians) * vector.Y);
            return ret;
        }

        public static float ToRotation(this Vector2 vector)
        {
            return MathF.Atan2(vector.Y, vector.X);
        }
    }
}
