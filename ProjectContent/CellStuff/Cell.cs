using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1.Core.NeuralNetworks;
using Project1.Helpers;
using Project1.Interfaces;
using Project1.ProjectContent.Resources;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.CellStuff
{
    internal class Cell
    {

        public readonly static float UPDATERATE = 5.0f;
        public readonly static int INPUTNUM = 84;
        public float updateTimer;

        public float width;

        public float height;

        public float energy;
        public float maxEnergy;

        public Color color;

        public Vector2 position;

        public bool dead = false;

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

        public float EnergyUsage => size.Length() * velocity.Length();

        public Vector2 velocity;

        public List<SightRay> sightRays = new List<SightRay>();

        public SimpleNeuralNetwork network = new SimpleNeuralNetwork(INPUTNUM);
        public NeuralLayerFactory factory = new NeuralLayerFactory();

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy, float _maxEnergy, SimpleNeuralNetwork _network = null)
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

            if (_network != null)
            {

            }
            else
                SetupNetwork();

        }

        public void Update(GameTime gameTime)
        {
            energy -= Game1.delta * EnergyUsage;

            if (energy <= 0)
                dead = true;
            UpdateRays(gameTime);
            AI();
            FoodInteraction();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawPixel(spriteBatch, color, position, width, height);
        }

        public void SetupNetwork()
        {
            network.AddLayer(factory.CreateNeuralLayer(120, new RectifiedActivationFuncion(), new WeightedSumFunction()));

            network.AddLayer(factory.CreateNeuralLayer(3, new SigmoidActivationFunction(0.7), new WeightedSumFunction()));
        }

        public void UpdateRays(GameTime gameTime)
        {
            updateTimer++;
            if (updateTimer > UPDATERATE)
            {
                updateTimer -= UPDATERATE;
                sightRays.ForEach(n => n.CastRay(this));
            }
        }

        public void AI()
        {
            updateTimer += Game1.delta;
            if (updateTimer > UPDATERATE)
            {
                updateTimer -= UPDATERATE;
                double[][] expectation = new double[INPUTNUM][];
                for (int i = 0; i < INPUTNUM; i++)
                {
                    expectation[i] = new double[] { 0.5, 0.5, 1.0};
                }
                network.PushExpectedValues(expectation);


            }
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

                Reward(toEat);
                if (!CellManager.foundMinimum)
                {
                    CellManager.EnactMinimum(network);
                }

                if (foodFound.energy <= 0)
                    FoodManager.foods.Remove(foodFound);
            }
        }
    }
}
