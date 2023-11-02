using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EvoSim.Core.NeuralNetworks;
using EvoSim.Core.NeuralNetworks.NEAT;
using EvoSim.Helpers;
using EvoSim.Helpers.HelperClasses;
using EvoSim.Interfaces;
using EvoSim.ProjectContent.Resources;
using EvoSim.ProjectContent.Terrain;
using EvoSim.ProjectContent.Terrain.TerrainTypes;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using EvoSim.ProjectContent.CellStuff.SightRayStuff;
using static EvoSim.ProjectContent.Resources.FoodManager;
using Aardvark.Base;

namespace EvoSim.ProjectContent.CellStuff
{
    public class Cell : NeatAgent
    {
        #region constants

        #endregion
        #region properties

        public static int DEADENERGY = 300;
        public float maxEnergy => (1000 * (scale * scale * scale)) + DEADENERGY;
        public float maxHealth => (100 * (scale * scale)) + 20;

        public float accelerationBase => 250.0f;

        public float EnergyUsage => ((scale * scale) * (((hitWall ? Speed : velocity.Length()) * 0.00003f * MathF.Pow(1.0f / terrainVelocity, 1.4f)) + 1.25f + (0.6f / terrainVelocity) + (0.6f * DamageCapacity) + (SunlightConsumption * 0.0525f) + (RayDistance / 5000f)))

        public float ConsumptionRate => (scale * scale) * 1200f;

        public float SunlightConsumptionRate => (SunlightConsumption * ((scale * scale) + 0.6f) * 200f) / (50f + MathF.Pow((hitWall ? Speed : velocity.Length()) / 6f, 8.5f));
        public float FoodCounterRate => 2f;

        public float TurnRate => 16f;
        #endregion
        #region neural network node info
        public readonly static float UPDATERATE = 0.01f;
        public const int RAYS = 4;
        public static int RAYVALUES => SightRay.OUTPUTNUM;
        public readonly static int TERRAINRANGE = 0;
        public readonly static int MEMORYCELLS = 10;
        public readonly static int ADDITIONALVALUES = 13;
        public static int INPUTNUM => ((RAYS + 1) * RAYVALUES) + ADDITIONALVALUES + (TERRAINRANGE * TERRAINRANGE) + MEMORYCELLS;
        public readonly static int BASICOUTPUT = 9;
        public static int OUTPUTNUM => BASICOUTPUT + MEMORYCELLS;
        #endregion
        #region cell stats
        public float SexLikelihood => cellStats[0].Value;
        public float AceLikelihood => cellStats[1].Value;
        public float Speed => cellStats[2].Value;
        public float ChildScale => cellStats[3].Value;
        public float MaxScale => cellStats[4].Value;
        public float Red => cellStats[5].Value;
        public float Green => cellStats[6].Value;
        public float Blue => cellStats[7].Value;
        public float FightThreshhold => cellStats[8].Value;
        public float RegenRate => cellStats[9].Value;
        public float DeathThreshhold => cellStats[10].Value;
        public float SpawnDistance => cellStats[11].Value;
        public float MutationRate => cellStats[12].Value;
        public float SwimmingProficiency => cellStats[13].Value;

        public float ChildDampen => cellStats[14].Value;

        public float DamageCapacity => cellStats[15].Value;

        public float SunlightConsumption => cellStats[16].Value;

        public float GrowthRate => cellStats[17].Value;

        public bool HasSight => cellStats[18].Value > 0.03f;

        public float RayDistance => 1000 * cellStats[18].Value;

        #endregion
        #region size and dimension stuff
        public float width = 32;
        public float height = 32;

        public Vector2 Size;

        public Vector2 TopLeft => position;

        public Vector2 TopRight => position + new Vector2(Size.X, 0);

        public Vector2 BottomLeft => position + new Vector2(0, Size.Y);

        public Vector2 BottomRight => position + Size;

        public float Left => position.X;

        public float Right => position.X + Size.X;

        public float Top => position.Y;

        public float Bottom => position.Y + Size.Y;

        #endregion
        #region instance Stats
        public Color color;

        public Vector2 position => Center - (Size / 2);
        public Vector2 Center = Vector2.Zero;
        public Vector2 oldCenter = Vector2.Zero;

        public Vector2 velocity;

        public Box2d box = new Box2d();

        public float lifeCounter;

        public float currentGrowthRate;

        public float scale;

        public float energy;
        public float oldEnergy;

        public float health;

        public float rotation;

        public bool dead = false;

        public bool foundSunlight = false;
        public bool foundFood = false;

        public float foodCounter = 0;

        public int kids = 0;
        public int kills = 0;

        public float stillCounter = 0;

        public float oldFitness = 0;

        public bool hitWall = false;

        private float mitosisCounter;

        public Vector2 acceleration = Vector2.Zero;
        public float turnAcceleration;

        public float mateWillingness = 0f;
        private float splitWillingness = 0f;

        private float fightWillingness = 0f;

        public float generation = 0;

        public Vector2 offspringOffset = Vector2.Zero;

        public int horribleParentCounter = 0;

        public float childDampMult = 1.0f;

        public List<Cell> parents = new List<Cell>();

        public List<Cell> mates = new List<Cell>();

        private float terrainVelocity = 0.1f;
        #endregion
        #region miscellaneous data
        public List<CellStat> cellStats= new List<CellStat>();

        public CellNeatSimulation<Cell> sim => SceneManager.cellSimulation;  

        public List<SightRay> sightRays = new List<SightRay>();
        public bool sightRaysInitialized = false;

        public List<Cell> livingChildren = new List<Cell>();

        public IDna network;

        public float[,] localTiles = new float[TERRAINRANGE, TERRAINRANGE];

        private TimeCounter TileRefreshCounter;
        private TimeCounter NetworkTimer;
        private TimeCounter mutationTimer;

        private List<float> memory = new List<float>();

        public IDna[] SightRayDNA = new IDna[RAYS];
        #endregion

        #region initialization methods

        public void BaseInitializer(IDna dna)
        {
            health = maxHealth;

            network = dna;
            NetworkTimer = new TimeCounter(Main.random.NextFloat(0.15f, 0.25f), new CounterAction((object o, ref float counter, float threshhold) =>
            {
                counter = 0;
                sightRays?.ForEach(n => n.UpdateNetwork());
                network.Compute(FeedInputs().ToArray());
                Response(network.Response);
            }));

            TileRefreshCounter = new TimeCounter(Main.random.NextFloat(2f, 3f) * 1000, new CounterAction((object o, ref float counter, float threshhold) =>
            {
                counter = 0;
                UpdateLocalTiles();
            }));

            mutationTimer = new TimeCounter(9999999999, new CounterAction((object o, ref float counter, float threshhold) =>
            {
                counter = 0;
                Mutate();
                cellStats.ForEach(n => n.Mutate());
                sightRays.ForEach(n => n.Mutate());
            }));

            for (int i = 0; i < MEMORYCELLS; i++)
            {
                memory.Add(0);
            }

            SceneManager.cellSimulation?.AddAgent(this);
        }

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<TanhActivationFunction>(25)
                   .AddLayer<TanhActivationFunction>(25)
                   .AddLayer<TanhActivationFunction>(25)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Main.random.NextFloat(-5, 5));

            return network;
        }

        public Cell() : base()
        {
            InitializeCellStats();
            Vector2 pos = Vector2.Zero;
            pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
            pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));
            Center = pos;
            oldCenter = Center;
            color = new Color(0, 0, 1.0f);
            health = maxHealth;
            scale = 1;
            width = 32;
            height = 32;
            Size = new Vector2(width, height) * scale;
            BaseInitializer(Dna);
            energy = maxEnergy;
            box.Size = new V2d(Size.X, Size.Y);
            box.Min = new V2d(position.X, position.Y);
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float scale, int generation, IDna NewDNA = null) : base()
        {
            InitializeCellStats();
            color = _color;
            width = size.X;
            height = size.Y;
            Center = _position;
            oldCenter = Center;
            this.scale = scale;
            health = maxHealth;
            Size = size * scale;
            this.generation = generation;
            BaseInitializer(NewDNA ?? Dna);
            energy = maxEnergy;
            box.Size = new V2d(Size.X, Size.Y);
            box.Min = new V2d(position.X, position.Y);
        }

        private void InitializeCellStats()
        {
            cellStats.Add(new CellStat(150f, 0.5f, 0.1f, 100f, 200f, 2, 1, false)); //sexLikelihood
            cellStats.Add(new CellStat(3f, 0.5f, 0.05f, 1.1f, 10f, 3, 1.5f, false)); //aceLikelihood
            cellStats.Add(new CellStat(0, 80f, 0.3f, 0, 400, 10, 20, false)); //speed
            cellStats.Add(new CellStat(0.25f, 0.03f, 0.004f, 0.1f, 0.5f, 1.5f, 0.5f, false)); //childScale
            cellStats.Add(new CellStat(1.0f, 0.03f, 0.001f, 0.07f, 6f, 3, 10f, true)); //maxScale
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.02f, 0.1f, 1, 1, 3.5f, false)); //red
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.02f, 0.1f, 1, 1, 3.5f, false)); //green
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.02f, 0.1f, 1, 1, 3.5f, false)); //blue
            cellStats.Add(new CellStat(4f, 0.5f, 0.05f, 1f, 10, 2, 0.5f, false)); //fightThreshhold
            cellStats.Add(new CellStat(3f, 0.1f, 0.01f, 0.1f, 10, 3, 0.25f, false)); //regenRate
            cellStats.Add(new CellStat(0.95f, 0.01f, 0.001f, 0.9f, 1.0f, 1, 0f, false)); //deathThreshhold
            cellStats.Add(new CellStat(120, 8f, 0.1f, 50, 250, 3, 1f, false)); //spawnDistance
            cellStats.Add(new CellStat(40 * Main.random.NextFloat(0.8f, 1.2f), 1f, 0.01f, 20, 200, 1, 0f, false)); //mutationRate
            cellStats.Add(new CellStat(0.05f, 0.12f, 0.05f, 0.01f, 0.99f, 40, 3f, false)); //SwimmingProficiency
            cellStats.Add(new CellStat(0.97f, 0.06f, 0.006f, 0.8f, 0.994f, 2, 0.5f, false)); //ChildDampen
            cellStats.Add(new CellStat(1.0f, 0.4f, 0.06f, 0.1f, 10f, 2.5f, 1f, false)); //DamageCapacity
            cellStats.Add(new CellStat(4.5f, 0.05f, 0.005f, 0, 5f, 10, 2f, false)); //SunlightConsumption
            cellStats.Add(new CellStat(0.25f, 0.04f, 0.004f, 0.05f, 0.7f, 1f, 1f, false)); //growthRate
            cellStats.Add(new CellStat(0.02f, 0.05f, 0.01f, 0, 1, 2, 30f, false)); //rayDistance
        }

        public void InitializeRays(Cell fitterParent, Cell lessFitParent)
        {
            sightRays?.ForEach(n => n.Cull());
            List<SightRay> tempSightRays = new List<SightRay>();
            for (int i = 0; i < RAYS; i++)
            {
                Species raySpecies = fitterParent.sightRays[i].GetSpecies();
                IDna newDNA = raySpecies.Breed(fitterParent.sightRays[i], lessFitParent.sightRays[i]);
                tempSightRays.Add(new SightRay((i / (float)RAYS) * 6.28f, this, i, newDNA, raySpecies));
                SightRayDNA[i] = newDNA;
            }
            sightRays = tempSightRays;
            sightRaysInitialized = true;
        }

        public void InitializeRays()
        {
            List<SightRay> tempSightRays = new List<SightRay>();
            for (int i = 0; i < RAYS; i++)
            {
                tempSightRays.Add(new SightRay((i / (float)RAYS) * 6.28f, this, i, default, default));
                SightRayDNA[i] = tempSightRays[i].Dna;
            }
            sightRays = tempSightRays;
            sightRaysInitialized = true;
        }

        #endregion

        #region Behavior methods
        public override void OnUpdate()
        {
            if (energy > maxEnergy)
                energy = maxEnergy;
            if (CorpseLogic())
                return;

            if (HasSight && sightRays.Count == 0)
                InitializeRays();

            if (!HasSight && sightRays.Count > 0)
            {
                sightRays.ForEach(n => n.Kill());
                sightRays.Clear();
            }

            scale += currentGrowthRate * Main.delta;
            scale = MathF.Min(scale, MaxScale);
            Size = new Vector2(width, height) * scale;

            box.Size = new V2d(Size.X, Size.Y);
            box.Min = new V2d(position.X, position.Y);

            terrainVelocity = TerrainVelocity();
            mitosisCounter += Main.delta;
            lifeCounter += Main.delta;

            color = new Color(Red, Green, Blue);

            TileRefreshCounter.Update(this);

            RegenerationLogic();

            energy -= Main.delta * EnergyUsage;

            if (energy <= DEADENERGY)
            {
                color = ColorHelper.deadCellColor;
                Kill();
                return;
            }

            Movement();
            SceneManager.cellSimulation.PList.UpdateObjPos(this);
            oldCenter = Center;
            NetworkTimer.Update(this);
            mutationTimer.Update(this);

            foreach (Cell child in livingChildren.ToArray())
            {
                if (!child.IsActive())
                {
                    livingChildren.Remove(child);
                }
            }

            foreach(Cell parent in parents.ToArray())
            {
                if (!parent.IsActive())
                {
                    parents.Remove(parent);
                }
            }
            FoodInteraction();
            TrySpecificActions();

            if (foundFood && foodCounter < 5)
            {
                foodCounter += FoodCounterRate * Main.delta * 15;
            }
            else if (foodCounter > 0)
            {
                foodCounter -= FoodCounterRate * Main.delta;
            }
            foodCounter = MathHelper.Max(foodCounter, 0);

            hitWall = false;
        }

        public void TrySpecificActions()
        {
            if (mateWillingness > 1 && (500 * (ChildScale * ChildScale * ChildScale)) + DEADENERGY < energy - 40 && lifeCounter > 1)
            {
                if (TryMate(mateWillingness))
                    return;
            }

            if (fightWillingness > 1)
                TryFight();

            float val1 = (1000 * (ChildScale * ChildScale * ChildScale)) + (DEADENERGY * 2);
            float val2 = energy + 60;
            if (splitWillingness > 1 && (1000 * (ChildScale * ChildScale * ChildScale)) + (DEADENERGY * 2) < energy - 60 && lifeCounter > 0.5 && mitosisCounter > 0.25f)
                Mitosis();

        }

        public void RegenerationLogic()
        {
            if (health < maxHealth)
            {
                float regen = RegenRate * Main.delta;
                regen = MathF.Min(regen, maxHealth - health);
                energy -= regen;
                health += regen;
            }
        }

        public bool CorpseLogic()
        {
            if (!IsActive())
            {
                energy -= Main.delta * 4.5f;
                if (energy < 0)
                {
                    if (sim != null && sim.Agents.Contains(this))
                        sim.RemoveAgent(this);

                    if (SceneManager.cellSimulation != null)
                    {
                        if (SceneManager.cellSimulation.Agents.Contains(this))
                            SceneManager.cellSimulation.RemoveAgent(this);

                        //if (GetSpecies() != null && GetSpecies().clients.Contains(this))
                        //    GetSpecies().clients.Remove(this);
                    }
                }
                return true;
            }
            return false;
        }

        public void Movement()
        {
            velocity += acceleration * terrainVelocity * Main.delta;
            velocity = velocity.RotatedBy(turnAcceleration * Main.delta);

            if (velocity.Length() > Speed * terrainVelocity)
            {
                velocity.Normalize();
                velocity *= Speed * terrainVelocity;
            }
            TileCollision();

            Center += velocity * Main.delta;
            rotation = velocity.ToRotation() + MathHelper.PiOver2;
        }

        public float TerrainVelocity() => (InWater() ? SwimmingProficiency : 1.0f - SwimmingProficiency);

        public bool InWater()
        {
            return SceneManager.grid.TileID(Center) == 2;
        }

        public void TileCollision()
        {
            int tries = 0;
            while (tries < 20)
            {
                tries++;
                bool inWall = false;
                double longestLength = 0;
                V2d realShift = new V2d(0, 0);
                Vector2 newVel = velocity;
                for (float i = Left - SceneManager.grid.squareWidth; i <= Right + SceneManager.grid.squareWidth; i += SceneManager.grid.squareWidth)
                {
                    for (float j = Top - SceneManager.grid.squareHeight; j <= Bottom + SceneManager.grid.squareHeight; j += SceneManager.grid.squareHeight)
                    {
                        Vector2 check = new Vector2(i, j);
                        check.Wrap(SceneManager.grid.mapSize.X, SceneManager.grid.mapSize.Y);
                        int x = (int)(check.X / SceneManager.grid.squareWidth);
                        int y = (int)(check.Y / SceneManager.grid.squareHeight);
                        if (SceneManager.grid.TileID(check) == 1)
                        {
                            VectorHelper.WrapPoints(ref x, ref y, SceneManager.grid.gridWidth, SceneManager.grid.gridHeight);
                            TerrainSquare square = SceneManager.grid.terrainGrid[x, y];
                            Vector2 tempVel = velocity;

                            V2d tempShift = CollisionHelper.StopBox(box, square.box, ref tempVel);
                            if (Math.Abs(tempShift.X) > longestLength || Math.Abs(tempShift.Y) > longestLength)
                            {
                                inWall = true;
                                longestLength = Math.Max(Math.Abs(tempShift.X), Math.Abs(tempShift.Y));
                                newVel = tempVel;
                                realShift = tempShift;
                            }
                        }
                    }
                }
                if (inWall)
                {
                    hitWall = true;
                    velocity = newVel;
                    box = box.Translated(realShift);
                    break;
                }
            }

            if (TopLeft.X < 0)
            {
                Center.X += SceneManager.grid.mapSize.X;
            }

            if (TopRight.X > SceneManager.grid.mapSize.X)
            {
                Center.X -= SceneManager.grid.mapSize.X;
            }

            if (TopLeft.Y < 0)
            {
                Center.Y += SceneManager.grid.mapSize.Y;
            }

            if (BottomRight.Y > SceneManager.grid.mapSize.Y)
            {
                Center.Y -= SceneManager.grid.mapSize.Y;
            }
        }

        public void FoodInteraction()
        {
            foundFood = false;
            foundSunlight = false;
            //maybreak
            var corpseFound = SceneManager.cellSimulation.PList.GetList(Center).Where(n => n != this && !n.IsActive() && n.energy > 0 && CollisionHelper.CheckBoxvBoxCollision(position, Size, n.position, n.Size)).FirstOrDefault();
            if (corpseFound != default)
            {
                Cell corpseFoundCast = corpseFound as Cell;
                float hunger = maxEnergy - energy;

                float toEat = MathF.Min(hunger, corpseFoundCast.energy);
                toEat = MathF.Min(toEat, Main.delta * ConsumptionRate);
                corpseFoundCast.energy -= toEat;
                energy += toEat;

                foundFood = true;
            }

            FoodManager.ChangeSpecificPassiveFood(Center, new PassiveFoodAction((ref float i, Point point) =>
            {
                float hunger = maxEnergy - energy;
                float toEat = MathF.Min(hunger, i);
                toEat = MathF.Min(toEat, Main.delta * SunlightConsumptionRate);
                i -= toEat;
                energy += toEat;
                if (toEat < EnergyUsage * Main.delta && toEat < maxEnergy - energy && Main.random.NextBool(60) && velocity.Length() < 20)
                {
                    int y = 0; //debug
                }
                if (toEat > EnergyUsage * Main.delta)
                    foundSunlight = true;
            }));
            return;
            var sunlightFound = FoodManager.foods.GetList(Center).Where(n => CollisionHelper.CheckBoxvBoxCollision(position, Size, n.position, n.size)).FirstOrDefault();

            if (sunlightFound != default)
            {
                float hunger = maxEnergy - energy;

                float toEat = MathF.Min(hunger, sunlightFound.energy);
                toEat = MathF.Min(toEat, Main.delta * ConsumptionRate);
                toEat /= (1.0f + (velocity.Length() * 0.001f * terrainVelocity));
                sunlightFound.energy -= toEat;
                energy += toEat;

                if (sunlightFound.energy <= 0)
                    FoodManager.foods.Remove(sunlightFound);

                foundSunlight = true;
            }
        }
        #endregion

        #region neural network interactions
        public List<float> FeedInputs()
        {
            List<float> inputs = new List<float>();

            float[] raySums = new float[RAYVALUES];

            sightRays?.ForEach(n => n.FeedData(inputs, ref raySums));
            if (sightRays.Count() == 0)
            {
                for (int i = 0; i < RAYS * RAYVALUES; i++)
                {
                    inputs.Add(0);
                }
            }

            for (int i = 0; i < TERRAINRANGE; i++)
            {
                for (int j = 0; j < TERRAINRANGE; j++)
                {
                    inputs.Add(localTiles[i, j]);
                }
            }

            inputs.Add((rotation - 3.14f) / 6.28f);
            inputs.Add(energy * 0.005f);
            inputs.Add(energy / maxEnergy);
            inputs.Add(health / maxHealth);
            inputs.Add(velocity.X * 0.01f);
            inputs.Add(velocity.Y * 0.01f);
            inputs.Add(livingChildren.Count * 0.2f);
            inputs.Add(foundSunlight ? -1 : 1);
            inputs.Add(foundFood ? -1 : 1);
            inputs.Add(lifeCounter / 30f);
            inputs.Add(hitWall ? -5 : 5);
            inputs.Add(InWater() ? -1 : 1);
            inputs.Add(livingChildren.Count() - 1);

            for (int i = 0; i < RAYVALUES; i++)
            {
                inputs.Add(raySums[i]);
            }

            memory.ForEach(n => inputs.Add(n));

            return inputs;
        }

        public void Response(float[] output)
        {

            float x = output[0] - 0.5f;
            float y = output[1] - 0.5f;
            acceleration.X = accelerationBase * MathF.Pow(MathF.Abs(x * 2), 0.4f) * MathF.Sign(x);
            acceleration.Y = accelerationBase * MathF.Pow(MathF.Abs(y * 2), 0.4f) * MathF.Sign(y);
            //acceleration = acceleration.RotatedBy(rotation);

            mateWillingness = output[2] * SexLikelihood * childDampMult;
            splitWillingness = output[3] * AceLikelihood * childDampMult;
            fightWillingness = output[4] * FightThreshhold;
            turnAcceleration= (output[5] - 0.5f) * TurnRate;

            for (int i = 0; i < MEMORYCELLS; i++)
            {
                float m = output[i + BASICOUTPUT] - 0.5f;
                memory[i] += m;
                memory[i] *= 0.97f;
            }

            x = output[6] - 0.5f;
            y = output[7] - 0.5f;
            offspringOffset = SpawnDistance * new Vector2(
                MathF.Pow(MathF.Abs(x * 2), 0.05f) * MathF.Sign(x),
                MathF.Pow(MathF.Abs(y * 2), 0.05f) * MathF.Sign(y));

            currentGrowthRate = output[8] * GrowthRate;
        }

        public override double Distance(NeatAgent other)
        {
            Cell otherCell = other as Cell;
            double ret = GetGenome().Distance(other.GetGenome());
            double ret1 = ret;

            if (sightRays.Count() > 0 && otherCell.sightRays.Count() > 0)
            {
                for (int i = 0; i < RAYS; i++)
                {
                    SightRay localRay = sightRays[i];
                    SightRay otherRay = otherCell.sightRays[i];
                    if (localRay.Dna == null || localRay.Dna is not Genome || otherRay.Dna == null || otherRay.Dna is not Genome)
                        continue;
                    ret += (localRay.BaseDistance(otherRay) / (double)RAYS);
                }
            }
            else
            {
                ret *= 2;
            }

            double ret2 = ret;

            for (int j = 0; j < cellStats.Count(); j++)
            {
                CellStat a = cellStats[j];
                CellStat b = otherCell.cellStats[j];
                ret += ((double)CellStat.Distance(a, b) / (double)cellStats.Count()) * 40;
            }
            if (ret > GetGenome().Neat.CP)
            {
                double ret3 = ret;
            }
            return ret;
        }

        public double BaseDistance(NeatAgent other)
        {
            Cell otherCell = other as Cell;
            double ret = GetGenome().Distance(other.GetGenome());

            for (int j = 0; j < cellStats.Count(); j++)
            {
                CellStat a = cellStats[j];
                CellStat b = otherCell.cellStats[j];
                ret += ((double)CellStat.Distance(a, b) / (double)cellStats.Count()) * 40;
            }
            return ret;
        }

        public override void CalculateCurrentFitness() => Fitness = GetFitness(true);

        public float GetFitness(bool reset, bool forMating = false)
        {
            //if (!IsActive() && forMating)
           // {
            //    return -999;
           // }
            //float fitness = 0;
            float fitness = ((energy / maxEnergy) * 3) + (kills * 7);
            float childrenDistanceThing = 0f;
            livingChildren.ForEach(n => childrenDistanceThing += IdealDistanceCalculation(n.Center.Distance(Center), 0.98f) / 18f);

            float parentDistanceThing = 0f;
            parents.ForEach(n => parentDistanceThing += IdealDistanceCalculation(n.Center.Distance(Center), 0.98f) / 3f);


            fitness += MathF.Min(foodCounter + 1, 15) * 5;
           // fitness *= FitnessLifetimeCorrelation(lifeCounter);

            //fitness *= MathF.Sqrt(energy / maxEnergy);

            if (hitWall)
                fitness -= 10;

            if (foundSunlight)
                fitness += 2f;

            if (foundFood)
                fitness += 20f;

            fitness += (livingChildren.Count * 5f);

            fitness += childrenDistanceThing + parentDistanceThing + (kids * 10);

            fitness *= terrainVelocity * terrainVelocity;

            if (velocity.Length() < 15)
                fitness *= 0.2f;

            if (energy <= DEADENERGY)
                fitness *= 0.8f;

            float newOldFitness = fitness;
            if (reset)
            {
                fitness -= oldFitness;
                oldFitness = newOldFitness;
                hitWall = false;
                for (int i = 0; i < MEMORYCELLS; i++)
                {
                    memory[i] = 0;
                }
                foodCounter = 0;
                kills = 0;
                kids = 0;
                childDampMult = 1;
                oldEnergy = energy;
            }


            if (double.IsNaN(fitness))
            {
                throw new Exception("Fitness is not a number!");
            }

            return fitness;

        }

        private float IdealDistanceCalculation(float val, float exp)
        {
            return val * MathF.Pow(exp, val);
        }

        private float FitnessLifetimeCorrelation(float val)
        {
            return 1;
            if (val < -60)
                return 0;
            return ((0.001f * (val * val)) + (0.062f * val) + 0.1f);
        }

        public override void Refresh()
        {
            //SceneManager.simulation?.Agents.Add(this);
            //hitWall = false;
        }
        #endregion

        #region specific actions
        public void TryFight()
        {
            var nearestCell = sim.PList.GetList(Center).Where(n => n != this && n.IsActive() && !mates.Contains(n) && !parents.Contains(n) && !livingChildren.Contains(n) && n.health < energy && CollisionHelper.CheckBoxvBoxCollision(n.position, n.Size, position, Size)).OrderBy(n => n.health).FirstOrDefault();
            if (nearestCell != default)
            {
                Cell nearestCellCast = nearestCell as Cell;
                float damage = (Main.delta * scale * scale * (DamageCapacity * (1 + MathF.Sqrt(velocity.Length() / 2f))) * 200f);
                energy -= damage;
                if (energy < 0)
                    return;
                nearestCellCast.health -= damage;
                foundFood = true;
                if (nearestCellCast.health < 0)
                {
                    float hunger = maxEnergy - energy;
                    hunger = MathF.Min(hunger, nearestCellCast.energy * 0.4f);
                    energy += hunger;

                    if (livingChildren.Contains(nearestCellCast))
                        horribleParentCounter++;
                    else
                        kills += nearestCellCast.kills + 1;

                    nearestCellCast.Kill();
                    nearestCellCast.energy = 0;
                    Debug.WriteLine("Kill at " + ((int)position.X).ToString() + "," + ((int)position.Y).ToString());
                }
            }
        }

        public bool TryMate(float willingness)
        {
            if (SceneManager.NumCells() > 600)
                return false;

            //maybreak
            var partnerCell = sim.PList.GetList(Center).Where(n => n != this && 
            n.IsActive() && 
            CollisionHelper.CheckBoxvBoxCollision(position, Size, n.position, n.Size) && 
            n.GetSpecies() == GetSpecies() && 
            n.mateWillingness > 1 &&
            !n.livingChildren.Contains(this) &&
            !livingChildren.Contains(n) &&
            //parents.Intersect((n as Cell).parents).Count<Cell>() == 0 && 
            1 < n.GetFitness(false, true) && 
            1 < GetFitness(false, true) &&
            (500 * (n.ChildScale * n.ChildScale * n.ChildScale)) + DEADENERGY < n.energy - 40).OrderBy(n => n.Fitness).LastOrDefault();
            if (partnerCell != default)
            {
                Vector2 newPos = Center + offspringOffset;
                if (SceneManager.grid.TileID(newPos) == 1)
                    return false;

                Debug.WriteLine("Mating at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
                Cell child = new Cell(color, Vector2.One * 32, newPos, MathHelper.Lerp(ChildScale, partnerCell.ChildScale, 0.5f), (int)MathF.Max(generation, partnerCell.generation) + 1);
                child.SetGenome(GetSpecies().Breed(partnerCell, this));
                child.Mutate();
                if (!GetSpecies().Add(child))
                {
                    Species newSpecies = new Species(child);
                    (SceneManager.cellSimulation).neatHost.species.Add(newSpecies);
                    child.SetSpecies(newSpecies);
                }

                sim.AddAgent(child);

                for (int i = 0; i < cellStats.Count; i++)
                {
                    child.cellStats[i] = cellStats[i].Combine(partnerCell.cellStats[i]);
                }

                child.health = child.maxHealth;

                child.parents.Add(this);
                child.parents.Add(partnerCell);

                partnerCell.energy -= (child.maxEnergy * 0.5f);
                energy -= (child.maxEnergy * 0.5f);
                partnerCell.kids++;
                kids++;

                partnerCell.livingChildren.Add(child);
                livingChildren.Add(child);

                childDampMult *= ChildDampen;
                SceneManager.successfulMates++;
                if (child.HasSight)
                {
                    if (partnerCell.sightRays.Count() == 0 && sightRays.Count() == 0)
                    {
                        child.InitializeRays();
                    }
                    else if (partnerCell.sightRays.Count() > 0 && sightRays.Count() == 0)
                    {
                        child.InitializeRays(partnerCell, partnerCell);
                    }
                    else if (partnerCell.sightRays.Count() == 0 && sightRays.Count() > 0)
                    {
                        child.InitializeRays(this, this);
                    }
                    else if (partnerCell.Fitness > Fitness)
                    {
                        child.InitializeRays(partnerCell, this);
                    }
                    else
                    {
                        child.InitializeRays(this, partnerCell);
                    }
                }

                mates.Add(partnerCell);
                return true;
            }
            return false;
        }

        public void Mitosis()
        {
            if (SceneManager.NumCells() > 600)
                return;
            if (GetSpecies() == null)
            {
                Debug.WriteLine("Failed mitosis from generation " + generation.ToString() + " type 1");
                return;
            }
            Vector2 newPos = Center + offspringOffset;
            if (SceneManager.grid.TileID(newPos) == 1)
                return;
            Debug.WriteLine("Mitosis at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
            Cell child = new Cell(color, Vector2.One * 32, newPos, ChildScale, (int)generation + 1);

            for (int i = 0; i < cellStats.Count; i++)
            {
                child.cellStats[i] = cellStats[i].Duplicate();
            }
            child.scale = ChildScale;

            child.health = child.maxHealth;

            child.parents.Add(this);

            livingChildren.Add(child);

            if (child.HasSight && HasSight)
            {
                child.InitializeRays(this, this);
            }

            if (GetSpecies().Size() > 0)
            {
                child.SetGenome(Dna as Genome);
                child.Mutate();
                if (!GetSpecies().Add(child))
                {
                    Species newSpecies = new Species(child);
                    (SceneManager.cellSimulation).neatHost.species.Add(newSpecies);
                    child.SetSpecies(newSpecies);
                }
            }
            else
            {
                Debug.WriteLine("Failed mitosis from generation " + generation.ToString() + " type 2");
                child.Kill();
                return;
            }

            if (child.HasSight && !HasSight)
            {
                child.InitializeRays();
            }
            energy -= child.maxEnergy;

            if (energy < DEADENERGY)
            {
                int y = 0;
            }
            mitosisCounter = 0;

            sim.AddAgent(child);
            childDampMult *= ChildDampen;
            //kids++;
        }
        #endregion

        public void Draw(SpriteBatch spriteBatch)
        { 
            string text = /*"Species: " + speciesString +*/"Energy: " + ((int)energy).ToString() + "\nHas sight: " + HasSight.ToString() + "\nChildren: " + livingChildren.Count.ToString() + "\nFitness: " + GetFitness(false).ToString() + "\nMitosis likelihood: " + AceLikelihood;
            if (IsActive())
                DrawHelper.DrawText(spriteBatch, text, ColorHelper.textColor, position - new Vector2(0, 120), Vector2.One);
            DrawHelper.DrawPixel(spriteBatch, color, position, Vector2.Zero, width * scale, height * scale);
        }

        public override void OnKill()
        {
            color = ColorHelper.deadCellColor;
            //if (GetSpecies() != null)
            //    GetSpecies().clients.Remove(this);

            sightRays?.ForEach(n => n.Kill());
            //SceneManager.simulation?.Agents.Remove(this);
        }

        public bool ToCull()
        {
            if (!SceneManager.cellSimulation.Agents.Contains(this))
            {
                return true;
            }
            return false;
        }

        public override void Inherit(NeatAgent other)
        {
            Cell otherCell = other as Cell;
            for (int i = 0; i < cellStats.Count; i++)
            {
                cellStats[i] = otherCell.cellStats[i].Duplicate();
            }
        }

        private void UpdateLocalTiles()
        {
            int tileRadius = TERRAINRANGE / 2;
            for (int i = -tileRadius; i < tileRadius; i++)
            {
                for (int j = -tileRadius; j < tileRadius; j++)
                {
                    Vector2 originTile = Center / SceneManager.grid.squareSize;

                    int coordX = (int)(originTile.X + i);
                    int coordY = (int)(originTile.Y + j);
                    if (SceneManager.grid.InGrid(coordX, coordY))
                    {
                        TerrainSquare square = SceneManager.grid.terrainGrid[coordX, coordY];
                        localTiles[i + tileRadius, j + tileRadius] = square.ID * 100;
                    }
                    else
                        localTiles[i + tileRadius, j + tileRadius] = 0;
                }
            }
        }
    }
}
