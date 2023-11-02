using EvoSim.Core.NeuralNetworks.NEAT;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.ProjectContent.CellStuff.SightRayStuff;

namespace EvoSim.ProjectContent.CellStuff
{
    internal class SightRayNeatSimulation<T> : NEATSimulation where T : SightRay, new()
    {

        public Func<IDna, T> GenerateAgentWithDNA;
        public int Size => 300;


        public SightRayNeatSimulation(
            int inputSize,
            int outputSize,
            int GenerationSize,
            Func<IDna, T> GenerateAgentWithDNA,
            float MaxSimulationTime = 1) :
            base(inputSize, outputSize, GenerationSize, 0.03f, MaxSimulationTime)
        {
            this.GenerateAgentWithDNA = GenerateAgentWithDNA;
        }

        public override GeneticAgent InitialiseAgent() => new T();

        public override GeneticAgent InitialiseAgent(IDna dna) => GenerateAgentWithDNA.Invoke(dna);

        public override void UpdateHostStats()
        {
            neatHost.C1 = 1;
            neatHost.C2 = 1;
            neatHost.C3 = 0.5f;

            neatHost.CP = 20f;

            neatHost.WEIGHT_SHIFT_STRENGTH = 2f;
            neatHost.WEIGHT_RANDOM_STRENGTH = 3.5f;

            neatHost.PROBABILITY_MUTATE_LINK = 0.8f;
            neatHost.PROBABILITY_MUTATE_NODE = 0.6f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_SHIFT = 1.3f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_RANDOM = 0.1f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_TOGGLE_LINK = 0.3f;

            neatHost.SURVIVORS = 0.8f;

            neatHost.STALESPECIES = 3;
        }

    }
}
