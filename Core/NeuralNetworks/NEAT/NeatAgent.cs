﻿using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public class NeatAgent : ContinuousGeneticAgent
    {
        private Species species;

        public void SetSpecies(Species species) => this.species = species;

        public Species GetSpecies() => species;

        public Genome GetGenome() => Dna as Genome;

        public void SetGenome(Genome g) => Dna = g;

        public double Distance(NeatAgent other) => GetGenome().Distance(other.GetGenome());

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
