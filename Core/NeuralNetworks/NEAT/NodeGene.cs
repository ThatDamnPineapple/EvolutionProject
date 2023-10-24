using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Core.NeuralNetworks.NEAT
{
    public class NodeGene : Gene
    {
        public float x, y;

        public NodeGene(int innovationNumber) : base(innovationNumber) { }

        public override bool Equals(object o)
        {
            if (!(o is NodeGene)) return false;
            return innovationNumber == ((NodeGene)o).innovationNumber;
        }
        public override int GetHashCode() => innovationNumber;
    }
}
