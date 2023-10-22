using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public class Gene
    {
        protected int innovationNumber;

        public Gene(int innovationNumber) => this.innovationNumber = innovationNumber;

        public int getInovationNumber() => innovationNumber;

        public void setInovationNumber(int n) => innovationNumber = n;
    }
}
