using DeepCopy;
using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1.Core.NeuralNetworks;
using Project1.Core.NeuralNetworks.NEAT;
using Project1.Helpers;
using Project1.Interfaces;
using Project1.ProjectContent.Resources;
using Project1.ProjectContent.Terrain;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.CellStuff
{
    internal class Cell : NeatAgent
    {

        public readonly static float UPDATERATE = 0.01f;
        public readonly static int RAYS = 12;
        public readonly static int RAYVALUES = 3;
        public readonly static int ADDITIONALVALUES = 5;
        public static int INPUTNUM => (RAYS * RAYVALUES) + ADDITIONALVALUES;
        public readonly static int OUTPUTNUM = 3;
        public float lifeCounter;
        public CellStat AceThreshhold = new CellStat(0.95f, 0.01f, 0.0005f);
        public CellStat SexThreshhold = new CellStat(0.8f, 0.01f, 0.0005f);
        public CellStat Speed = new CellStat(200f, 5f, 0.5f);
        public CellStat ChildEnergy = new CellStat(0.5f, 0.05f, 0.001f);
        public CellStat Scale = new CellStat(1.0f, 0.15f, 0.005f);

        public float width;

        public CellNeatSimulation<Cell> sim;
        public float height;

        public float energy;
        public float maxEnergy;

        public Color color;

        public Vector2 position;

        public float acceleration=> 10.0f;

        public float bearings = 0f;

        public float rotation;

        public float timeLived = 0f;


        public bool dead = false;

        public bool foundFood = false;

        public int kids = 0;

        public Vector2 size => new Vector2(width, height) * Scale.Value;

        public Vector2 Center
        {
            get
            {
                return position + (size / 2.0f);
            }
            set
            {
                position = value - (size / 2.0f);
            }
        }

        public float EnergyUsage => (size.Length() * Scale.Value * velocity.Length() * 0.0001f) + 20;

        public Vector2 velocity;

        public List<SightRay> sightRays = new List<SightRay>();

        public IDna network;

        public Cell() : base()
        {
            Vector2 pos = Vector2.Zero;
            pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
            pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
            position = pos;
            color = new Color(0, 0, 1.0f);
            //energy = 500;
            maxEnergy = 1000;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;

            CellManager.cells.Add(this);
        }

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<SigmoidActivationFunction>(40)
                   .AddLayer<SigmoidActivationFunction>(40)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Game1.random.NextFloat(-1, 1));

            return network;
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy, float _maxEnergy, IDna Dna) : base()
        {
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            maxEnergy = _maxEnergy;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;

            CellManager.cells.Add(this);
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy, float _maxEnergy) : base()
        {
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            maxEnergy = _maxEnergy;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;
            CellManager.cells.Add(this);
        }

        public override void OnUpdate()
        {
            energy -= Game1.delta * EnergyUsage * (CollisionHelper.CheckBoxvBoxCollision(position, size, Vector2.Zero, TerrainManager.mapSize) ? 1.0f : 10.0f);
            timeLived += Game1.delta;

            if (energy <= 0)
            {
                color = Color.Gray;
                Kill();
            }
            AI();
            FoodInteraction();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            string speciesString = "null";
            if (GetSpecies() != null)
            {
                speciesString = GetSpecies().GetHashCode().ToString();
            }

            string text = "Species: " + speciesString +"\nEnergy: " + energy.ToString();
            DrawHelper.DrawText(spriteBatch, text, Color.White, position - new Vector2(0, 32), Vector2.One);
            DrawHelper.DrawPixel(spriteBatch, color * (energy / maxEnergy), position, width * Scale.Value, height * Scale.Value);
        }

        public void AI()
        {
            sightRays.ForEach(n => n.CastRay(this));
            network.Compute(FeedInputs().ToArray());
            Response(network.Response);
           
            position += velocity * Game1.delta;
        }

        public void FoodInteraction()
        {
            var foodFound = FoodManager.foods.Where(n => CollisionHelper.CheckBoxvBoxCollision(Center, size, n.Center, n.size)).FirstOrDefault();

            if (foodFound != default)
            {
                float hunger = maxEnergy - energy;

                float toEat = MathF.Min(hunger, foodFound.energy);
                foodFound.energy -= toEat;
                energy += toEat;

                if (foodFound.energy <= 0)
                    FoodManager.foods.Remove(foodFound);

                foodFound.color.R = (byte)(255 * (foodFound.energy / 1000f));

                foundFood = true;
            }
        }

        public List<float> FeedInputs()
        {
            List<float> sight = new List<float>();

            sightRays.ForEach(n => n.FeedData(sight));

            sight.Add(position.X);
            sight.Add(position.Y);
            sight.Add(rotation % MathHelper.TwoPi);
            sight.Add(energy);
            sight.Add(lifeCounter);

            return sight;
        }

        public void Response(float[] output)
        {


            velocity.X += acceleration * (output[0] - 0.5f);
            velocity.Y += acceleration * (output[1] - 0.5f);

            if (velocity.Length() > Speed.Value)
            {
                velocity.Normalize();
                velocity *= Speed.Value;
            }

            lifeCounter += Game1.delta;
            if (output[2] > AceThreshhold.Value && GetSpecies() != null && energy > 400)
            {
                Mitosis();
            }
            if (output[2] > SexThreshhold.Value && GetSpecies() != null && kids < 5 && energy > 100)
            {
                TryHaveSex();
            }

            rotation = velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void CalculateContinuousFitness()
        {
            Fitness = (energy / maxEnergy) + (velocity.Length() / Speed.Value) + kids;

            if (velocity.Length() < 10 || !IsActive())
                Fitness = -999;
        }

        public override void CalculateCurrentFitness()
        {
            Fitness = (energy / maxEnergy) + (velocity.Length() / Speed.Value) + kids;
            if (velocity.Length() < 10 || !IsActive())
                Fitness = -999;
        }

        public override void OnKill()
        {
            color = Color.Gray;
            if (GetSpecies() != null)
                GetSpecies().clients.Remove(this);
            sim.Agents.Remove(this);
            //CellManager.cells.Remove(this);
        }

        public override void Refresh()
        {
            //CellManager.cells.Add(this);
            foundFood = false;
        }

        public void TryHaveSex()
        {
            if (GetSpecies().Size() > 1)
            {
                var partner = CellManager.cells.Where(n => n != this && n.IsActive() && CollisionHelper.CheckBoxvBoxCollision(n.Center, n.size, Center, size) && Distance(n) < GetGenome().Neat.CP).FirstOrDefault();
                if (partner != default && partner.energy > 100 && partner.kids < 5)
                {
                    Cell child = new Cell(color, size, position, MathHelper.Lerp(energy, partner.energy, 0.5f) * ChildEnergy.Value, maxEnergy);
                    child.SetGenome(GetSpecies().Breed(partner, this));
                    child.SetSpecies(GetSpecies());

                    sim.neatHost.GetClients().Add(child);

                    child.sim = sim;
                    sim.Agents.Add(child);

                    child.Speed = new CellStat(MathHelper.Lerp(Speed.Value, partner.Speed.Value, 0.5f), MathHelper.Lerp(Speed.Mutation, partner.Speed.Mutation, 0.5f), MathHelper.Lerp(Speed.Mutation2, partner.Speed.Mutation2, 0.5f));
                    child.Speed.Mutate();

                    child.AceThreshhold = new CellStat(MathHelper.Lerp(AceThreshhold.Value, partner.AceThreshhold.Value, 0.5f), MathHelper.Lerp(AceThreshhold.Mutation, partner.AceThreshhold.Mutation, 0.5f), MathHelper.Lerp(AceThreshhold.Mutation2, partner.AceThreshhold.Mutation2, 0.5f));
                    child.AceThreshhold.Mutate();

                    child.SexThreshhold = new CellStat(MathHelper.Lerp(SexThreshhold.Value, partner.SexThreshhold.Value, 0.5f), MathHelper.Lerp(SexThreshhold.Mutation, partner.SexThreshhold.Mutation, 0.5f), MathHelper.Lerp(SexThreshhold.Mutation2, partner.SexThreshhold.Mutation2, 0.5f));
                    child.SexThreshhold.Mutate();

                    child.ChildEnergy = new CellStat(MathHelper.Lerp(ChildEnergy.Value, partner.ChildEnergy.Value, 0.5f), MathHelper.Lerp(ChildEnergy.Mutation, partner.ChildEnergy.Mutation, 0.5f), MathHelper.Lerp(ChildEnergy.Mutation2, partner.ChildEnergy.Mutation2, 0.5f));
                    child.ChildEnergy.Mutate();

                    child.Scale = new CellStat(MathHelper.Lerp(Scale.Value, partner.Scale.Value, 0.5f), MathHelper.Lerp(Scale.Mutation, partner.Scale.Mutation, 0.5f), MathHelper.Lerp(Scale.Mutation2, partner.Scale.Mutation2, 0.5f));
                    child.Scale.Mutate();

                    partner.energy *= 1.0f - (ChildEnergy.Value * 0.5f);
                    energy *= 1.0f - (ChildEnergy.Value * 0.5f);
                    partner.kids++;
                    kids++;
                }
            }
        }

        public void Mitosis()
        {
            Cell child = new Cell(color, size, position, energy * ChildEnergy.Value, maxEnergy);
            if (GetSpecies().Size() > 0)
            {
                child.SetGenome(GetSpecies().Breed());
                child.SetSpecies(GetSpecies());
            }
            else
                return;
            sim.neatHost.GetClients().Add(child);

            energy *= (1.0f - ChildEnergy.Value);
            child.sim = sim;
            sim.Agents.Add(child);

            child.Speed = new CellStat(Speed.Value, Speed.Mutation, Speed.Mutation2);
            child.Speed.Mutate();

            child.AceThreshhold = new CellStat(AceThreshhold.Value, AceThreshhold.Mutation, AceThreshhold.Mutation2);
            child.AceThreshhold.Mutate();

            child.SexThreshhold = new CellStat(SexThreshhold.Value, SexThreshhold.Mutation, SexThreshhold.Mutation2);
            child.SexThreshhold.Mutate();

            child.ChildEnergy = new CellStat(ChildEnergy.Value, ChildEnergy.Mutation, ChildEnergy.Mutation2);
            child.ChildEnergy.Mutate();

            child.Scale = new CellStat(Scale.Value, Scale.Mutation, Scale.Mutation2);
            child.Scale.Mutate();
        }
    }
}
