using DeepCopy;
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
        public float maxEnergy => 1000 * (Scale * Scale);
        public float maxHealth => 300 * (Scale * Scale);

        public float accelerationBase => 10.0f;

        public float EnergyUsage => ((Scale * Scale) * velocity.Length() * 0.000002f * (1.0f / TerrainVelocity())) + 1.7f;

        public float ConsumptionRate => (Scale * Scale) * 200f;

        public float FoodCounterRate => 2f;
        #endregion
        #region neural network node info
        public readonly static float UPDATERATE = 0.01f;
        public const int RAYS = 8;
        public static int RAYVALUES => SightRay.OUTPUTNUM;
        public readonly static int TERRAINRANGE = 2;
        public readonly static int MEMORYCELLS = 3;
        public readonly static int ADDITIONALVALUES = 10;
        public static int INPUTNUM => (RAYS * RAYVALUES) + ADDITIONALVALUES + (TERRAINRANGE * TERRAINRANGE) + MEMORYCELLS;
        public readonly static int BASICOUTPUT = 7;
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

        #endregion
        #region size and dimension stuff
        public float width;
        public float height;

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

        #endregion
        #region instance Stats
        public Color color;

        public Vector2 position;

        public Vector2 velocity;

        public float lifeCounter;

        public float energy;

        public float health;

        public float rotation;

        public bool dead = false;

        public bool foundFood = false;

        public float foodCounter = 0;

        public int kids = 0;
        public int kills = 0;

        public float stillCounter = 0;

        public bool hitWall = false;

        private float mitosisCounter;

        public Vector2 acceleration = Vector2.Zero;

        private float mateWillingness = 0f;
        private float splitWillingness = 0f;

        private float fightWillingness = 0f;

        public float generation = 0;

        public Vector2 offspringOffset = Vector2.Zero;

        public int horribleParentCounter = 0;

        public float childDampMult = 1.0f;

        public List<Cell> parents = new List<Cell>();
        #endregion
        #region miscellaneous data
        public List<CellStat> cellStats= new List<CellStat>();

        public CellNeatSimulation<Cell> sim => SceneManager.cellSimulation as CellNeatSimulation<Cell>;  

        public List<SightRay> sightRays = new List<SightRay>();
        public bool sightRaysInitialized = false;

        public List<Cell> children = new List<Cell>();

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
            NetworkTimer = new TimeCounter(Main.random.NextFloat(0.4f, 0.6f), new CounterAction((object o, ref float counter, float threshhold) =>
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

            mutationTimer = new TimeCounter(MutationRate, new CounterAction((object o, ref float counter, float threshhold) =>
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

            SceneManager.cellSimulation?.Agents.Add(this);
        }

        public override IDna GenerateRandomAgent()
        {
            IDna network = new BaseNeuralNetwork(INPUTNUM)
                   .AddLayer<LinearActivationFunction>(100)
                   .AddLayer<SigmoidActivationFunction>(100)
                   .AddLayer<LinearActivationFunction>(80)
                   .SetOutput<SigmoidActivationFunction>(OUTPUTNUM)
                   .GenerateWeights(() => Main.random.NextFloat(-4, 4));

            return network;
        }

        public Cell() : base()
        {
            InitializeCellStats();
            Vector2 pos = Vector2.Zero;
            pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
            pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));
            position = pos;
            color = new Color(0, 0, 1.0f);
            health = maxHealth;
            Size = new Vector2(width, height) * Scale;
            InitializeRays();
            BaseInitializer(Dna);
        }

        public Cell(Color _color, Vector2 size, Vector2 _position, float _energy, IDna NewDNA = null) : base()
        {
            InitializeCellStats();
            color = _color;
            width = size.X;
            height = size.Y;
            position = _position;
            energy = _energy;
            health = maxHealth;
            Size = size * Scale;
            InitializeRays();

            BaseInitializer(NewDNA ?? Dna);
        }

        private void InitializeCellStats()
        {
            cellStats.Add(new CellStat(10f, 0.05f, 0.02f, 4f, 20f, false)); //sexLikelihood
            cellStats.Add(new CellStat(8f, 0.01f, 0.005f, 5.5f, 15f, false)); //aceLikelihood
            cellStats.Add(new CellStat(200f, 1f, 0.1f, 2, 700, false)); //speed
            cellStats.Add(new CellStat(0.25f, 0.02f, 0.001f, 0.05f, 0.49f, false)); //childEnergy
            cellStats.Add(new CellStat(1.0f, 0.095f, 0.001f, 0.07f, 6f, false)); //scale
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.001f, 0.1f, 1, false)); //red
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.001f, 0.1f, 1, false)); //green
            cellStats.Add(new CellStat(0.5f, 0.035f, 0.001f, 0.1f, 1, false)); //blue
            cellStats.Add(new CellStat(0.4f, 0.01f, 0.0001f, 0.1f, 1, false)); //fightThreshhold
            cellStats.Add(new CellStat(0.5f, 0.005f, 0.0001f, 0.05f, 5, true)); //regenRate
            cellStats.Add(new CellStat(0.95f, 0.001f, 0.0001f, 0.9f, 1.0f, false)); //deathThreshhold
            cellStats.Add(new CellStat(35, 0.15f, 0.0001f, 15, 65, false)); //spawnDistance
            cellStats.Add(new CellStat(40 * Main.random.NextFloat(0.8f, 1.2f), 1f, 0.01f, 20, 200, false)); //mutationRate
            cellStats.Add(new CellStat(0.5f, 0.0f, 0.000f, 0.01f, 0.99f, false)); //SwimmingProficiency
            cellStats.Add(new CellStat(0.87f, 0.01f, 0.001f, 0.7f, 0.99f, false)); //ChildDampen
        }

        public void InitializeRays(IDna[] oldDNA, IDna[] oldDNA2, Cell fitterParent, Cell lessFitParent)
        {
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
            if (CorpseLogic())
                return;
            mitosisCounter += Main.delta;
            lifeCounter += Main.delta;

            color = new Color(Red, Green, Blue);

            TileRefreshCounter.Update(this);

            RegenerationLogic();

            energy -= Main.delta * EnergyUsage;

            if (energy <= 50)
            {
                color = StaticColors.deadCellColor;
                Kill();
                return;
            }

            Movement();
            NetworkTimer.Update(this);
            mutationTimer.Update(this);

            foreach (Cell child in children.ToArray())
            {
                if (!child.IsActive())
                {
                    children.Remove(child);
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
            if (foundFood)
            {
                foodCounter += FoodCounterRate * Main.delta;
            }
            else if (foodCounter > 0)
            {
                foodCounter -= FoodCounterRate * Main.delta;
            }
            foodCounter = MathHelper.Max(foodCounter, 0);
        }

        public void TrySpecificActions()
        {
            if (mateWillingness > 1 && energy * ChildEnergy > 75 && lifeCounter > 3 && TryMate(mateWillingness))
            {
                return;
            }

            if (fightWillingness > FightThreshhold)
                TryFight(fightWillingness - FightThreshhold);

            if (splitWillingness > 1 && energy * ChildEnergy > 150 && lifeCounter > 3)
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
                energy -= Main.delta * 1f;
                if (energy < 0)
                {
                    if (sim != null && sim.Agents.Contains(this))
                        sim.Agents.Remove(this);

                    if (SceneManager.cellSimulation != null)
                    {
                        if (SceneManager.cellSimulation.Agents.Contains(this))
                            SceneManager.cellSimulation.Agents.Remove(this);
                    }
                }
                return true;
            }
            return false;
        }

        public void Movement()
        {
            velocity += acceleration * TerrainVelocity();

            if (velocity.Length() > Speed * TerrainVelocity())
            {
                velocity.Normalize();
                velocity *= Speed * TerrainVelocity();
            }
            TileCollision();

            position += velocity * Main.delta;
            rotation = velocity.ToRotation() + MathHelper.PiOver2;
        }

        public float TerrainVelocity() => (InWater() ? SwimmingProficiency : 1.0f - SwimmingProficiency);

        public bool InWater()
        {
            int x = (int)(Center.X / SceneManager.grid.squareWidth);
            int y = (int)(Center.Y / SceneManager.grid.squareHeight);
            if (!SceneManager.grid.InGrid(x,y)) 
                return false;

            TerrainSquare square = SceneManager.grid.terrainGrid[x, y];
            return square is WaterSquare;
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
                position.X += SceneManager.grid.mapSize.X;
            }

            if (TopRight.X > SceneManager.grid.mapSize.X)
            {
                position.X -= SceneManager.grid.mapSize.X;
            }

            if (TopLeft.Y < 0)
            {
                position.Y += SceneManager.grid.mapSize.Y;
            }

            if (BottomRight.Y > SceneManager.grid.mapSize.Y)
            {
                position.Y -= SceneManager.grid.mapSize.Y;
            }
        }

        public void FoodInteraction()
        {
            foundFood = false;
            var corpseFound = SceneManager.cellSimulation?.Agents.Where(n => n != this && (n as Cell).energy > 0 && !(n as Cell).IsActive() && CollisionHelper.CheckBoxvBoxCollision(position, Size, (n as Cell).position, (n as Cell).Size)).FirstOrDefault();
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

            var foodFound = FoodManager.foods.Where(n => CollisionHelper.CheckBoxvBoxCollision(position, Size, n.position, n.size)).FirstOrDefault();

            if (foodFound != default)
            {
                float hunger = maxEnergy - energy;

                float toEat = MathF.Min(hunger, foodFound.energy);
                toEat = MathF.Min(toEat, Main.delta * ConsumptionRate);
                toEat /= (1.0f + MathF.Sqrt(velocity.Length() * 0.03f));
                foodFound.energy -= toEat;
                energy += toEat;

                if (foodFound.energy <= 0)
                    FoodManager.foods.Remove(foodFound);

                foundFood = true;
            }
        }
        #endregion

        #region neural network interactions
        public List<float> FeedInputs()
        {
            List<float> inputs = new List<float>();

            sightRays.ForEach(n => n.FeedData(inputs));

            for (int i = 0; i < TERRAINRANGE; i++)
            {
                for (int j = 0; j < TERRAINRANGE; j++)
                {
                    inputs.Add(localTiles[i, j]);
                }
            }

            inputs.Add(rotation);
            inputs.Add(energy);
            inputs.Add(lifeCounter);
            inputs.Add(health);
            inputs.Add(velocity.X);
            inputs.Add(velocity.Y);
            inputs.Add(children.Count * 20);
            inputs.Add(foundFood ? -200 : 200);
            inputs.Add(Center.X);
            inputs.Add(Center.Y);

            memory.ForEach(n => inputs.Add(n));

            return inputs;
        }

        public void Response(float[] output)
        {
            acceleration.X = accelerationBase * (output[0] - 0.5f);
            acceleration.Y = accelerationBase * (output[1] - 0.5f);
            //acceleration = acceleration.RotatedBy(rotation);

            mateWillingness = output[2] * SexLikelihood * childDampMult * MathF.Sqrt(MathF.Sqrt(energy / maxEnergy));
            splitWillingness = output[3] * AceLikelihood * childDampMult * MathF.Sqrt(MathF.Sqrt(energy / maxEnergy));
            fightWillingness = output[4];

            for (int i = 0; i < MEMORYCELLS; i++)
            {
                memory[i] = (output[i + BASICOUTPUT] - 0.5f) * 10;
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
                    ret += localRay.GetGenome().Distance(otherRay.GetGenome()) / RAYS;
                }
            }
            return ret;
        }

        public double BaseDistance(NeatAgent other)
        {
            return GetGenome().Distance(other.GetGenome());
        }

        public override void CalculateContinuousFitness() => Fitness = GetFitness(false);

        public override void CalculateCurrentFitness() => Fitness = GetFitness(true);

        public float GetFitness(bool reset, bool forMating = false)
        {
            if (!IsActive() && forMating)
            {
                return -999;
            }
            float fitness = (energy / maxEnergy) + MathF.Sqrt(kills * 7) + MathF.Sqrt(generation);
            float childrenDistanceThing = 0f;
            children.ForEach(n => childrenDistanceThing += (2f / (9 + n.Center.Distance(Center))));

            float parentDistanceThing = 0f;
            parents.ForEach(n => parentDistanceThing += (2f / (9 + n.Center.Distance(Center))));
            fitness += MathF.Sqrt(generation) + (kids * 0.5f) + childrenDistanceThing + (children.Count * 1.2f) + parentDistanceThing;

            fitness += MathF.Sqrt(foodCounter + 1);
            fitness *= FitnessLifetimeCorrelation(lifeCounter);

            //fitness *= MathF.Sqrt(energy / maxEnergy);

            if (hitWall)
                fitness -= 10;

            if (energy <= 0)
                fitness -= 7;

            if (reset)
            {
                hitWall = false;
            }

            if (foundFood)
                fitness += 10;

            //fitness -= 100 * horribleParentCounter;

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
        public void TryFight(float effort)
        {
            var nearestCell = SceneManager.cellSimulation?.Agents.Where(n => n != this && CollisionHelper.CheckBoxvBoxCollision((n as Cell).position, (n as Cell).Size, position, Size) && (n as Cell).health < energy).OrderBy(n => (n as Cell).health).FirstOrDefault();
            if (nearestCell != default)
            {
                float hunger = maxEnergy - energy;
                Cell nearestCellCast = nearestCell as Cell;
                float damage = Main.delta * effort * 10 * Scale * Scale * (1.0f + velocity.Length());
                energy -= damage;
                if (energy < 0)
                    return;

                energy += MathF.Min(MathF.Min(damage, hunger), nearestCellCast.health);
                nearestCellCast.health -= damage;
                foundFood = true;
                if (nearestCellCast.health < 0)
                {
                    if (children.Contains(nearestCellCast))
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
            if (true)
            {
                var partner = SceneManager.cellSimulation?.Agents.Where(n => n != this && 
                (n as Cell).IsActive() &&
                (Center.Distance((n as Cell).Center)) < SpawnDistance && 
                Distance(n as Cell) < GetGenome().Neat.CP * 1.5f && 
                (n as Cell).mateWillingness > 1 &&
                !(n as Cell).children.Contains(this) &&
                !children.Contains(n as Cell) &&
                parents.Intersect((n as Cell).parents).Count<Cell>() == 0 && 
                ((SexLikelihood - willingness) / SexLikelihood) < (n as Cell).GetFitness(false, true) * 0.5f && 
                ((n as Cell).SexLikelihood - (n as Cell).mateWillingness) / (n as Cell).SexLikelihood < GetFitness(false, true) * 0.5f).FirstOrDefault();
                var partnerCell = partner as Cell;
                if (partner != default && partnerCell.energy > 100)
                {
                    Vector2 newPos = position + offspringOffset;
                    Debug.WriteLine("Mating at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
                    Cell child = new Cell(color, Size, newPos, MathHelper.Lerp(energy, partnerCell.energy, 0.5f) * MathHelper.Lerp(ChildEnergy, partnerCell.ChildEnergy, 0.5f));
                    child.SetGenome(GetSpecies().Breed(partnerCell, this));
                    GetSpecies().ForceAdd(child);

                    sim.neatHost.GetClients().Add(child);

                    sim.Agents.Add(child);

                    for (int i = 0; i < cellStats.Count; i++)
                    {
                        child.cellStats[i] = cellStats[i].Combine(partnerCell.cellStats[i]);
                    }

                    child.health = child.maxHealth;

                    child.generation = MathF.Max(generation, partnerCell.generation) + 1;

                    child.parents.Add(this);
                    child.parents.Add(partnerCell);

                    partnerCell.energy *= 1.0f - (partnerCell.ChildEnergy * 0.4f);
                    energy *= 1.0f - (ChildEnergy * 0.4f);
                    partnerCell.kids++;
                    kids++;

                    partnerCell.children.Add(child);
                    children.Add(child);

                    child.Mutate();
                    childDampMult *= ChildDampen;

                    if (partnerCell.Fitness > Fitness)
                    {
                        child.InitializeRays(partnerCell.SightRayDNA, SightRayDNA, partnerCell, this);
                    }
                    else
                    {
                        child.InitializeRays(SightRayDNA, partnerCell.SightRayDNA, this, partnerCell);
                    }
                    return true;
                }
            }
            return false;
        }

        public void Mitosis()
        {
            if (SceneManager.NumCells() > 600)
                return;
            if (GetSpecies() == null)
            {
                Debug.WriteLine("Failed mitosis from generation " + generation.ToString());
                return;
            }
            Vector2 newPos = position + offspringOffset;
            Debug.WriteLine("Mitosis at " + ((int)newPos.X).ToString() + "," + ((int)newPos.Y).ToString());
            Cell child = new Cell(color, Size, newPos, energy * ChildEnergy);
            energy *= (0.9f - ChildEnergy);
            mitosisCounter = 0;
            if (GetSpecies().Size() > 0)
            {
                child.SetGenome(GetSpecies().Breed());
                GetSpecies().ForceAdd(child);
            }
            else
            {
                Debug.WriteLine("Failed mitosis from generation " + generation.ToString());
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
            child.parents.Add(this);

            children.Add(child);
            child.InitializeRays(SightRayDNA, SightRayDNA, this, this);
            childDampMult *= ChildDampen;
            kids++;
        }
        #endregion

        public void Draw(SpriteBatch spriteBatch)
        {
            string speciesString = "null";
            if (GetSpecies() != null)
            {
                speciesString = GetSpecies().GetHashCode().ToString();
            }

            string text = /*"Species: " + speciesString +*/"Energy: " + ((int)energy).ToString() + "\nGeneration: " + generation.ToString() + "\nChildren: " + children.Count.ToString() + "\nFitness: " + GetFitness(false).ToString() + "\nDampen: " + childDampMult.ToString();
            DrawHelper.DrawText(spriteBatch, text, StaticColors.textColor, position - new Vector2(0, 90), Vector2.One);
            DrawHelper.DrawPixel(spriteBatch, color, position, Vector2.Zero, width * Scale, height * Scale);
        }

        public override void OnKill()
        {
            color = StaticColors.deadCellColor;
            if (GetSpecies() != null)
                GetSpecies().clients.Remove(this);

            sightRays.ForEach(n => n.Cull());
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
                        localTiles[i + tileRadius, j + tileRadius] = TerrainSquare.GetTerrainID(square) * 100;
                    }
                    else
                        localTiles[i + tileRadius, j + tileRadius] = 0;
                }
            }
        }      
    }
}
