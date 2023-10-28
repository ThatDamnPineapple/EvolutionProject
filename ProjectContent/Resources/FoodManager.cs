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

        public static List<Food> foods = new List<Food>();

        public static float FoodEnergy => 1200;

        public static Vector2 FoodSize => new Vector2(300, 300);

        public static int FoodAmount => 30;

        public static float FoodSpawnRate => 6f;


        private TimeCounter AutomaticFoodSpawner;
        private ButtonToggle ManualFoodSpawner;

        public void Load()
        {
            Main.drawables.Add(this);
            Main.updatables.Add(this);

            AutomaticFoodSpawner = new TimeCounter(FoodSpawnRate, new CounterAction((object o, ref float counter, float threshhold) =>
            {
                counter -= FoodSpawnRate;
                NewFood(1);
            }));

            ManualFoodSpawner = new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.E)), new ButtonAction((object o) => NewFood(FoodAmount)));
        }

        public void Unload()
        {

        }

        public void Update(GameTime gameTime)
        {
            ManualFoodSpawner.Update(this);
            if (SceneManager.cellSimulation != null)
                AutomaticFoodSpawner.Update(this);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Food food in foods)
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
                Food newFood = new Food(FoodSize * scale, FoodEnergy * scale, StaticColors.foodColor, pos);
                foods.Add(newFood);
            }
        }
    }
}
