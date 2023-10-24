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
using EvoSim.Core.NeuralNetworks.NEAT;
using EvoSim.Helpers;
using EvoSim.Helpers.HelperClasses;

namespace EvoSim.ProjectContent.SceneStuff
{
    internal class SceneManager : ILoadable, IDraw, IUpdate
    {
        #region priorities
        public float LoadPriority => 1.5f;

        public float DrawPriority => 1.0f;

        public float UpdatePriority => 1.0f;

        #endregion

        public int StartingCells => 30;

        public static bool trainingMode = true;

        public static List<Cell> cells = new List<Cell>();

        public static Simulation simulation;

        private ButtonToggle TrainingModeToggle;
        private ButtonToggle SimStarter;

        public static CameraObject camera;
        public static TerrainGrid grid;

        public void Load()
        {
            Main.drawables.Add(this);
            Main.updatables.Add(this);

            camera = new CameraObject();
            Main.updatables.Add(camera);

            grid = new TerrainGrid();
            Main.updatables.Add(grid);
            Main.drawables.Add(grid);

            SimStarter = new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.Space)), new ButtonAction((object o) => NewCells(StartingCells)));
            TrainingModeToggle = new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.T)), new ButtonAction((object o) => trainingMode = !trainingMode));
        }

        public void Unload()
        {

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            string debugInfo = "Total Cells: " + cells.Count.ToString();
            if (simulation != null && simulation != default)
            {
                debugInfo += "\n Global Sharing: " + (simulation as NEATSimulation).globalSharing.ToString();
            }
            debugInfo += "\n Training mode: " + trainingMode.ToString();
            DrawHelper.DrawText(spriteBatch, debugInfo, StaticColors.textColor, new Vector2(50, 50), Vector2.One, false);
            simulation?.Agents.ForEach(cell => (cell as Cell).Draw(spriteBatch));
        }

        public void Update(GameTime gameTime)
        {
            SimStarter.Update(this);
            TrainingModeToggle.Update(this);

            simulation?.Update();

            var cellsToDestroy = cells.Where(n => !n.IsActive()).ToList();
            cellsToDestroy.ForEach(n => cells.Remove(n));
        }

        private void NewCells(int numCells)
        {
            var newsim = new CellNeatSimulation<Cell>(Cell.INPUTNUM, Cell.OUTPUTNUM, numCells, (IDna) => CreateRawCell(IDna), 1f);
            newsim.Deploy();
            simulation = newsim;
        }

        private Cell CreateRawCell(IDna dna)
        {
            Vector2 pos = Vector2.Zero;
            pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
            pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));

            while (SceneManager.grid.ContainsRockWorld(pos))
            {
                pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
                pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));
            }
            Cell newCell = new Cell(new Color(0, 0, 1.0f), Vector2.One * 32, pos, 500, dna);
            return newCell;
        }
    }
}
