using Aardvark.Base;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Helpers
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

        public static Vector2 ToRotationVector2(this float val)
        {
            return Vector2.UnitX.RotatedBy(val);
        }

        public static float Distance(this Vector2 vector1, Vector2 vector2)
        {
            float x = MathF.Abs(vector1.X - vector2.X);
            float y = MathF.Abs(vector1.Y - vector2.Y);
            return MathF.Sqrt((x * x) + (y * y));
        }

        public static Vector2 Wrap(this Vector2 vector, float width, float height)
        {
            float x = vector.X;
            float y = vector.Y;

            while (x < 0)
                x += width;
            while (y < 0)
                y += height;

            x %= width;
            y %= height;
            return new Vector2 (x, y);
        }

        public static void WrapPoints(ref float x, ref float y, float width, float height)
        {
            while (x < 0)
                x += width;
            while (y < 0)
                y += height;

            x %= width;
            y %= height;
        }

        public static void WrapPoints(ref int x, ref int y, int width, int height)
        {
            while (x < 0)
                x += width;
            while (y < 0)
                y += height;

            x %= width;
            y %= height;
        }

        public static V2d ToV2d(this Vector2 vector)
        {
            return new V2d(vector.X, vector.Y);
        }

        public static Vector2 ToVector2(this V2d v)
        {
            return new Vector2((float)v.X, (float)v.Y);
        }
    }
}
