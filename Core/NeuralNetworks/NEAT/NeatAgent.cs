using EvoSim.ProjectContent.CellStuff.SightRayStuff;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Core.NeuralNetworks.NEAT
{
    public class NeatAgent : ContinuousGeneticAgent
    {
        private Species species;

        public void SetSpecies(Species species)
        {
            if (species == null && this is SightRay ray)
            {
                float debug1 = ray.debugInfo;
                float debug2 = ray.debugInfo;
                ray.debugInfo3 = 5;
            }
            this.species = species;
        }

        public Species GetSpecies() => species;

        public Genome GetGenome() => Dna as Genome;

        public void SetGenome(Genome g)
        {
            if (this is SightRay ray && g == null)
                ray.debugNulled = 5;
            Dna = g;
        }

        public virtual double Distance(NeatAgent other) => GetGenome().Distance(other.GetGenome());

        public void Mutate() => GetGenome().Mutate();

        public void GenerateCalculator() => GetGenome().GenerateCalculator();

        public virtual void Refresh() { }
        public virtual void Initialise()
        {
            Active = true;
            Refresh();
        }

    }
}
