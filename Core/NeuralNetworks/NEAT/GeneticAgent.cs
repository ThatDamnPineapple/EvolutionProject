using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public class GeneticAgent
    {
        public IDna Dna;
        public float Fitness;

        public GeneticAgent(IDna Dna)
        {
            this.Dna = Dna;
        }

        public GeneticAgent()
        {
            Dna = GenerateRandomAgent();
        }

        public virtual IDna GenerateRandomAgent() { return null; }
        public virtual void CalculateCurrentFitness() { }
    }

    public class ContinuousGeneticAgent : GeneticAgent
    {
        protected bool Active = true;

        public ContinuousGeneticAgent(IDna Dna) : base(Dna) { }

        public ContinuousGeneticAgent() : base() { }

        public virtual void Update()
        {
            CalculateContinuousFitness();
            OnUpdate();
        }
        public virtual void OnUpdate() { }

        public virtual void OnKill() { }

        public bool IsActive() => Active;
        public void Kill()
        {
            CalculateCurrentFitness();
            Active = false;
            OnKill();
        }
        public virtual void CalculateContinuousFitness() { }
    }
}
