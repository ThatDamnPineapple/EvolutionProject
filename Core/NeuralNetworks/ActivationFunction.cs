using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Core.NeuralNetworks
{
    public abstract class ActivationFunction
    {
        public virtual float Compute(float x)
        {
            return x;
        }
    }

    public class LinearActivationFunction : ActivationFunction { }

    public class SigmoidActivationFunction : ActivationFunction
    {
        public override float Compute(float x)
        {
            return 1f / (float)(1 + Math.Pow(MathHelper.E, -x));
        }
    }
}
