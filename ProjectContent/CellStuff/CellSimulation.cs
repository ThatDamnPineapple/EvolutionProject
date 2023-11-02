using EvoSim.Core.NeuralNetworks.NEAT;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.ProjectContent.CellStuff.SightRayStuff;
using EvoSim.ProjectContent.CellStuff;
using EvoSim.Helpers;

namespace EvoSim.ProjectContent.CellStuff
{
    public class CellNeatSimulation<T> : NEATSimulation where T : Cell, new()
    {
        public Func<IDna, T> GenerateAgentWithDNA;

        public PartitionedCellList<Cell> PList;

        public int Size => 300;


        public CellNeatSimulation(
            int inputSize,
            int outputSize,
            int GenerationSize,
            Func<IDna, T> GenerateAgentWithDNA,
            float MaxSimulationTime = 1) :
            base(inputSize, outputSize, GenerationSize, 0.03f, MaxSimulationTime)
        {
            PList = new PartitionedCellList<Cell>(SceneManager.PARTITIONROWS, SceneManager.PARTITIONCOLUMNS);
            this.GenerateAgentWithDNA = GenerateAgentWithDNA;
        }

        public override void SetAgents(List<GeneticAgent> value)
        {
            PList = new PartitionedCellList<Cell>(SceneManager.PARTITIONROWS, SceneManager.PARTITIONCOLUMNS);
            value.ForEach(n => PList.Add(n as Cell));
        }

        public override List<GeneticAgent> GetAgents()
        {
            return PList.basicList.OfType<GeneticAgent>().ToList();
        }

        public override void AddAgent(GeneticAgent agent) => PList.Add(agent as Cell);

        public override void RemoveAgent(GeneticAgent agent) => PList.Remove(agent as Cell);

        public override GeneticAgent GetAgent(int index) => PList.basicList[index];

        public override void ClearAgents() => PList.Clear();

        public override GeneticAgent InitialiseAgent() => new T();

        public override GeneticAgent InitialiseAgent(IDna dna) => GenerateAgentWithDNA.Invoke(dna);

        public override void Draw(SpriteBatch sb)
        {
        }

        public override void PreEvolve()
        {
            SceneManager.camera.CenterCamera();

            NEATSimulation sightRaySim = (SceneManager.sightRaySimulation as NEATSimulation);
            NeatHost sightRayHost = sightRaySim.neatHost;
            foreach (GeneticAgent agent in sightRaySim.Agents)
            {
                agent.CalculateCurrentFitness();
            }

            sightRaySim.BestAgent = sightRaySim.FindBestAgent();
            sightRayHost.Evolve();
            sightRaySim.Time = 0;
            sightRaySim.Generation++;
        }

        public override void PostEvolve()
        {
            /*foreach (GeneticAgent a in neatHost.GetClients())
            {
                if (neatHost.species.Count > 0 && Main.random.NextBool(15))
                {
                    Cell cell = a as Cell;

                    List<SightRay> orderedRays = cell.sightRays.OrderBy(n => n.GetSpecies().score).ToList();

                    for (int i = 0; i < Cell.RAYS; i++)
                    {
                        int index = (int)MathHelper.Clamp(Main.random.NextFloat(Cell.RAYS) * Main.random.NextFloat(Cell.RAYS), 0, Cell.RAYS - 1);
                        SightRay referenceRay = orderedRays[index];
                        cell.sightRays[i].Kill();
                        cell.sightRays[i] = new SightRay((i * 6.28f) / (float)Cell.RAYS, cell, i, referenceRay.Dna, referenceRay.GetSpecies());
                    }

                }
            }*/
            SceneManager.firstMutation = false;
        }

        public override void UpdateHostStats()
        {
            neatHost.C1 = 1;
            neatHost.C2 = 1;
            neatHost.C3 = 0.5f;

            neatHost.CP = 8f;

            neatHost.WEIGHT_SHIFT_STRENGTH = 2f;
            neatHost.WEIGHT_RANDOM_STRENGTH = 5f;

            neatHost.PROBABILITY_MUTATE_LINK = 0.3f;
            neatHost.PROBABILITY_MUTATE_NODE = 0.2f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_SHIFT = 0.15f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_RANDOM = 0.1f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_TOGGLE_LINK = 0.5f;

            neatHost.SURVIVORS = 0.92f;

            neatHost.STALESPECIES = 2;
        }
    }
}
