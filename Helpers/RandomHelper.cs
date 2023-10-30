using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Helpers
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

        internal static bool NextBool(this Random random, int chance = 2)
        {
            return random.Next(chance) == 0;
        }

        internal static int NextSign(this Random random)
        {
            return random.NextBool() ? -1 : 1;
        }
    }
}
