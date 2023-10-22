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

        public float DrawPriority => 1.0f;

        public float UpdatePriority => 1.0f;

        public static List<Food> foods = new List<Food>();

        public static SimpleNeuralNetwork minimumNetwork;

        private bool pressingE = false;

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
                NewCells(20);
            }
            if (!Keyboard.GetState().IsKeyDown(Keys.E))
                pressingE = false;
        }

        private void NewCells(int numFood)
        {
            for (int i = 0; i < numFood; i++)
            {
                Vector2 pos = Vector2.Zero;
                pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
                pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
                Food newFood = new Food(32, 32, 4, Color.Orange, Vector2.One * 32, pos);
                foods.Add(newFood);
            }
        }
    }
}
