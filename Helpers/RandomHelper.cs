using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Helpers
{
    internal static class RandomHelper
    {

        internal static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        internal static float NextFloat(this Random random, float max)
        {
            float ret = random.NextFloat();
            return ret * max;
        }

        internal static float NextFloat(this Random random, float min, float max)
        {
            float ret = random.NextFloat();
            return ret * (max - min) + min;
        }
    }
}
