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
using Aardvark.Base;

namespace EvoSim.ProjectContent.CellStuff.SightRayStuff
{
    public class SightRay : NeatAgent
    {

        public readonly static int INPUTNUM = 20;
        public readonly static int OUTPUTNUM = 5;
        public readonly static int SUBCASTS = 6;

        public float MaxLength => (owner != null) ? owner.RayDistance : 0;
        readonly float Presision = 15;
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

        public float parentVal;

        public float mateWillingness = 0;

        public IDna network;

        public List<float> outputData = new List<float>();

        public Cell owner;

        public int ID;

        public float waterDistance;

        public float landDistance;

        public float rockDistance;

        public float damageCapcity;

        public string SpeciesOrigin = "";

        public float realAngle = 0;

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<TanhActivationFunction>(15)
                   .AddLayer<TanhActivationFunction>(15)
                   .AddLayer<TanhActivationFunction>(15)
                   .SetOutput<TanhActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Main.random.NextFloat(-10, 10));

            return network;
        }

        private Species FindSpecies()
        {
            if (SceneManager.sightRaySimulation.Agents.Count > 1 && owner != null && owner.Dna is Genome)
            {
                SightRay closestRelative = SceneManager.sightRaySimulation.Agents.Where(n => (n as SightRay) != this && owner.GetSpecies() == (n as SightRay).owner.GetSpecies()).OrderBy(n => owner.BaseDistance((n as SightRay).owner)).FirstOrDefault() as SightRay;
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

            float distanceSqrt = 1;
            HSV hsv = ColorHelper.RGBToHSV(color);
            List<float> inputs = new List<float>
            {
                StretchNegative(distanceMult) * 15,
                StretchNegative((hsv.H / 360f) * distanceSqrt),
                StretchNegative(hsv.S * distanceSqrt),
                StretchNegative(hsv.V * distanceSqrt),
                StretchNegative(similarity * distanceSqrt),
                StretchNegative(health * 0.01f * distanceSqrt),
                (energy - (500 + Cell.DEADENERGY)) * 0.01f * distanceSqrt,
                StretchNegative((scale - 1) * distanceSqrt),
                MathF.Sqrt(MathF.Abs(fitness)) * distanceSqrt * MathF.Sign(fitness),
                (velocity.X * distanceSqrt) / 100f,
                (velocity.Y * distanceSqrt) / 100f,
                StretchNegative((age * distanceSqrt) / 30f),
                StretchNegative(child),
                StretchNegative(parentVal),
                ((realAngle - 3.14f) / 6.28f),
                ((mateWillingness * 0.1f) - 1) * distanceSqrt,
                StretchNegative(1.0f - (waterDistance / MaxLength)) * 3,
                StretchNegative(1.0f - (landDistance / MaxLength)) * 3,
                StretchNegative(1.0f - (rockDistance / MaxLength)) * 3,
                StretchNegative(damageCapcity / 10f) * 2,
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
            parentVal = 0;
            mateWillingness = 0;
            waterDistance = MaxLength;
            landDistance = MaxLength;
            rockDistance = MaxLength;
            damageCapcity = 0;
            realAngle = rotation;

            float anglePerRay = 6.28f / Cell.RAYS;
            float highestAngle = rotation + (anglePerRay * 0.5f);
            float lowestAngle = rotation - (anglePerRay * 0.5f);
            float angleIncrement = 1.0f / SUBCASTS;
            for (float angleLerper = 0; angleLerper < 1; angleLerper += angleIncrement)
            {
                float angleOffset = MathHelper.Lerp(lowestAngle, highestAngle, angleLerper);
                for (float i = 0; i < distance; i += Presision)
                {
                    Vector2 offset = Vector2.One.RotatedBy(rotation + owner.rotation + angleOffset) * i;
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

                    if (rockDistance > i && SceneManager.grid.TileID(checkPos) == 1)
                    {
                        distance = i;
                        rockDistance = i;
                        realAngle = rotation + owner.rotation + angleOffset;
                        break;
                    }

                    if (waterDistance > i && SceneManager.grid.TileID(checkPos) == 2)
                    {
                        waterDistance = i;
                    }

                    if (landDistance > i && SceneManager.grid.TileID(checkPos) == 3)
                    {
                        landDistance = i;
                    }

                    V2d v = checkPos.ToV2d();

                    //maybreak
                    var closestCell = SceneManager.cellSimulation.PList.GetList(checkPos).Where(n => n != parent && n.box.Contains(v)).FirstOrDefault();
                    if (closestCell != default)
                    {
                        distance = i;
                        if (closestCell.GetGenome() != null)
                            similarity = parent.GetSpecies() == closestCell.GetSpecies() ? 5 : 0;
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
                        realAngle = rotation + angleOffset;

                        if (parent.livingChildren.Contains(closestCell))
                            child = 1;

                        if (parent.parents.Contains(closestCell))
                            parentVal = 1;
                        break;
                    }
                }
            }

            return;

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
                inputs.Add(outputData[i] * 10);
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
            //if (GetSpecies() != null && GetSpecies().clients.Contains(this))
            //    GetSpecies().clients.Remove(this);
            SceneManager.sightRaySimulation.RemoveAgent(this);
        }

        public override double Distance(NeatAgent other)
        {
            Cell otherCell = (other as SightRay).owner;
            return otherCell.BaseDistance(owner) + BaseDistance(other as SightRay);
        }

        public double BaseDistance(SightRay other)
        {
            return GetGenome().Distance(other.GetGenome());
        }

        public override void OnKill()
        {
            Cull();
        }
    }
}
