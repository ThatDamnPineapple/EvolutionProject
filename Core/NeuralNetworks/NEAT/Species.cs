using Project1.ProjectContent.CellStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks.NEAT
{
    public class Species
    {
        public List<NeatAgent> clients = new List<NeatAgent>();
        public NeatAgent representative;
        public double score;
        public double staleness;

        public Species(NeatAgent representative)
        {
            this.representative = representative;
            this.representative.SetSpecies(this);
            clients.Add(representative);
        }

        public bool Add(NeatAgent agent)
        {
            if (agent.Distance(representative) < representative.GetGenome().Neat.CP)
            {
                agent.SetSpecies(this);
                clients.Add(agent);
                return true;
            }
            return false;
        }

        public void ForceAdd(NeatAgent agent)
        {
            agent.SetSpecies(this);
            clients.Add(agent);
            CellManager.cells.Add(agent as Cell);
        }

        public void GoExtinct()
        {
            if (clients.Count == 0)
                return;
            foreach (NeatAgent c in clients)
            {
                c.SetSpecies(null);
            }
        }

        public void EvaluateScore()
        {
            if (clients.Count == 0)
                return;
            double v = 0;
            foreach (NeatAgent a in clients)
            {
                if (a.IsActive())
                    v += a.Fitness;
            }
            score = v / clients.Count(n => n.IsActive());
        }

        public GeneticAgent BestClient()
        {
            if (clients.Count == 0)
                return default;
            float max = float.MinValue;
            GeneticAgent agent = null;

            foreach (GeneticAgent a in clients)
            {
                if (a.Fitness > max)
                {
                    max = a.Fitness;
                    agent = a;
                }
            }

            return agent;
        }

        public void Reset()
        {
            if (clients.Count == 0)
                return;
            representative = clients[Game1.random.Next(clients.Count)];
            foreach (NeatAgent c in clients)
            {
                c.SetSpecies(null);
            }

            clients.Clear();

            clients.Add(representative);
            representative.SetSpecies(this);

            score = 0;
        }

        public void Kill(float percentage)
        {
            if (clients.Count == 0)
                return;
            clients.Sort((x, y) => Math.Sign(x.Fitness - y.Fitness));

            double amount = percentage * clients.Count;
            for (int i = 0; i < amount; i++)
            {
                if (clients.Count <= 1) break;

                clients[0].SetSpecies(null);
                clients.RemoveAt(0);
            }
        }

        public Genome Breed(NeatAgent a1, NeatAgent a2)
        {
            if (a1.Fitness > a2.Fitness) return (Genome)a1.GetGenome().Combine(a2.GetGenome(), 0);

            return (Genome)a2.GetGenome().Combine(a1.GetGenome(), 0);
        }

        public Genome Breed()
        {
            NeatAgent a1 = clients[Game1.random.Next(clients.Count)];
            NeatAgent a2 = clients[Game1.random.Next(clients.Count)];

            if (a1.Fitness > a2.Fitness) return (Genome)a1.GetGenome().Combine(a2.GetGenome(), 0);

            return (Genome)a2.GetGenome().Combine(a1.GetGenome(), 0);
        }

        public int Size() => clients.Count;
    }
}
