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
using Project1.Core.NeuralNetworks.NEAT;

namespace Project1.ProjectContent.CellStuff
{
    internal class CellManager : ILoadable, IDraw, IUpdatable
    {
        public float LoadPriority => 1.5f;

        public float DrawPriority => 1.0f;

        public float UpdatePriority => 1.0f;

        public static List<Cell> cells = new List<Cell>();

        public static List<Simulation> simulations = new List<Simulation>();

        public static bool foundMinimum = false;

        private bool pressingSpace = false;

        public void Load()
        {
            Game1.drawables.Add(this);
            Game1.updatables.Add(this);
        }

        public void Unload()
        {
            
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Cell cell in cells)
            {
                cell.Draw(spriteBatch);
            }
        }

        public void Update(GameTime gameTime)
        {
            TestForNewCells();

            simulations.ForEach(n => n.Update());

            var cellsToDestroy = cells.Where(n => !n.IsActive()).ToList();
            cellsToDestroy.ForEach(n => cells.Remove(n));
        }

        private void TestForNewCells()
        {
            if (!pressingSpace && Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                pressingSpace = true;
                NewCells(50);
            }
            if (!Keyboard.GetState().IsKeyDown(Keys.Space))
                pressingSpace = false;
        }

        private void NewCells(int numCells)
        {
            var newsim = new CellNeatSimulation<Cell>(Cell.INPUTNUM, Cell.OUTPUTNUM, numCells, (IDna) => CreateRawCell(IDna), 0.8f);
            newsim.Deploy();
            newsim.Agents.ForEach(n => (n as Cell).sim = newsim);
            simulations.Add(newsim);
            
            /*for (int i = 0; i < numCells; i++)
            {
                Vector2 pos = Vector2.Zero;
                pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
                pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
                Cell newCell = new Cell(Color.White, Vector2.One * 32, pos, 500, 1000);
                cells.Add(newCell);
            }*/
        }

        private Cell CreateRawCell(IDna dna)
        {
            Vector2 pos = Vector2.Zero;
            pos.X = Game1.random.Next((int)(TerrainManager.squareWidth * TerrainManager.gridWidth));
            pos.Y = Game1.random.Next((int)(TerrainManager.squareHeight * TerrainManager.gridHeight));
            Cell newCell = new Cell(new Color(0, 0, 1.0f), Vector2.One * 32, pos, 500, 1000, dna);
            return newCell;
        }
    }
}
