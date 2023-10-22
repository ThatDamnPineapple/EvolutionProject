using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public class NEATSimulation : Simulation
    {
        public NeatHost neatHost;
        private int inputSize;
        private int outputSize;
        private int maxClients;
        private bool isSimulating = true;

        public NEATSimulation(int inputSize, int outputSize, int GenerationSize, float MutationRate = 0.01F, float MaxSimulationTime = 1) : base(GenerationSize, MutationRate, MaxSimulationTime)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            maxClients = GenerationSize;

            neatHost = new NeatHost(inputSize, outputSize, GenerationSize, this);
        }
        public override void Deploy()
        {
            neatHost.Reset(inputSize, outputSize, maxClients);
            //base.Deploy();
        }
        public override void Update()
        {
            Time+= Game1.delta;
            int inActivity = 0;

            foreach (GeneticAgent agent in Agents)
            {
                if (agent is ContinuousGeneticAgent r)
                {
                    if (!r.IsActive()) inActivity++;
                    else r.Update();
                }
                else
                {
                    inActivity++;
                }
            }


            if (Time >= MaxSimulationTime || inActivity == Agents.Count)
            {
                foreach (GeneticAgent agent in Agents)
                {
                    /*if (agent is ContinuousGeneticAgent r)
                    {
                        if (r.IsActive()) r.Kill();
                    }
                    else */agent.CalculateCurrentFitness();
                }


                BestAgent = FindBestAgent();
                neatHost.Evolve();
                Time = 0;
                Generation++;
            }
        }
    }
}
