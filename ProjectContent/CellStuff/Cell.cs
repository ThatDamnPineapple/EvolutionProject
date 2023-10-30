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

namespace EvoSim.ProjectContent.CellStuff
{
    public class Cell : NeatAgent
    {
        #region constants

        #endregion
        #region properties

        public static int DEADENERGY = 125;
        public float maxEnergy => (1000 * (Scale * Scale * Scale)) + DEADENERGY;
        public float maxHealth => (100 * (Scale * Scale)) + 20;

        public float accelerationBase => 80.0f;

        public float EnergyUsage => ((Scale * Scale) * velocity.Length() * 0.0000005f * MathF.Pow(1.0f / terrainVelocity, 4.4f)) + 2.25f + (0.6f / terrainVelocity) + (0.6f * DamageCapacity);

        public float ConsumptionRate => (Scale * Scale) * 400f;

        public float FoodCounterRate => 2f;

        public float TurnRate => 8f;
        #endregion
        #region neural network node info
        public readonly static float UPDATERATE = 0.01f;
        public const int RAYS = 16;
        public static int RAYVALUES => SightRay.OUTPUTNUM;
        public readonly static int TERRAINRANGE = 0;
        public readonly static int MEMORYCELLS = 10;
        public readonly static int ADDITIONALVALUES = 11;
        public static int INPUTNUM => ((RAYS + 1) * RAYVALUES) + ADDITIONALVALUES + (TERRAINRANGE * TERRAINRANGE) + MEMORYCELLS;
        public readonly static int BASICOUTPUT = 8;
        public static int OUTPUTNUM => BASICOUTPUT + MEMORYCELLS;
        #endregion
        #region cell stats
        public float SexLikelihood => cellStats[0].Value;
        public float AceLikelihood => cellStats[1].Value;
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
        public float MutationRate => cellStats[12].Value;
        public float SwimmingProficiency => cellStats[13].Value;

        public float ChildDampen => cellStats[14].Value;

        public float DamageCapacity => cellStats[15].Value;

        #endregion
        #region size and dimension stuff
        public float width;
        public float height;

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

        public float lifeCounter;

        public float energy;

        public float health;

        public float rotation;

        public bool dead = false;

        public bool foundSunlight = false;
        public bool foundFood = false;

        public float foodCounter = 0;
        public float sunlightCounter = 0;

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
            NetworkTimer = new TimeCounter(Main.random.NextFloat(0.05f, 0.1f), new CounterAction((object o, ref float counter, float threshhold) =>
            {
                counter = 0;
                sightRays.ForEach(n => n.UpdateNetwork());
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
                   .AddLayer<TanhActivationFunction>(31)
                   .AddLayer<TanhActivationFunction>(31)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Main.random.NextFloat(-40, 40));

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
            Size = new Vector2(width, height) * Scale;
            InitializeRays();
            BaseInitializer(Dna);
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy, int generation, IDna NewDNA = null) : base()
        {
            InitializeCellStats();
            color = _color;
            width = size.X;
            height = size.Y;
            Center = _position;
            oldCenter = Center;
            energy = _energy;
            health = maxHealth;
            Size = size * Scale;
            if (generation == 0)
                InitializeRays();
            this.generation = generation;
            BaseInitializer(NewDNA ?? Dna);
        }

        private void InitializeCellStats()
        {
            cellStats.Add(new CellStat(150f, 0.2f, 0.2f, 100f, 200f, false)); //sexLikelihood
            cellStats.Add(new CellStat(30f, 0.1f, 0.1f, 5f, 50f, false)); //aceLikelihood
            cellStats.Add(new CellStat(300, 1f, 0.1f, 0, 400, false)); //speed
            cellStats.Add(new CellStat(0.25f, 0.02f, 0.001f, 0.05f, 0.35f, false)); //childEnergy
            cellStats.Add(new CellStat(1.0f, 0.2f, 0.001f, 0.07f, 6f, false)); //scale
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.001f, 0.1f, 1, false)); //red
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.001f, 0.1f, 1, false)); //green
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.001f, 0.1f, 1, false)); //blue
            cellStats.Add(new CellStat(4f, 0.1f, 0.001f, 1f, 10, false)); //fightThreshhold
            cellStats.Add(new CellStat(10f, 0.05f, 0.001f, 1f, 30, true)); //regenRate
            cellStats.Add(new CellStat(0.95f, 0.001f, 0.0001f, 0.9f, 1.0f, false)); //deathThreshhold
            cellStats.Add(new CellStat(80, 0.15f, 0.0001f, 15, 150, false)); //spawnDistance
            cellStats.Add(new CellStat(40 * Main.random.NextFloat(0.8f, 1.2f), 1f, 0.01f, 20, 200, false)); //mutationRate
            cellStats.Add(new CellStat(0.35f, 0.05f, 0.01f, 0.01f, 0.99f, false)); //SwimmingProficiency
            cellStats.Add(new CellStat(0.97f, 0.01f, 0.001f, 0.8f, 0.994f, false)); //ChildDampen
            cellStats.Add(new CellStat(3.0f, 0.4f, 0.01f, 0.1f, 10f, false)); //DamageCapacity
        }

        public void InitializeRays(Cell fitterParent, Cell lessFitParent)
        {
            sightRays.ForEach(n => n.Cull());
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
            Size = new Vector2(width, height) * Scale;
            if (CorpseLogic())
                return;

            terrainVelocity = TerrainVelocity();
            mitosisCounter += Main.delta;
            lifeCounter += Main.delta;

            color = new Color(Red, Green, Blue);

            TileRefreshCounter.Update(this);

            RegenerationLogic();

            energy -= Main.delta * EnergyUsage;

            if (energy <= DEADENERGY)
            {
                color = StaticColors.deadCellColor;
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
            if (foundSunlight && sunlightCounter < 15)
            {
                sunlightCounter += (FoodCounterRate * Main.delta) * (450f / (450.0f + (velocity.Length() / MathF.Pow(terrainVelocity, 1.5f))));
            }
            else if (sunlightCounter > 0)
            {
                sunlightCounter -= FoodCounterRate * Main.delta;
            }
            sunlightCounter = MathHelper.Max(sunlightCounter, 0);

            if (foundFood && foodCounter < 5)
            {
                foodCounter += FoodCounterRate * Main.delta * 5;
            }
            else if (foodCounter > 0)
            {
                foodCounter -= FoodCounterRate * Main.delta;
            }
            foodCounter = MathHelper.Max(foodCounter, 0);
        }

        public void TrySpecificActions()
        {
            if (mateWillingness > 1 && (energy - DEADENERGY) * ChildEnergy > 50 && lifeCounter > 1)
            {
                if (TryMate(mateWillingness))
                    return;
            }

            if (fightWillingness > 1)
                TryFight();

            if (splitWillingness > 1 && (energy - DEADENERGY) * ChildEnergy > 80 + DEADENERGY && lifeCounter > 1 && mitosisCounter > 0.5f)
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
                energy -= Main.delta * 0.5f;
                if (energy < 0)
                {
                    if (sim != null && sim.Agents.Contains(this))
                        sim.RemoveAgent(this);

                    if (SceneManager.cellSimulation != null)
                    {
                        if (SceneManager.cellSimulation.Agents.Contains(this))
                            SceneManager.cellSimulation.RemoveAgent(this);

                        if (GetSpecies() != null && GetSpecies().clients.Contains(this))
                            GetSpecies().clients.Remove(this);
                    }
                }
                return true;
            }
            return false;
        }

        public void Movement()
        {
            velocity += acceleration * terrainVelocity;
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
           /* for (float i = Left - SceneManager.grid.squareWidth; i <= Right + SceneManager.grid.squareWidth; i += SceneManager.grid.squareWidth)
            {
                for (float j = Top - SceneManager.grid.squareHeight; j <= Bottom + SceneManager.grid.squareHeight; j += SceneManager.grid.squareHeight)
                {
                    int x = (int)((i + (SceneManager.grid.squareWidth / 2)) / SceneManager.grid.squareWidth);
                    int y = (int)((j + (SceneManager.grid.squareHeight / 2)) / SceneManager.grid.squareHeight);
                    if (SceneManager.grid.InGrid(x, y) && SceneManager.grid.terrainGrid[x, y] is RockSquare)
                    {
                        if (CollisionHelper.CheckBoxvBoxCollision(position, Size, new Vector2(x, y) * SceneManager.grid.squareSize, SceneManager.grid.squareSize))
                        {
                            Center = CollisionHelper.StopBox(Center, Size, new Vector2(x + 0.5f, y + 0.5f) * SceneManager.grid.squareSize, SceneManager.grid.squareSize, ref velocity);
                            hitWall = true;
                        }
                    }
                }
            }*/

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

            sightRays.ForEach(n => n.FeedData(inputs, ref raySums));

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
            inputs.Add(InWater() ? -1 : 1);

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
                memory[i] += MathF.Sqrt(MathF.Abs(m)) * MathF.Sign(m);
                memory[i] *= 0.99f;
            }

            offspringOffset = new Vector2(0, SpawnDistance * output[5]).RotatedBy(output[6] * 6.28f);
        }

        public override double Distance(NeatAgent other)
        {
            Cell otherCell = other as Cell;
            double ret = BaseDistance(otherCell);
            if (sightRaysInitialized && otherCell.sightRaysInitialized)
            {
                for (int i = 0; i < RAYS; i++)
                {
                    SightRay localRay = sightRays[i];
                    SightRay otherRay = otherCell.sightRays[i];
                    if (localRay.Dna == null || localRay.Dna is not Genome || otherRay.Dna == null || otherRay.Dna is not Genome)
                        continue;
                    ret += (localRay.GetGenome().Distance(otherRay.GetGenome()) / (double)RAYS) * 0.4f;
                }
            }

            for (int j = 0; j < cellStats.Count(); j++)
            {
                CellStat a = cellStats[j];
                CellStat b = otherCell.cellStats[j];
                ret += ((double)CellStat.Distance(a, b) / (double)cellStats.Count()) * 0.5f;
            }
            return ret;
        }

        public double BaseDistance(NeatAgent other)
        {
            return GetGenome().Distance(other.GetGenome());
        }

        public override void CalculateCurrentFitness() => Fitness = GetFitness(true);

        public float GetFitness(bool reset, bool forMating = false)
        {
            //if (!IsActive() && forMating)
           // {
            //    return -999;
           // }
            float fitness = 0;
            //float fitness = ((energy / maxEnergy) * Scale) + MathF.Sqrt(kills * 10);
            float childrenDistanceThing = 0f;
            livingChildren.ForEach(n => childrenDistanceThing += (2f / (9 + n.Center.Distance(Center))));

            float parentDistanceThing = 0f;
            parents.ForEach(n => parentDistanceThing += (2f / (9 + n.Center.Distance(Center))));


            fitness += MathF.Min(foodCounter + 1, 15) * 5;
            fitness += MathF.Min(sunlightCounter + 1, 15) * 5;
           // fitness *= FitnessLifetimeCorrelation(lifeCounter);

            //fitness *= MathF.Sqrt(energy / maxEnergy);

            if (hitWall)
                fitness -= 10;

            

            if (foundSunlight)
                fitness += 160f / (80.0f + velocity.Length());

            if (foundFood)
                fitness += 2f;

            fitness += (MathF.Sqrt(generation) * 0.6f) + MathF.Sqrt(childrenDistanceThing) + MathF.Sqrt(parentDistanceThing) + MathF.Sqrt(kids * 10);

            float newOldFitness = fitness;
            //fitness += (fitness - oldFitness) * 0.1f;
            if (reset)
            {
                //oldFitness = newOldFitness;
                hitWall = false;
                for (int i = 0; i < MEMORYCELLS; i++)
                {
                    memory[i] = 0;
                }
                sunlightCounter = 0;
                foodCounter = 0;
                kills = 0;
                kids = 0;
            }

            fitness += (livingChildren.Count * 6f);

            if (double.IsNaN(fitness))
            {
                throw new Exception("Fitness is not a number!");
            }

            if (energy <= DEADENERGY)
                fitness *= 0.2f;

            if (velocity.Length() < 10)
                fitness *= 0.1f;

            fitness *= terrainVelocity * terrainVelocity;

            return fitness;

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
            hitWall = false;
        }
        #endregion

        #region specific actions
        public void TryFight()
        {
            var nearestCell = sim.PList.GetList(Center).Where(n => n != this && n.IsActive() && !mates.Contains(n) && !parents.Contains(n) && !livingChildren.Contains(n) && n.health < energy && CollisionHelper.CheckBoxvBoxCollision(n.position, n.Size, position, Size)).OrderBy(n => n.health).FirstOrDefault();
            if (nearestCell != default)
            {
                Cell nearestCellCast = nearestCell as Cell;
                float damage = Main.delta * Scale * Scale * Scale * DamageCapacity * 500f;
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
            Distance(n) < GetGenome().Neat.CP * 5 && 
            n.mateWillingness > 1 &&
            !n.livingChildren.Contains(this) &&
            !livingChildren.Contains(n) &&
            //parents.Intersect((n as Cell).parents).Count<Cell>() == 0 && 
            1 < n.GetFitness(false, true) && 
            1 < GetFitness(false, true) &&
            n.energy * n.ChildEnergy > 50 + DEADENERGY).OrderBy(n => n.Fitness).LastOrDefault();
            if (partnerCell != default)
            {
                Vector2 newPos = Center + offspringOffset;
                Debug.WriteLine("Mating at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
                Cell child = new Cell(color, Vector2.One * 32, newPos, MathHelper.Lerp(energy, partnerCell.energy, 0.5f) * MathHelper.Lerp(ChildEnergy, partnerCell.ChildEnergy, 0.5f), (int)MathF.Max(generation, partnerCell.generation) + 1);
                child.SetGenome(GetSpecies().Breed(partnerCell, this));
                GetSpecies().ForceAdd(child);

                sim.AddAgent(child);

                for (int i = 0; i < cellStats.Count; i++)
                {
                    child.cellStats[i] = cellStats[i].Combine(partnerCell.cellStats[i]);
                }

                child.health = child.maxHealth;

                child.parents.Add(this);
                child.parents.Add(partnerCell);

                partnerCell.energy *= 1.0f - (partnerCell.ChildEnergy * 0.4f);
                energy *= 1.0f - (ChildEnergy * 0.4f);
                partnerCell.kids++;
                kids++;

                partnerCell.livingChildren.Add(child);
                livingChildren.Add(child);

                child.Mutate();
                childDampMult *= ChildDampen;
                SceneManager.successfulMates++;
                if (partnerCell.Fitness > Fitness)
                {
                    child.InitializeRays(partnerCell, this);
                }
                else
                {
                    child.InitializeRays(this, partnerCell);
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
            Debug.WriteLine("Mitosis at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
            Cell child = new Cell(color, Vector2.One * 32, newPos, energy * ChildEnergy, (int)generation + 1);
            energy *= (0.8f - ChildEnergy);
            mitosisCounter = 0;
            if (GetSpecies().Size() > 0)
            {
                child.SetGenome(GetSpecies().Breed());
                GetSpecies().ForceAdd(child);
            }
            else
            {
                Debug.WriteLine("Failed mitosis from generation " + generation.ToString() + " type 2");
                child.Kill();
                return;
            }
            sim.AddAgent(child);

            for (int i = 0; i < cellStats.Count; i++)
            {
                child.cellStats[i] = cellStats[i].Duplicate();
            }

            child.health = child.maxHealth;

            child.Mutate();
            child.parents.Add(this);

            livingChildren.Add(child);
            child.InitializeRays(this, this);
            childDampMult *= ChildDampen;
            //kids++;
        }
        #endregion

        public void Draw(SpriteBatch spriteBatch)
        {
            string speciesString = "null";
            if (GetSpecies() != null)
            {
                speciesString = GetSpecies().GetHashCode().ToString();
            }

            string text = /*"Species: " + speciesString +*/"Energy: " + ((int)energy).ToString() + "\nKill power: " + fightWillingness.ToString() + "\nChildren: " + livingChildren.Count.ToString() + "\nFitness: " + GetFitness(false).ToString() + "\nKills: " + kills;
            DrawHelper.DrawText(spriteBatch, text, StaticColors.textColor, position - new Vector2(0, 120), Vector2.One);
            DrawHelper.DrawPixel(spriteBatch, color, position, Vector2.Zero, width * Scale, height * Scale);
        }

        public override void OnKill()
        {
            color = StaticColors.deadCellColor;
            if (GetSpecies() != null)
                GetSpecies().clients.Remove(this);

            sightRays.ForEach(n => n.Kill());
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
