﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.Helpers;
using EvoSim.ProjectContent.CellStuff;
using EvoSim.ProjectContent.CellStuff.SightRayStuff;

namespace EvoSim.Core.NeuralNetworks.NEAT
{
    public class NeatHost
    {
        public static readonly int MAX_NODES = (int)Math.Pow(2, 20);

        public NEATSimulation simulation;

        public Dictionary<ConnectionGene, ConnectionGene> allConnections = new Dictionary<ConnectionGene, ConnectionGene>();
        public List<NodeGene> allNodes = new List<NodeGene>();
        public List<Species> species = new List<Species>();

        public int maxClients;
        public int outputSize;
        public int inputSize;

        public double C1 = 1;
        public double C2 = 1;
        public double C3 = 0.5f;

        public double CP = 13f;

        public double WEIGHT_SHIFT_STRENGTH = 7f;
        public double WEIGHT_RANDOM_STRENGTH = 2.5f;

        public double PROBABILITY_MUTATE_LINK = 4.8f;
        public double PROBABILITY_MUTATE_NODE = 4.6f;
        public double PROBABILITY_MUTATE_WEIGHT_SHIFT = 4.3f;
        public double PROBABILITY_MUTATE_WEIGHT_RANDOM = 4.3f;
        public double PROBABILITY_MUTATE_WEIGHT_TOGGLE_LINK = 7.0f;

        public float SURVIVORS = 0.3f;

        public int STALESPECIES = 3;

        public NeatHost(int inputSize, int outputSize, int clients, NEATSimulation parent)
        {
            simulation = parent;
        }

        public Genome GenerateEmptyGenome()
        {
            Genome g = new Genome(this);
            for (int i = 0; i < inputSize + outputSize; i++)
            {
                g.nodes.Add(getNode(i + 1));
            }
            g.GenerateCalculator();

            return g;
        }

        public List<GeneticAgent> GetClients() => simulation.GetAgents();

        public void Reset(int inputSize, int outputSize, int maxClients)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            this.maxClients = maxClients;

            allConnections.Clear();
            allNodes.Clear();
            simulation.ClearAgents();

            for (int i = 0; i < inputSize; i++)
            {
                NodeGene n = getNode();
                n.x = 0.1f;
                n.y = (i + 1) / (float)(inputSize + 1);
            }

            for (int i = 0; i < outputSize; i++)
            {
                NodeGene n = getNode();
                n.x = 0.9f;
                n.y = (i + 1) / (float)(inputSize + 1);
            }

            for (int i = 0; i < this.maxClients; i++)
            {
                Genome g = GenerateEmptyGenome();
                //g.FullyConnect();
                NeatAgent a = simulation.InitialiseAgent(g) as NeatAgent;
                a.SetGenome(g);
                simulation.AddAgent(a);
            }
        }

        public NeatAgent GetClient(int i) => GetClients()[i] as NeatAgent;

        public NodeGene getNode()
        {
            NodeGene n = new NodeGene(allNodes.Count + 1);
            allNodes.Add(n);
            return n;
        }

        public NodeGene getNode(int id)
        {
            if (id <= allNodes.Count) return allNodes[id - 1];
            return getNode();
        }

        public static ConnectionGene getConnection(ConnectionGene connection)
        {
            ConnectionGene c = new ConnectionGene(connection.from, connection.to);
            c.setInovationNumber(connection.getInovationNumber());
            c.weight = connection.weight;
            c.enabled = connection.enabled;

            return c;
        }

        public ConnectionGene getConnection(NodeGene node1, NodeGene node2)
        {
            ConnectionGene connectionGene = new ConnectionGene(node1, node2);

            if (allConnections.ContainsKey(connectionGene))
            {
                connectionGene.setInovationNumber(allConnections[connectionGene].getInovationNumber());
            }
            else
            {
                connectionGene.setInovationNumber(allConnections.Count + 1);
                allConnections.Add(connectionGene, connectionGene);
            }

            return connectionGene;
        }

        public void Evolve()
        {
            GenerateSpecies();
            Kill();
            RemoveExtinctSpecies();
            Reproduce();
            Mutate();

            foreach (NeatAgent n in GetClients())
            {
                if (n.Dna == null || n.Dna is not Genome)
                    continue;
                n.GenerateCalculator();
            }
        }

        public void GenerateSpecies()
        {
            foreach (Species s in species)
            {
                s.Reset();
            }

            List<GeneticAgent> agents = GetClients();

            foreach (GeneticAgent a in agents)
            {
                NeatAgent nA = a as NeatAgent;
                if (nA.GetSpecies() != null) continue;

                bool found = false;
                foreach (Species s in species)
                {
                    if (s.Add(nA))
                    {
                        found = true;
                        break;
                    }

                }

                if (!found)
                {
                    species.Add(new Species(nA));
                }
            }

            foreach (Species s in species)
            {
                s.EvaluateScore();
            }

            foreach (GeneticAgent a in agents)
            {
                NeatAgent nA = a as NeatAgent;
                if (nA.IsActive())
                    nA.Initialise();
            }
        }

        public void Kill()
        {
            foreach (Species s in species)
            {
                s.Kill(1 - SURVIVORS);
            }
        }

        public void RemoveExtinctSpecies()
        {
            for (int i = species.Count - 1; i >= 0; i--)
            {
                if (species[i].Size() <= 1)
                {
                    species[i].staleness++;

                    if (species[i].staleness >= STALESPECIES)
                    {
                        species[i].GoExtinct();
                        species.RemoveAt(i);
                    }
                }
                else
                {
                    species[i].staleness = 0;
                }
            }
        }

        public Species PickWeightedSpecies()
        {
            float totalFitness = 0;

            foreach (Species s in species) totalFitness += (float)s.score;

            float r = Main.random.NextFloat(totalFitness);
            int index = 0;
            while (r > 0 && index < species.Count)
            {
                r -= (float)species[index].score;
                index++;
            }

            return species[Math.Max(0, index - 1)];
        }

        public void Reproduce()
        {
            foreach (GeneticAgent a in GetClients())
            {
                NeatAgent nA = a as NeatAgent;

                if (nA.GetSpecies() == null)
                {
                    if (species.Count > 0)
                    {
                        Species s = PickWeightedSpecies();
                        nA.SetGenome(s.Breed());
                        nA.Inherit(s.representative);
                        if (nA is Cell cell)
                        {
                            Cell repCell = s.representative as Cell;
                            for (int i = 0; i < repCell.cellStats.Count; i++)
                            {
                                cell.cellStats[i] = repCell.cellStats[i].Duplicate();
                            }
                        }
                        nA.Mutate();
                        s.ForceAdd(nA);
                        if (nA is SightRay h)
                            h.SpeciesOrigin = "Evolved";
                    }
                }
            }
        }

        public void Mutate()
        {
            foreach (GeneticAgent a in GetClients())
            {
                NeatAgent nA = a as NeatAgent;
                if (nA.Dna != null && nA.Dna is Genome)
                {

                    int numCycles = SceneManager.firstMutation ? 100 : 1;
                    for (int i = 0; i < numCycles; i++)
                        nA.Mutate();
                }
            }
        }
    }
}
