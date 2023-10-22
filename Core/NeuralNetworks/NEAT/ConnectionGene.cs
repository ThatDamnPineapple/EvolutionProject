using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public class ConnectionGene : Gene
    {
        public NodeGene from;
        public NodeGene to;

        public double weight;
        public bool enabled = true;

        public ConnectionGene(NodeGene from, NodeGene to) : base(0)
        {
            this.from = from;
            this.to = to;
        }

        public override bool Equals(object o)
        {
            if (!(o is ConnectionGene)) return false;
            ConnectionGene c = (ConnectionGene)o;
            return from.Equals(c.from) && to.Equals(c.to);
        }

        public override int GetHashCode()
        {
            return from.GetHashCode() * NeatHost.MAX_NODES + to.GetHashCode();
        }
    }
}
