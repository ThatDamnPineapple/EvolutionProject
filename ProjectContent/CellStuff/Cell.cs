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
using Project1.ProjectContent.Terrain.TerrainTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public readonly static int RAYVALUES = 6;
        public readonly static int TERRAINRANGE = 4;
        public readonly static int ADDITIONALVALUES = 6;
        public static int INPUTNUM => (RAYS * RAYVALUES) + ADDITIONALVALUES + (TERRAINRANGE * TERRAINRANGE);
        public readonly static int OUTPUTNUM = 4;
        public float lifeCounter;
        public CellStat AceThreshhold = new CellStat(0.8f, 0.01f, 0.0005f, 0.25f, 0.9f, false);
        public CellStat SexThreshhold = new CellStat(0.5f, 0.01f, 0.0005f, 0.05f, 1, false);
        public CellStat Speed = new CellStat(200f, 0.1f, 0.01f, 20, 400, true);
        public CellStat ChildEnergy = new CellStat(0.5f, 0.01f, 0.001f, 0.3f, 0.9f, false);
        public CellStat Scale = new CellStat(1.0f, 0.15f, 0.005f, 0.125f, 7f, true);
        public CellStat Red = new CellStat(0.1f, 0.45f, 0.001f, 0.1f, 1, false);
        public CellStat Green = new CellStat(0.1f, 0.45f, 0.001f, 0.1f, 1, false);
        public CellStat Blue = new CellStat(1, 0.45f, 0.001f, 0.1f, 1, false);
        public CellStat fightThreshhold = new CellStat(0.5f, 0.01f, 0.0001f, 0.05f, 1, false);
        public CellStat regenRate = new CellStat(10, 0.05f, 0.001f, 1, 100, true);

        public float width;

        public CellNeatSimulation<Cell> sim;
        public float height;

        public float energy;

        public float health;

        public float maxEnergy => 1000 * Scale.Value;
        public float maxHealth => 1000 * Scale.Value;

        public Color color;

        public Vector2 position;

        public float acceleration=> 10.0f;

        public float bearings = 0f;

        public float rotation;

        public float timeLived = 0f;


        public bool dead = false;

        public bool foundFood = false;

        public int kids = 0;
        public int kills = 0;

        public float stillCounter = 0;

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

        public Vector2 TopLeft => position;

        public Vector2 TopRight => position + new Vector2(size.X, 0);

        public Vector2 BottomLeft => position + new Vector2(0, size.Y);

        public Vector2 BottomRight => position + size;

        public float Left => position.X;

        public float Right => position.X + size.X;

        public float Top => position.Y;

        public float Bottom => position.Y + size.Y;

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
            health = maxHealth;

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
                   .AddLayer<SigmoidActivationFunction>(80)
                   .AddLayer<SigmoidActivationFunction>(80)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Game1.random.NextFloat(-1, 1));

            return network;
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy, IDna Dna) : base()
        {
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            health = maxHealth;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;

            CellManager.cells.Add(this);
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy) : base()
        {
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            health = maxHealth;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;
            CellManager.cells.Add(this);
        }

        public override void OnUpdate()
        {
            if (velocity.Length() < 1)
            {
                stillCounter += Game1.delta;
                if (stillCounter > 8)
                {
                    Kill();
                    return;
                }
            }
            if (health < maxHealth)
            {
                float regen = regenRate.Value * Game1.delta;
                regen = MathF.Min(regen, maxHealth - health);
                energy -= regen * 2;
                health += regen;
            }
            energy -= Game1.delta * EnergyUsage * (CollisionHelper.CheckBoxvBoxCollision(position, size, Vector2.Zero, TerrainManager.mapSize) ? 1.0f : 10.0f);
            timeLived += Game1.delta;

            color = new Color(Red.Value, Green.Value, Blue.Value);
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

            string text = "Species: " + speciesString +"\nEnergy: " + ((int)energy).ToString() + "\nHealth:" + ((int)health).ToString()+ "/" + ((int)maxHealth).ToString() + "\nKills: " + kills.ToString() ;
            DrawHelper.DrawText(spriteBatch, text, Color.Black, position - new Vector2(0, 72), Vector2.One);
            DrawHelper.DrawPixel(spriteBatch, color, position, width * Scale.Value, height * Scale.Value);
        }

        public void AI()
        {
            sightRays.ForEach(n => n.CastRay(this));
            network.Compute(FeedInputs().ToArray());
            Response(network.Response);
           
            position += velocity * Game1.delta;

            if (TopLeft.X < 0)
            {
                position.X = 0;
                velocity.X = 0;
            }

            if (TopRight.X > TerrainManager.mapSize.X)
            {
                position.X = TerrainManager.mapSize.X - size.X;
                velocity.X = 0;
            }

            if (TopLeft.Y < 0)
            {
                position.Y = 0;
                velocity.Y = 0;
            }

            if (BottomRight.Y > TerrainManager.mapSize.Y)
            {
                position.Y = TerrainManager.mapSize.Y - size.Y;
                velocity.Y = 0;
            }

            for (float i = Left; i < Right; i+= TerrainManager.squareWidth)
            {
                for (float j = Top; j < Bottom; j += TerrainManager.squareHeight)
                {
                    int x = (int)(i / TerrainManager.squareWidth);
                    int y = (int)(j / TerrainManager.squareHeight);
                    if (TerrainManager.InGrid(x, y) && TerrainManager.terrainGrid[x,y] is RockSquare)
                    {
                        Center = CollisionHelper.StopBox(Center, size, new Vector2(x + 0.5f, y + 0.5f) * TerrainManager.squareSize, TerrainManager.squareSize, ref velocity);
                    }
                }
            }
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

            int tileRadius = TERRAINRANGE / 2;
            for (int i = -tileRadius; i < tileRadius; i++)
            {
                for (int j = -tileRadius; j < tileRadius; j++)
                {
                    Vector2 originTile = Center / TerrainManager.squareSize;
                    if (TerrainManager.InGrid((int)(originTile.X + i), (int)(originTile.Y + j)))
                    {
                        TerrainSquare square = TerrainManager.terrainGrid[(int)(originTile.X + i), (int)(originTile.Y + j)];
                        if (square is RockSquare)
                            sight.Add(200);
                        else
                            sight.Add(0);
                    }
                    else
                        sight.Add(200);
                }
            }

            sight.Add(rotation % MathHelper.TwoPi);
            sight.Add(energy);
            sight.Add(lifeCounter);
            sight.Add(health);
            sight.Add(velocity.X);
            sight.Add(velocity.Y);

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

            float fightVal = output[3];
            if (fightVal > fightThreshhold.Value && lifeCounter > 5)
            {
                TryKill(fightVal - fightThreshhold.Value);
            }

            lifeCounter += Game1.delta;
            if (output[2] > AceThreshhold.Value && GetSpecies() != null && energy > 40)
            {
                Mitosis();
            }
            if (output[2] > SexThreshhold.Value && GetSpecies() != null)
            {
                TryHaveSex();
            }


            rotation = velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void CalculateContinuousFitness()
        {
            Fitness = (energy / maxEnergy) + (velocity.Length() / Speed.Value) + kids + (health / maxHealth);

            if (velocity.Length() < 10 || !IsActive())
                Fitness = -999;
        }

        public override void CalculateCurrentFitness()
        {
            Fitness = (energy / maxEnergy) + (velocity.Length() / Speed.Value) + kids + (health / maxHealth);
            if (velocity.Length() < 10 || !IsActive())
                Fitness = -999;
        }

        public override void OnKill()
        {
            color = Color.Gray;
            if (GetSpecies() != null)
                GetSpecies().clients.Remove(this);
            if (sim != null && sim.Agents.Contains(this))
                sim.Agents.Remove(this);
            //CellManager.cells.Remove(this);
        }

        public override void Refresh()
        {
            //CellManager.cells.Add(this);
            foundFood = false;
        }

        public void TryKill(float effort)
        {
            var nearestCell = CellManager.cells.Where(n => n != this && CollisionHelper.CheckBoxvBoxCollision(n.Center, n.size, Center, size) && n.health < energy && n.lifeCounter > 5).OrderBy(n => n.health).FirstOrDefault();
            if (nearestCell != default)
            {
                float damage = Game1.delta * effort * 100 * Scale.Value;
                energy -= damage;
                if (energy < 0)
                    return;

                nearestCell.health -= damage;
                if (nearestCell.health < 0)
                {
                    float hunger = maxEnergy - energy;

                    float toEat = MathF.Min(hunger, nearestCell.energy);
                    energy += toEat;
                    kills++;
                    nearestCell.Kill();
                    Debug.WriteLine("Kill at " + ((int)position.X).ToString() + "," + ((int)position.Y).ToString());
                }
            }
        }

        public void TryHaveSex()
        {
            if (GetSpecies().Size() > 1)
            {
                var partner = CellManager.cells.Where(n => n != this && n.IsActive() && CollisionHelper.CheckBoxvBoxCollision(n.Center, n.size, Center, size) && Distance(n) < GetGenome().Neat.CP).FirstOrDefault();
                if (partner != default && partner.energy > 100 && partner.kids < 5)
                {
                    Debug.WriteLine("Sex at " + ((int)position.X).ToString() + "," + ((int)position.Y).ToString());
                    Cell child = new Cell(color, size, position, MathHelper.Lerp(energy, partner.energy, 0.5f) * ChildEnergy.Value * 0.5f);
                    child.SetGenome(GetSpecies().Breed(partner, this));
                    child.SetSpecies(GetSpecies());

                    sim.neatHost.GetClients().Add(child);

                    child.sim = sim;
                    sim.Agents.Add(child);

                    child.Speed = Speed.Combine(partner.Speed);

                    child.AceThreshhold = AceThreshhold.Combine(partner.AceThreshhold);

                    child.SexThreshhold = SexThreshhold.Combine(partner.SexThreshhold);

                    child.ChildEnergy = ChildEnergy.Combine(partner.ChildEnergy);

                    child.Scale = ChildEnergy.Combine(partner.Scale);

                    child.Red = Red.Combine(partner.Red);
                    child.Green = Green.Combine(partner.Green);
                    child.Blue = Blue.Combine(partner.Blue);

                    child.fightThreshhold = fightThreshhold.Combine(partner.fightThreshhold);

                    child.regenRate= regenRate.Combine(partner.regenRate);

                    child.health = child.maxHealth;

                    partner.energy *= 1.0f - (ChildEnergy.Value * 0.5f);
                    energy *= 1.0f - (ChildEnergy.Value * 0.5f);
                    partner.kids++;
                    kids++;
                }
            }
        }

        public void Mitosis()
        {
            Debug.WriteLine("Mitosis at " + ((int)position.X).ToString() + "," + ((int)position.Y).ToString());
            Cell child = new Cell(color, size, position, energy * ChildEnergy.Value * 0.5f);
            if (GetSpecies().Size() > 0)
            {
                child.SetGenome(GetSpecies().Breed());
                child.SetSpecies(GetSpecies());
            }
            else
            {
                child.Kill();
                return;
            }
            sim.neatHost.GetClients().Add(child);

            energy -= child.energy * 2;
            child.sim = sim;
            sim.Agents.Add(child);

            child.Speed = Speed.Duplicate();

            child.AceThreshhold = AceThreshhold.Duplicate();

            child.SexThreshhold = SexThreshhold.Duplicate();

            child.ChildEnergy = ChildEnergy.Duplicate();

            child.Scale = Scale.Duplicate();

            child.Red = Red.Duplicate();

            child.Green = Green.Duplicate();

            child.Blue = Blue.Duplicate();

            child.fightThreshhold = fightThreshhold.Duplicate();

            child.regenRate = regenRate.Duplicate();

            child.health = child.maxHealth;
            kids++;
        }
    }
}
