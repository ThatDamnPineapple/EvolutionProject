using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.Interfaces;
using Microsoft.Xna.Framework.Input;
using EvoSim.ProjectContent.Terrain;
using EvoSim.Core.NeuralNetworks;
using EvoSim.ProjectContent.CellStuff;
using EvoSim.Helpers.HelperClasses;
using EvoSim.Helpers;

namespace EvoSim.ProjectContent.Resources
{
    internal class FoodManager : ILoadable, IDraw, IUpdate
    {
        #region priorities
        public float LoadPriority => 1.5f;

        public float DrawPriority => 0.7f;

        public float UpdatePriority => 1.0f;

        #endregion

        public delegate void PassiveFoodAction(ref float item, Point index);

        public static PartitionedFoodList<Food> foods;

        public static float FoodEnergy => 1200;

        public static Vector2 FoodSize => new Vector2(300, 300);

        public static int FOODROWS = 80;
        public static int FOODCOLUMNS = 80;

        public static float[,] passiveFood = new float[FOODROWS, FOODCOLUMNS];
        public static float passiveFoodRegen = 4500f;
        public static float passiveFoodCap = 3000f;
        public static float passiveFoodStart = 1000f;

        public static int FoodAmount => 150;

        public static float FoodSpawnRate => 3000f;



        private TimeCounter AutomaticFoodSpawner;
        private ButtonToggle ManualFoodSpawner;

        public void Load()
        {
            Main.drawables.Add(this);
            Main.updatables.Add(this);
            foods = new PartitionedFoodList<Food>(SceneManager.PARTITIONROWS, SceneManager.PARTITIONCOLUMNS);
            AutomaticFoodSpawner = new TimeCounter(FoodSpawnRate, new CounterAction((object o, ref float counter, float threshhold) =>
            {
                counter -= FoodSpawnRate;
                NewFood(1);
            }));

            ChangeEveryPassiveFood(new PassiveFoodAction((ref float i, Point point) =>
            {
                i = passiveFoodStart;
            }));

            ManualFoodSpawner = new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.E)), new ButtonAction((object o) => NewFood(FoodAmount)));
        }

        public static void ChangeEveryPassiveFood(PassiveFoodAction action)
        {
            for (int i = 0; i < FOODROWS; i++)
            {
                for (int j = 0; j < FOODCOLUMNS; j++)
                {
                    action.Invoke(ref passiveFood[i, j], new Point(i, j));
                }
            }
        }

        public static void ChangeSpecificPassiveFood(Vector2 pos, PassiveFoodAction action)
        {
            Point point = new Point((int)(pos.X / SceneManager.grid.mapSize.X) * FOODROWS, (int)(pos.Y / SceneManager.grid.mapSize.Y) * FOODCOLUMNS);
            point.X %= FOODROWS;
            point.Y %= FOODCOLUMNS;

            while (point.X < 0)
                point.X += FOODROWS;

            while (point.Y < 0)
                point.Y += FOODCOLUMNS;

            action.Invoke(ref passiveFood[point.X, point.Y], point);
        }

        public void Unload()
        {

        }

        public void Update(GameTime gameTime)
        {
            ChangeEveryPassiveFood(new PassiveFoodAction((ref float i, Point point) =>
            {
                i += passiveFoodRegen * Main.delta;
                i = MathF.Min(i, passiveFoodCap);
            }));

            ManualFoodSpawner.Update(this);
            if (SceneManager.cellSimulation != null)
                AutomaticFoodSpawner.Update(this);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Food food in foods.basicList)
            {
                food.Draw(spriteBatch);
            }
        }

        private void NewFood(int numFood)
        {  
            for (int i = 0; i < numFood; i++)
            {
                float scale = Main.random.NextFloat(0.5f, 1.0f);
                Vector2 pos = Vector2.Zero;
                pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
                pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));

                while (SceneManager.grid.ContainsRockWorld(pos))
                {
                    pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
                    pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));
                }
                Food newFood = new Food(FoodSize * scale, FoodEnergy * scale, ColorHelper.foodColor, pos);
                foods.Add(newFood);
            }
        }
    }
}
