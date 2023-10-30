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

        public readonly static int INPUTNUM = 18;
        public readonly static int OUTPUTNUM = 8;

        readonly float MaxLength = 500;
        readonly float Presision = 20;
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

        public float waterDistance;

        public float landDistance;

        public float damageCapcity;

        public string SpeciesOrigin = "";

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<TanhActivationFunction>(23)
                   .AddLayer<TanhActivationFunction>(23)
                   .SetOutput<TanhActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Main.random.NextFloat(-1, 1));

            return network;
        }

        private Species FindSpecies()
        {
            if (SceneManager.sightRaySimulation.Agents.Count > 1 && owner != null && owner.Dna is Genome)
            {
                SightRay closestRelative = SceneManager.sightRaySimulation.Agents.Where(n => (n as SightRay).IsActive() && (n as SightRay) != this && (n as SightRay).Distance(this) < GetGenome().Neat.CP).OrderBy(n => (n as SightRay).Distance(this)).FirstOrDefault() as SightRay;
                if (closestRelative != default)
                {
                    Species closestSpecies = closestRelative.GetSpecies();
                    closestSpecies.ForceAdd(this);
                    return closestSpecies;
                }
                SpeciesOrigin = "searched";
            }
            SpeciesOrigin = "created";
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
            SceneManager.sightRaySimulation.AddAgent(this);
            //BaseInitialization();
        }

        public SightRay(float _rotation, Cell parent, int id, IDna NewDNA = null, Species species = default)
        {
            ID = id;
            owner = parent;
            rotation = _rotation;

            Genome actualNewDNA = null;
            if (NewDNA == null || NewDNA is not Genome)
            {
                actualNewDNA = (SceneManager.sightRaySimulation as NEATSimulation).neatHost.GenerateEmptyGenome();
            }
            else
            {
                actualNewDNA = (NewDNA as Genome);
                actualNewDNA.Mutate();
            }
            Dna= actualNewDNA;
            network = Dna;
            if (species != default)
            {
                SpeciesOrigin = "inherited";
                species.ForceAdd(this);
            }
            else
            {
                SetSpecies(FindSpecies());
            }
            SceneManager.sightRaySimulation.AddAgent(this);
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

        private float StretchNegative(float n)
        {
            return (n - 0.5f) * 2;
        }

        public List<float> FeedInputs()
        {
            float distanceMult = distance / MaxLength;
            distanceMult = MathF.Pow(1.0f - distanceMult, 0.5f);

            float distanceSqrt = MathHelper.Lerp(0.8f, 1.0f, distanceMult);
            List<float> inputs = new List<float>
            {

                StretchNegative(distanceMult) * 15,
                StretchNegative((color.R / 255f ) * distanceSqrt),
                StretchNegative((color.G / 255f ) * distanceSqrt),
                StretchNegative((color.B / 255f ) * distanceSqrt),
                StretchNegative(similarity * distanceSqrt),
                StretchNegative(health * 0.01f * distanceSqrt),
                StretchNegative(energy * 0.005f * distanceSqrt),
                StretchNegative(MathF.Sqrt(MathF.Sqrt(scale)) * distanceSqrt),
                MathF.Sqrt(MathF.Abs(fitness)) * distanceSqrt * MathF.Sign(fitness),
                (velocity.X * distanceSqrt) / 100f,
                (velocity.Y * distanceSqrt) / 100f,
                StretchNegative((age * distanceSqrt) / 30f),
                child * distanceSqrt,
                ((rotation - 3.14f) / 6.28f),
                ((mateWillingness * 0.1f) - 1) * distanceSqrt,
                StretchNegative(1.0f - (waterDistance / MaxLength)) * 25,
                StretchNegative(1.0f - (landDistance / MaxLength)) * 25,
                StretchNegative(damageCapcity / 10f) * 3
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
            distance = MaxLength;
            similarity = 3;
            color = Color.Black;
            health = 0;
            energy = 0;
            fitness = 0;
            velocity = Vector2.Zero;
            age = 0;
            child = 0;
            mateWillingness = 0;
            waterDistance = MaxLength;
            landDistance = MaxLength;
            damageCapcity = 0;
            for (float i = 0; i < MaxLength; i += Presision)
            {
                Vector2 offset = Vector2.One.RotatedBy(rotation + owner.rotation) * i;
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

                if (waterDistance == MaxLength && SceneManager.grid.TileID(checkPos) == 2)
                {
                    waterDistance = i;
                }

                if (landDistance == MaxLength && SceneManager.grid.TileID(checkPos) == 3)
                {
                    landDistance = i;
                }

                //maybreak
                var closestCell = SceneManager.cellSimulation.PList.GetList(checkPos).Where(n => n != parent && CollisionHelper.CheckBoxvPointCollision(n.position, n.Size, checkPos)).FirstOrDefault();
                if (closestCell != default)
                {
                    distance = i;
                    if (closestCell.GetGenome() != null)
                        similarity = (float)parent.BaseDistance(closestCell) / (float)GetGenome().Neat.CP;
                    else
                        similarity = 0;
                    scale = closestCell.Size.LengthSquared();
                    color = closestCell.color;
                    energy = closestCell.energy;
                    health = closestCell.health;
                    pickedUp = true;
                    fitness = closestCell.GetFitness(false, true);
                    velocity = closestCell.velocity;
                    age = closestCell.lifeCounter;
                    mateWillingness = closestCell.mateWillingness;
                    damageCapcity = closestCell.DamageCapacity;

                    if (parent.livingChildren.Contains(closestCell))
                        child = 1;

                    if (parent.parents.Contains(closestCell))
                        child = -1;
                    return;
                }  
            }

            for (float i = 0; i < MaxLength; i += Presision)
            {
                Vector2 offset = Vector2.One.RotatedBy(rotation + owner.rotation) * i;
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

                var closestFood = FoodManager.foods.GetList(checkPos).Where(n => CollisionHelper.CheckBoxvPointCollision(n.position, n.size, checkPos)).FirstOrDefault();
                if (closestFood != default)
                {
                    distance = i;
                    similarity = 3;
                    energy = closestFood.energy;
                    health = 0;
                    scale = closestFood.size.LengthSquared();
                    color = closestFood.color;
                    pickedUp = true;
                    return;
                }
            }

        }

        public void FeedData(List<float> inputs, ref float[] raySums)
        {
            for (int i = 0; i < OUTPUTNUM; i++)
            {
                inputs.Add(outputData[i]);
                raySums[i] += outputData[i];
            }
        }

        public override void CalculateCurrentFitness() => Fitness = GetFitness();

        public float GetFitness()
        {
            if (owner == null)
                return 0;
            return owner.GetFitness(false, false) * MathF.Sqrt(owner.sightRays.Count(n => n.GetSpecies() == GetSpecies()));
        }

        public void Cull()
        {
            if (GetSpecies() != null && GetSpecies().clients.Contains(this))
                GetSpecies().clients.Remove(this);
            SceneManager.sightRaySimulation.RemoveAgent(this);
        }

        public override double Distance(NeatAgent other)
        {
            Cell otherCell = (other as SightRay).owner;
            return otherCell.BaseDistance(owner) + GetGenome().Distance(other.GetGenome());
        }

        public override void OnKill()
        {
            Cull();
        }
    }
}
