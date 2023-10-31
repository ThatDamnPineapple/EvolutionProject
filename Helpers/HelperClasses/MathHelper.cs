using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Helpers.HelperClasses
{
    public static class NewMathHelper
    {
        public static float InverseSigmoid(float val)
        {
            return MathF.Log(val / (1 - val));
        }
    }
}
