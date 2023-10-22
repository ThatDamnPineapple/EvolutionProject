using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public interface IDna
    {
        IDna Combine(IDna combinee, float mutationRate);
        void Compute(float[] inputs);
        float[] Response { get; }
    }
}
