using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace EvoSim.Core.NeuralNetworks.NEAT
{
    public class NEATSimulation : Simulation
    {
        public NeatHost neatHost;
        private int inputSize;
        private int outputSize;
        private int maxClients;
        private bool isSimulating = true;
        public bool globalSharing = true;
        private bool pressedG = false;

        public NEATSimulation(int inputSize, int outputSize, int GenerationSize, float MutationRate = 0.01F, float MaxSimulationTime = 1) : base(GenerationSize, MutationRate, MaxSimulationTime)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            maxClients = GenerationSize;

            neatHost = new NeatHost(inputSize, outputSize, GenerationSize, this);
            UpdateHostStats();
        }
        public override void Deploy()
        {
            neatHost.Reset(inputSize, outputSize, maxClients);
            //base.Deploy();
        }

        public virtual void PreEvolve()
        {

        }

        public virtual void UpdateHostStats()
        {
            neatHost.C1 = 1;
            neatHost.C2 = 1;
            neatHost.C3 = 0.5f;

            neatHost.CP = 13f;

            neatHost.WEIGHT_SHIFT_STRENGTH = 7f;
            neatHost.WEIGHT_RANDOM_STRENGTH = 2.5f;

            neatHost.PROBABILITY_MUTATE_LINK = 4.8f;
            neatHost.PROBABILITY_MUTATE_NODE = 4.6f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_SHIFT = 4.3f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_RANDOM = 4.3f;
            neatHost.PROBABILITY_MUTATE_WEIGHT_TOGGLE_LINK = 7.0f;

            neatHost.SURVIVORS = 0.3f;

            neatHost.STALESPECIES = 3;
        }

        public virtual void PostEvolve()
        {

        }

        public virtual GeneticAgent[] SortedAgents()
        {
            return Agents.ToArray();
        }

        public override void Update()
        {
            UpdateHostStats();
            Time+= Main.delta;
            int inActivity = 0;

            if (!pressedG && Keyboard.GetState().IsKeyDown(Keys.G))
            {
                pressedG = true;
                globalSharing = !globalSharing;
            }

            if (!Keyboard.GetState().IsKeyDown(Keys.G))
            {
                pressedG = false;
            }

            foreach (GeneticAgent agent in Agents.ToArray())
            {
                if (agent is ContinuousGeneticAgent r)
                {
                    r.Update();
                }
                else
                {
                    inActivity++;
                }
            }


            if ((Time >= MaxSimulationTime || inActivity == Agents.Count) && globalSharing)
            {
                foreach (GeneticAgent agent in Agents)
                {
                    /*if (agent is ContinuousGeneticAgent r)
                    {
                        if (r.IsActive()) r.Kill();
                    }
                    else */agent.CalculateCurrentFitness();
                }

                PreEvolve();
                BestAgent = FindBestAgent();
                neatHost.Evolve();
                Time = 0;
                Generation++;

                PostEvolve();
            }
        }
    }
}
