using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project1.Interfaces;
using Microsoft.Xna.Framework.Input;
using Project1.ProjectContent.Terrain;
using Project1.Core.NeuralNetworks;
using Project1.ProjectContent.CellStuff;

namespace Project1.ProjectContent.Resources
{
    internal class FoodManager : ILoadable, IDraw, IUpdatable
    {
        public float LoadPriority => 1.5f;

        public float DrawPriority => 2.0f;

        public float UpdatePriority => 1.0f;

        public static List<Food> foods = new List<Food>();

        private bool pressingE = false;

        private float foodCounter;
        private float foodThreshhold = 0.3f;

        public void Load()
        {
            Game1.drawables.Add(this);
            Game1.updatables.Add(this);
        }

        public void Unload()
        {

        }

        public void Update(GameTime gameTime)
        {
            TestForNewFood();
            foodCounter += Game1.delta;
            while (foodCounter > foodThreshhold)
            {
                foodCounter -= foodThreshhold;
                NewFood(1);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Food food in foods)
            {
                food.Draw(spriteBatch);
            }
        }

        private void TestForNewFood()
        {
            if (!pressingE && Keyboard.GetState().IsKeyDown(Keys.E))
            {
                pressingE = true;
                NewFood(400);
            }
            if (!Keyboard.GetState().IsKeyDown(Keys.E))
                pressingE = false;
        }

        private void NewFood(int numFood)
        {
            for (int i = 0; i < numFood; i++)
            {
                Vector2 pos = Vector2.Zero;
                pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
                pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));

                while (TerrainManager.ContainsRockWorld(pos))
                {
                    pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
                    pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
                }
                Food newFood = new Food(32, 32, 200, new Color(255, 0, 0), pos);
                foods.Add(newFood);
            }
        }
    }
}
