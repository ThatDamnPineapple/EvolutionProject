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
    public class CellNeatSimulation<T> : NEATSimulation where T : Cell, new()
    {

        public Func<IDna, T> GenerateAgentWithDNA;
        public int Size => 300;


        public CellNeatSimulation(
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

        public override void Draw(SpriteBatch sb)
        {
        }

        public override void PreEvolve()
        {
            NEATSimulation sightRaySim = (SceneManager.sightRaySimulation as NEATSimulation);
            NeatHost sightRayHost = sightRaySim.neatHost;
            foreach (GeneticAgent agent in sightRaySim.Agents)
            {
                /*if (agent is ContinuousGeneticAgent r)
                {
                    if (r.IsActive()) r.Kill();
                }
                else */
                agent.CalculateCurrentFitness();
            }

            sightRaySim.BestAgent = sightRaySim.FindBestAgent();
            sightRayHost.Evolve();
            sightRaySim.Time = 0;
            sightRaySim.Generation++;
        }
    }
}
