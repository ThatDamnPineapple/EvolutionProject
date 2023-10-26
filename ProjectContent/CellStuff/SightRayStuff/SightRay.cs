using Microsoft.Xna.Framework;
using EvoSim.Helpers;
using EvoSim.ProjectContent.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.Core.NeuralNetworks.NEAT;
using EvoSim.Core.NeuralNetworks;
using EvoSim.Helpers.HelperClasses;

namespace EvoSim.ProjectContent.CellStuff.SightRayStuff
{
    public class SightRay : NeatAgent
    {

        public readonly static int INPUTNUM = 15;
        public readonly static int OUTPUTNUM = 8;

        readonly float MaxLength = 400;
        readonly float Presision = 10;
        public float rotation;

        public float distance;

        public float similarity = 0;

        public Color color = Color.Black;

        public bool pickedUp = false;

        public float scale;

        public float health;
        public float energy;

        public Vector2 velocity;

        public float fitness;

        public float age;

        public float child;

        public float mateWillingness = 0;

        public IDna network;

        public List<float> outputData = new List<float>();

        public Cell owner;

        public int ID;

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<SigmoidActivationFunction>(30)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Main.random.NextFloat(-1, 1));

            return network;
        }

        private Species FindSpecies()
        {
            if (SceneManager.cellSimulation != null && SceneManager.cellSimulation.Agents.Count > 1 && owner != null && owner.Dna is Genome)
            { 
                Cell closestOwner = SceneManager.cellSimulation.Agents.Where(n => n != owner && (n as Cell).IsActive() && (n as Cell).sightRays[ID].GetSpecies() != null).OrderBy(n => (n as Cell).Distance(owner)).FirstOrDefault() as Cell;

                if (closestOwner != default)
                {
                    Species closestSpecies = closestOwner.sightRays[ID].GetSpecies();
                    //SetGenome(closestSpecies.Breed());
                    closestSpecies.ForceAdd(this);
                    return closestSpecies;
                }
            }
            Species newSpecies = new Species(this);
            (SceneManager.sightRaySimulation as NEATSimulation).neatHost.species.Add(newSpecies);
            return newSpecies;        
        }

        public SightRay() : base()
        {
            rotation = 0;
            if (Dna == null || Dna is not Genome)
            {
                Dna = (SceneManager.sightRaySimulation as NEATSimulation).neatHost.GenerateEmptyGenome();
            }
            network = Dna;
            SetSpecies(FindSpecies());
            SceneManager.sightRaySimulation.Agents.Add(this);
            //BaseInitialization();
        }

        public SightRay(float _rotation, Cell parent, int id, IDna NewDNA = null, Species species = default)
        {
            ID = id;
            owner = parent;
            rotation = _rotation;

            IDna actualNewDNA = null;
            if (NewDNA == null || NewDNA is not Genome)
            {
                actualNewDNA = (SceneManager.sightRaySimulation as NEATSimulation).neatHost.GenerateEmptyGenome();
            }
            else
            {
                actualNewDNA = NewDNA;
            }
            Dna= actualNewDNA;
            network = Dna;
            if (species != default)
            {
                species.ForceAdd(this);
            }
            else
            {
                SetSpecies(FindSpecies());
            }
            SceneManager.sightRaySimulation.Agents.Add(this);
        }

        public override void OnUpdate()
        {
            
        }

        public void UpdateNetwork()
        {
            CastRay(owner);
            network.Compute(FeedInputs().ToArray());
            Response(network.Response);
        }

        public List<float> FeedInputs()
        {
            List<float> inputs = new List<float>
            {
                distance - MaxLength / 2.0f,
                color.R - 128,
                color.G - 128,
                color.B - 128,
                similarity * 1000,
                health,
                energy,
                scale * 100,
                fitness,
                velocity.X,
                velocity.Y,
                age,
                child,
                rotation,
                mateWillingness
            };
            return inputs;
        }

        public void Response(float[] output)
        {
            outputData.Clear();
            for (int i = 0; i < OUTPUTNUM; i++)
            {
                outputData.Add(output[i]);
            }
        }

        public void CastRay(Cell parent)
        {
            distance = 10000;
            similarity = 100;
            color = Color.Black;
            health = 0;
            energy = 0;
            fitness = 0;
            velocity = Vector2.Zero;
            age = -100;
            child = 0;
            mateWillingness = 0;
            for (float i = 0; i < MaxLength; i += Presision)
            {
                Vector2 offset = Vector2.One.RotatedBy(rotation) * i;
                Vector2 checkPos = offset + parent.Center;

                while (checkPos.X > SceneManager.grid.mapSize.X)
                {
                    checkPos.X -= SceneManager.grid.mapSize.X;
                }

                while (checkPos.X < 0)
                {
                    checkPos.X += SceneManager.grid.mapSize.X;
                }

                while (checkPos.Y > SceneManager.grid.mapSize.Y)
                {
                    checkPos.Y -= SceneManager.grid.mapSize.Y;
                }

                while (checkPos.Y < 0)
                {
                    checkPos.Y += SceneManager.grid.mapSize.Y;
                }

                var closestCell = SceneManager.cellSimulation?.Agents.Where(n => CollisionHelper.CheckBoxvPointCollision((n as Cell).position, (n as Cell).Size, checkPos)).FirstOrDefault();
                if (closestCell != default)
                {
                    var closestCellCast = closestCell as Cell;
                    distance = i;
                    if (closestCellCast.GetGenome() != null)
                        similarity = (float)parent.BaseDistance(closestCellCast);
                    else
                        similarity = 0;
                    scale = closestCellCast.Size.Length();
                    color = closestCellCast.color;
                    energy = closestCellCast.energy;
                    health = closestCellCast.health;
                    pickedUp = true;
                    fitness = closestCellCast.GetFitness(false, true);
                    velocity = closestCellCast.velocity;
                    age = closestCellCast.lifeCounter;
                    mateWillingness = closestCellCast.mateWillingness;

                    if (parent.livingChildren.Contains(closestCellCast))
                        child = 100;

                    if (parent.parents.Contains(closestCellCast))
                        child = -100;
                    return;
                }  
            }

            for (float i = 0; i < MaxLength; i += Presision)
            {
                Vector2 offset = Vector2.One.RotatedBy(rotation) * i;
                Vector2 checkPos = offset + parent.Center;

                while (checkPos.X > SceneManager.grid.mapSize.X)
                {
                    checkPos.X -= SceneManager.grid.mapSize.X;
                }

                while (checkPos.X < 0)
                {
                    checkPos.X += SceneManager.grid.mapSize.X;
                }

                while (checkPos.Y > SceneManager.grid.mapSize.Y)
                {
                    checkPos.Y -= SceneManager.grid.mapSize.Y;
                }

                while (checkPos.Y < 0)
                {
                    checkPos.Y += SceneManager.grid.mapSize.Y;
                }

                var closestFood = FoodManager.foods.Where(n => CollisionHelper.CheckBoxvPointCollision(n.position, n.size, checkPos)).FirstOrDefault();
                if (closestFood != default)
                {
                    distance = i;
                    similarity = 100;
                    energy = closestFood.energy;
                    health = 0;
                    scale = closestFood.size.Length();
                    color = closestFood.color;
                    pickedUp = true;
                    return;
                }
            }

        }

        public void FeedData(List<float> inputs)
        {
            outputData.ForEach(n => inputs.Add(n));
        }

        public override void CalculateContinuousFitness() => Fitness = GetFitness();

        public override void CalculateCurrentFitness() => Fitness = GetFitness();

        public float GetFitness()
        {
            if (owner == null)
                return 0;
            return owner.GetFitness(false, false);
        }

        public void Cull()
        {
            SceneManager.sightRaySimulation.Agents.Remove(this);
        }

        public override double Distance(NeatAgent other)
        {
            Cell otherCell = (other as SightRay).owner;
            double ret = GetGenome().Distance(other.GetGenome());
            return ret + otherCell.GetGenome().Distance(owner.GetGenome());
        }

        public override void OnKill()
        {
            
        }
    }
}
