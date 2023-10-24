using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Core.NeuralNetworks.NEAT
{
    public class Connection
    {
        public NeatNode from;
        public NeatNode to;

        public double weight;
        public bool enabled = true;

        public Connection(NeatNode from, NeatNode to)
        {
            this.from = from;
            this.to = to;
        }

    }
}
