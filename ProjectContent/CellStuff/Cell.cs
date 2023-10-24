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
using SharpDX.Direct3D9;
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
        public readonly static int RAYS = 16;
        public readonly static int RAYVALUES = 8;
        public readonly static int TERRAINRANGE = 4;
        public readonly static int ADDITIONALVALUES = 7;
        public static int INPUTNUM => (RAYS * RAYVALUES) + ADDITIONALVALUES + (TERRAINRANGE * TERRAINRANGE);
        public readonly static int OUTPUTNUM = 4;
        public float lifeCounter;
        public float AceThreshhold => cellStats[0].Value;
        public float SexThreshhold => cellStats[1].Value;
        public float Speed => cellStats[2].Value;
        public float ChildEnergy => cellStats[3].Value;
        public float Scale => cellStats[4].Value;
        public float Red => cellStats[5].Value;
        public float Green => cellStats[6].Value;
        public float Blue => cellStats[7].Value;
        public float FightThreshhold => cellStats[8].Value;
        public float RegenRate => cellStats[9].Value;
        public float DeathThreshhold => cellStats[10].Value;
        public float SpawnDistance => cellStats[11].Value;

        public List<CellStat> cellStats= new List<CellStat>();

        public float width;

        public CellNeatSimulation<Cell> sim => CellManager.simulation as CellNeatSimulation<Cell>;
        public float height;

        public float energy;

        public float health;

        public float maxEnergy => 1000 * Scale;
        public float maxHealth => 50 * Scale;

        public Color color;

        public Vector2 position;

        public float accelerationBase=> 10.0f;

        public float bearings = 0f;

        public float rotation;

        public float timeLived = 0f;


        public bool dead = false;

        public bool foundFood = false;

        public int kids = 0;
        public int kills = 0;

        public float stillCounter = 0;

        public bool hitWall = false;

        private float mitosisCounter;

        public Vector2 acceleration = Vector2.Zero;

        public Vector2 Size;

        public Vector2 Center
        {
            get
            {
                return position + (Size / 2.0f);
            }
            set
            {
                position = value - (Size / 2.0f);
            }
        }

        public Vector2 TopLeft => position;

        public Vector2 TopRight => position + new Vector2(Size.X, 0);

        public Vector2 BottomLeft => position + new Vector2(0, Size.Y);

        public Vector2 BottomRight => position + Size;

        public float Left => position.X;

        public float Right => position.X + Size.X;

        public float Top => position.Y;

        public float Bottom => position.Y + Size.Y;

        public float EnergyUsage => (Scale * velocity.Length() * 0.00004f) + 15;

        public Vector2 velocity;

        public List<SightRay> sightRays = new List<SightRay>();

        public List<Cell> children = new List<Cell>();

        public IDna network;

        public float[,] localTiles = new float[TERRAINRANGE, TERRAINRANGE];
        private float tileRefreshCounter = 0;
        private float tileRefreshRate = 1f;

        private float networkUpdateRate = 1f;
        private float networkUpdateCounter = 0;

        private float reproductionWillingness = 0f;

        private float fightWillingness = 0f;

        public float generation = 0;

        public Cell() : base()
        {
            InitializeCellStats();
            Vector2 pos = Vector2.Zero;
            pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
            pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
            position = pos;
            color = new Color(0, 0, 1.0f);
            health = maxHealth;
            Size = new Vector2(width, height) * Scale;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;

            CellManager.cells.Add(this);

            networkUpdateRate = Game1.random.NextFloat(0.4f, 0.6f);
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
            InitializeCellStats();
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            health = maxHealth;
            Size = size * Scale;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;
            networkUpdateRate = Game1.random.NextFloat(0.4f, 0.6f);

            CellManager.cells.Add(this);
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy) : base()
        {
            InitializeCellStats();
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            health = maxHealth;
            Size = size * Scale;

            for (int i = 0; i < RAYS; i++)
            {
                sightRays.Add(new SightRay((i / (float)RAYS) * 6.28f));
            }

            network = Dna;
            networkUpdateRate = Game1.random.NextFloat(0.4f, 0.6f);
            CellManager.cells.Add(this);
        }

        private void InitializeCellStats()
        {
            cellStats.Add(new CellStat(0.5f, 0.01f, 0.0005f, 0.25f, 0.9f, false)); //aceThreshhold
            cellStats.Add(new CellStat(0.3f, 0.01f, 0.0005f, 0.05f, 1, false)); //sexThreshhold
            cellStats.Add(new CellStat(200f, 0.1f, 0.01f, 20, 400, true)); //speed
            cellStats.Add(new CellStat(0.3f, 0.01f, 0.001f, 0.1f, 0.4f, false)); //childEnergy
            cellStats.Add(new CellStat(1.0f, 0.15f, 0.005f, 0.35f, 4f, true)); //scale
            cellStats.Add(new CellStat(1f, 0.25f, 0.001f, 0.1f, 1, false)); //red
            cellStats.Add(new CellStat(1f, 0.25f, 0.001f, 0.1f, 1, false)); //green
            cellStats.Add(new CellStat(0.1f, 0.25f, 0.001f, 0.1f, 1, false)); //blue
            cellStats.Add(new CellStat(0.4f, 0.01f, 0.0001f, 0.1f, 1, false)); //fightThreshhold
            cellStats.Add(new CellStat(1, 0.005f, 0.0001f, 0.1f, 10, true)); //regenRate
            cellStats.Add(new CellStat(0.95f, 0.001f, 0.0001f, 0.9f, 1.0f, false)); //deathThreshhold
            cellStats.Add(new CellStat(150, 5, 0.001f, 5, 500, false)); //spawnDistance
        }

        public override void OnUpdate()
        {
            if (!IsActive())
            {
                energy -= Game1.delta * 20f;
                if (energy < 0)
                {
                    if (sim != null && sim.Agents.Contains(this))
                        sim.Agents.Remove(this);
                }

                return;
            }
            mitosisCounter += Game1.delta;
            tileRefreshCounter += Game1.delta;
            if (tileRefreshCounter > tileRefreshRate)
            {
                tileRefreshCounter = 0;
                int tileRadius = TERRAINRANGE / 2;
                for (int i = -tileRadius; i < tileRadius; i++)
                {
                    for (int j = -tileRadius; j < tileRadius; j++)
                    {
                        Vector2 originTile = Center / TerrainManager.squareSize;

                        int coordX = (int)(originTile.X + i);
                        int coordY = (int)(originTile.Y + j);
                        if (TerrainManager.InGrid(coordX, coordY))
                        {
                            TerrainSquare square = TerrainManager.terrainGrid[coordX, coordY];
                            if (square is RockSquare)
                                localTiles[i + tileRadius, j + tileRadius] = 20;
                            else
                                localTiles[i + tileRadius, j + tileRadius] = 0;
                        }
                        else
                            localTiles[i + tileRadius, j + tileRadius] = 20;
                    }
                }
            }
            if (health < maxHealth)
            {
                float regen = RegenRate * Game1.delta;
                regen = MathF.Min(regen, maxHealth - health);
                energy -= regen;
                health += regen;
            }
            energy -= Game1.delta * EnergyUsage * (CollisionHelper.CheckBoxvBoxCollision(position, Size, Vector2.Zero, TerrainManager.mapSize) ? 1.0f : 10.0f);
            timeLived += Game1.delta;

            color = new Color(Red, Green, Blue);
            if (energy <= 50)
            {
                color = Color.LimeGreen;
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

            string text = /*"Species: " + speciesString +*/"Energy: " + ((int)energy).ToString() + "\nHealth:" + ((int)health).ToString()+ "/" + ((int)maxHealth).ToString() + "\nChildren: " + children.Count.ToString() + "\nFitness: " + Fitness.ToString() + "\nKills: " + kills.ToString();
            DrawHelper.DrawText(spriteBatch, text, Color.Black, position - new Vector2(0, 90), Vector2.One);
            DrawHelper.DrawPixel(spriteBatch, color, position, width * Scale, height * Scale);
        }

        public void AI()
        {
            networkUpdateCounter += Game1.delta;
            if (networkUpdateCounter > networkUpdateRate)
            {
                networkUpdateCounter = 0;
                sightRays.ForEach(n => n.CastRay(this));
                network.Compute(FeedInputs().ToArray());
                Response(network.Response);
            }

            velocity += acceleration;

            if (velocity.Length() > Speed)
            {
                velocity.Normalize();
                velocity *= Speed;
            }

            for (float i = Left - TerrainManager.squareWidth; i <= Right + TerrainManager.squareWidth; i += TerrainManager.squareWidth)
            {
                for (float j = Top - TerrainManager.squareHeight; j <= Bottom + TerrainManager.squareHeight; j += TerrainManager.squareHeight)
                {
                    int x = (int)((i + (TerrainManager.squareWidth / 2)) / TerrainManager.squareWidth);
                    int y = (int)((j + (TerrainManager.squareHeight / 2)) / TerrainManager.squareHeight);
                    if (TerrainManager.InGrid(x, y) && TerrainManager.terrainGrid[x, y] is RockSquare)
                    {
                        if (CollisionHelper.CheckBoxvBoxCollision(Center, Size, new Vector2(x + 0.5f, y + 0.5f) * TerrainManager.squareSize, TerrainManager.squareSize))
                        {
                            Center = CollisionHelper.StopBox(Center, Size, new Vector2(x + 0.5f, y + 0.5f) * TerrainManager.squareSize, TerrainManager.squareSize, ref velocity);
                            hitWall = true;
                        }
                    }
                }
            }

            position += velocity * Game1.delta;

            if (fightWillingness > FightThreshhold && lifeCounter > 5)
            {
                TryKill(fightWillingness - FightThreshhold);
            }

            lifeCounter += Game1.delta;
            if (reproductionWillingness > AceThreshhold && GetSpecies() != null && energy > 40 && children.Count < 7 && mitosisCounter > 3)
            {
                Mitosis();
            }
            if (reproductionWillingness > SexThreshhold && GetSpecies() != null && children.Count < 7)
            {
                TryMate();
            }


            rotation = velocity.ToRotation() + MathHelper.PiOver2;


            if (TopLeft.X < 0)
            {
                position.X = 0;
                velocity.X = 0;
                hitWall = true;
            }

            if (TopRight.X > TerrainManager.mapSize.X)
            {
                position.X = TerrainManager.mapSize.X - Size.X;
                velocity.X = 0;
                hitWall = true;
            }

            if (TopLeft.Y < 0)
            {
                position.Y = 0;
                velocity.Y = 0;
                hitWall = true;
            }

            if (BottomRight.Y > TerrainManager.mapSize.Y)
            {
                position.Y = TerrainManager.mapSize.Y - Size.Y;
                velocity.Y = 0;
                hitWall = true;
            }         

            foreach (Cell child in children.ToArray())
            {
                if (!child.IsActive())
                {
                    children.Remove(child);
                }
            }
        }

        public void FoodInteraction()
        {
            var corpseFound = CellManager.cells.Where(n => n != this && n.energy > 0 && !n.IsActive() && CollisionHelper.CheckBoxvBoxCollision(Center, Size, n.Center, n.Size)).FirstOrDefault();
            if (corpseFound != default)
            {
                float hunger = maxEnergy - energy;

                float toEat = MathF.Min(hunger, corpseFound.energy);
                corpseFound.energy -= toEat;
                energy += toEat;

                foundFood = true;
            }

            if (velocity.Length() > 10 && !CellManager.trainingMode)
                return;
            var foodFound = FoodManager.foods.Where(n => CollisionHelper.CheckBoxvBoxCollision(Center, Size, n.Center, n.size)).FirstOrDefault();

            if (foodFound != default)
            {
                float hunger = maxEnergy - energy;

                float toEat = MathF.Min(hunger, foodFound.energy);
                foodFound.energy -= toEat;
                energy += toEat;

                if (foodFound.energy <= 0)
                    FoodManager.foods.Remove(foodFound);

                //foodFound.color.R = (byte)(255 * MathF.Sqrt(foodFound.energy / 20f));

                foundFood = true;
            }
        }

        public List<float> FeedInputs()
        {
            List<float> sight = new List<float>();

            sightRays.ForEach(n => n.FeedData(sight));

            for (int i = 0; i < TERRAINRANGE; i++)
            {
                for (int j = 0; j < TERRAINRANGE; j++)
                {
                    sight.Add(localTiles[i, j]);
                }
            }

            sight.Add(rotation);
            sight.Add(energy);
            sight.Add(lifeCounter);
            sight.Add(health);
            sight.Add(velocity.X);
            sight.Add(velocity.Y);
            sight.Add(children.Count * 20);

            return sight;
        }

        public void Response(float[] output)
        {
            acceleration.X = accelerationBase * (output[0] - 0.5f);
            acceleration.Y = accelerationBase * (output[1] - 0.5f);
            //acceleration = acceleration.RotatedBy(rotation);

            reproductionWillingness = output[2];
            fightWillingness = output[3];

            /*if (output[4] > deathThreshhold.Value && children.Count > 5)
            {
                Kill();
            }*/
        }

        public override void CalculateContinuousFitness()
        {
            Fitness = (energy / maxEnergy) + (children.Count * 1.3f) + (health / maxHealth) + (generation * 0.1f) + (kids * 0.5f) + (kills * 10);
            Fitness /= MathF.Sqrt(MathF.Sqrt(lifeCounter + 1));
            Fitness *= MathF.Sqrt(energy / maxEnergy);

            if (hitWall)
            {
                Fitness -= 10;
            }

            if (energy <= 0)
            {
                Fitness -= 10;
            }

            if (velocity.Length() < 1)
                Fitness -= 10;
        }

        public override void CalculateCurrentFitness()
        {
            Fitness = (energy / maxEnergy) + (children.Count * 1.3f) + (health / maxHealth) + (generation * 0.1f) + (kids * 0.5f) + (kills * 10);
            Fitness /= MathF.Sqrt(MathF.Sqrt(lifeCounter + 1));
            Fitness *= MathF.Sqrt(energy / maxEnergy);

            if (hitWall)
            {
                Fitness -= 10;
            }

            if (energy <= 0)
            {
                Fitness -= 10;
            }

            hitWall = false;
            foundFood = false;

            if (velocity.Length() < 1)
                Fitness -= 10;
        }

        public override void OnKill()
        {
            color = Color.LimeGreen;
            if (GetSpecies() != null)
                GetSpecies().clients.Remove(this);
            //CellManager.cells.Remove(this);
        }

        public override void Refresh()
        {
            //CellManager.cells.Add(this);
            hitWall = false;
            foundFood = false;
        }

        public void TryKill(float effort)
        {
            var nearestCell = CellManager.cells.Where(n => n != this && CollisionHelper.CheckBoxvBoxCollision(n.Center, n.Size, Center, Size) && n.health < energy && n.lifeCounter > 5).OrderBy(n => n.health).FirstOrDefault();
            if (nearestCell != default)
            {
                float damage = Game1.delta * effort * 1000 * Scale;
                energy -= damage;
                if (energy < 0)
                    return;

                nearestCell.health -= damage;
                if (nearestCell.health < 0)
                {
                    float hunger = maxEnergy - energy;

                    float toEat = MathF.Min(hunger, nearestCell.energy);
                    energy += toEat;
                    kills+= nearestCell.kills + 1;
                    nearestCell.Kill();
                    Debug.WriteLine("Kill at " + ((int)position.X).ToString() + "," + ((int)position.Y).ToString());
                }
            }
        }

        public void TryMate()
        {
            if (true)
            {
                var partner = CellManager.cells.Where(n => n != this && n.IsActive() && (Center.Distance(n.Center) + n.Size.Length() + Size.Length()) < SpawnDistance && Distance(n) < GetGenome().Neat.CP * 10).FirstOrDefault();
                if (partner != default && partner.energy > 100 && partner.kids < 5)
                {
                    Vector2 newPos = position + (SpawnDistance * Game1.random.NextFloat()).ToRotationVector2();
                    Debug.WriteLine("Mating at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
                    Cell child = new Cell(color, Size, newPos, MathHelper.Lerp(energy, partner.energy, 0.5f) * ChildEnergy);
                    child.SetGenome(GetSpecies().Breed(partner, this));
                    child.SetSpecies(GetSpecies());

                    sim.neatHost.GetClients().Add(child);

                    sim.Agents.Add(child);

                    for (int i = 0; i < cellStats.Count; i++)
                    {
                        child.cellStats[i] = cellStats[i].Combine(partner.cellStats[i]);
                    }

                    child.health = child.maxHealth;

                    child.generation = MathF.Max(generation, partner.generation) + 1;

                    partner.energy *= 1.0f - (ChildEnergy * 0.15f);
                    energy *= 1.0f - (ChildEnergy * 0.15f);
                    partner.kids++;
                    kids++;

                    partner.children.Add(child);
                    children.Add(child);

                    child.Mutate();
                }
            }
        }

        public void Mitosis()
        {
            Vector2 newPos = position + SpawnDistance * (Game1.random.NextFloat(0.0f,1.0f)).ToRotationVector2();
            Debug.WriteLine("Mitosis at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
            Cell child = new Cell(color, Size, newPos, energy * ChildEnergy);
            energy -= child.energy * 0.25f;
            mitosisCounter = 0;
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

            sim.Agents.Add(child);

            for (int i = 0; i < cellStats.Count; i++) 
            {
                child.cellStats[i] = cellStats[i].Duplicate();
            }

            child.health = child.maxHealth;

            child.generation = generation + 1;

            child.Mutate();

            children.Add(child);
            kids++;
        }
    }
}
