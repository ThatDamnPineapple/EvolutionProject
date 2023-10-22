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
        public readonly static int INPUTNUM = 36;
        public readonly static int OUTPUTNUM = 3;
        public float updateTimer;

        public float width;

        public float height;

        public float energy;
        public float maxEnergy;

        public Color color;

        public Vector2 position;

        public float speed => 200f;
        public float angularSpeed => 0.1f;

        public float bearings = 0f;

        public float rotation;

        public float timeLived = 0f;


        public bool dead = false;

        public bool foundFood = false;

        public Vector2 size => new Vector2(width, height);

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

        public float EnergyUsage => size.Length() * velocity.Length() * 0.02f;

        public Vector2 velocity;

        public List<SightRay> sightRays = new List<SightRay>();

        public IDna network;

        public Cell() : base()
        {
            Vector2 pos = Vector2.Zero;
            pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
            pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
            position = pos;
            color = Color.White;
            energy = 500;
            maxEnergy = 1000;

            for (int i = 0; i < 16; i++)
            {
                sightRays.Add(new SightRay((i / 16f) * 6.28f));
            }

            network = Dna;

            CellManager.cells.Add(this);
        }

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<SigmoidActivationFunction>(40)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Game1.random.NextFloat(-5, 5));

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

            for (int i = 0; i < 16; i++)
            {
                sightRays.Add(new SightRay((i / 16f) * 6.28f));
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

            for (int i = 0; i < 16; i++)
            {
                sightRays.Add(new SightRay((i / 16f) * 6.28f));
            }

            network = Dna;
            CellManager.cells.Add(this);
        }

        public override void OnUpdate()
        {
            energy -= Game1.delta * EnergyUsage;
            timeLived += Game1.delta;

            if (energy <= 0)
                Kill();
            AI();
            FoodInteraction();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawPixel(spriteBatch, color, position, width, height);
        }

        public void SetupNetwork()
        {
           
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

                float toEat = MathF.Max(hunger, foodFound.energy);
                foodFound.energy -= toEat;
                energy += toEat;

                if (foodFound.energy <= 0)
                    FoodManager.foods.Remove(foodFound);

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

            return sight;
        }

        public void Response(float[] output)
        {
            float value = output.Max();
            int maxIndex = Array.IndexOf(output, value);

            if (maxIndex == 0) bearings += 0;
            else if (maxIndex == 1) bearings -= angularSpeed;
            else if (maxIndex == 2) bearings += angularSpeed;

            velocity = Vector2.UnitX.RotatedBy(bearings) * speed;

            rotation = velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void CalculateContinuousFitness()
        {
            if (foundFood)
                Fitness = 8.0f;
            else
                Fitness = Math.Clamp(energy / maxEnergy, 0, 1);
        }

        public override void CalculateCurrentFitness()
        {
            if (foundFood)
                Fitness = 8.0f;
            else
                Fitness = Math.Clamp(energy / maxEnergy, 0, 1);
        }

        public override void OnKill()
        {
            //CellManager.cells.Remove(this);
        }

        public override void Refresh()
        {
            //CellManager.cells.Add(this);
            foundFood = false;
        }
    }
}
