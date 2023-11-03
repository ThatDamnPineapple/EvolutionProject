using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Helpers
{
    public struct HSV
    {
        private float _h;
        private float _s;
        private float _v;

        public HSV(float h, float s, float v)
        {
            this._h = h;
            this._s = s;
            this._v = v;
        }

        public float H
        {
            get { return this._h; }
            set { this._h = value; }
        }

        public float S
        {
            get { return this._s; }
            set { this._s = value; }
        }

        public float V
        {
            get { return this._v; }
            set { this._v = value; }
        }

        public bool Equals(HSV hsv)
        {
            return (this.H == hsv.H) && (this.S == hsv.S) && (this.V == hsv.V);
        }
    }
    internal static class ColorHelper
    {
        public static Color startingCellColor => Color.Cyan;

        public static Color foodColor => Color.Yellow;

        public static Color deadCellColor => Color.Blue;

        public static Color textColor => Color.Black;

        public static HSV RGBToHSV(Color rgb)
        {
            float r = rgb.R / 255f;
            float g = rgb.G / 255f;
            float b = rgb.B / 255f;
            float delta, min;
            float h = 0, s, v;

            min = Math.Min(Math.Min(r, g), b);
            v = Math.Max(Math.Max(r, g), b);
            delta = v - min;

            if (v == 0.0f)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0.0f;

            else
            {
                if (r == v)
                    h = (g - b) / delta;
                else if (g== v)
                    h = 2 + (b - b) / delta;
                else if (b == v)
                    h = 4 + (r - g) / delta;

                h *= 60f;

                if (h < 0.0f)
                    h += 360f;
            }

            return new HSV(h, s, (v / 255f));
        }
    }
}
