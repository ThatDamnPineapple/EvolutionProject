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
using System.Drawing.Text;
using EvoSim.ProjectContent.Resources;

namespace EvoSim.ProjectContent.SceneStuff
{
    internal class SceneManager : ILoadable, IDraw, IUpdate
    {
        #region priorities
        public float LoadPriority => 1.5f;

        public float DrawPriority => 5.0f;

        public float UpdatePriority => 1.0f;

        #endregion
        public static float NumCells()
        {
            if (simulation == null)
                return 0;
            return simulation.Agents.Count;
        }

        public int StartingCells => 50;

        public static bool trainingMode = false;

        public static Simulation simulation;

        private List<ButtonToggle> Toggles = new List<ButtonToggle>();

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

            Toggles.Add(new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.Space)), new ButtonAction((object o) => NewCells(StartingCells)))); //Start sim
            Toggles.Add(new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.T)), new ButtonAction((object o) => trainingMode = !trainingMode))); //toggle Training Mode
            Toggles.Add(new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.R)), new ButtonAction((object o) => grid.PopulateGrid()))); //Regenerate terrain
        }

        public void Unload()
        {

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            simulation?.Agents.ForEach(cell => (cell as Cell).Draw(spriteBatch));

            float totalEnergy = 0;
            simulation?.Agents.ForEach(n => totalEnergy += (n as Cell).energy);
            FoodManager.foods.ForEach(n => totalEnergy += n.energy);

            string debugInfo = "Total Cells: " + simulation?.Agents.Count.ToString();
            debugInfo += "\nTotal energy: " + ((int)totalEnergy).ToString();

            if (simulation != null && simulation.Agents.Count > 0)
                debugInfo += "\nHighest generation: " + (simulation?.Agents.OrderBy(n => (n as Cell).generation).LastOrDefault() as Cell).generation;
            if (simulation != null && simulation != default)
            {
                debugInfo += "\nGlobal Sharing: " + (simulation as NEATSimulation).globalSharing.ToString();
            }
            debugInfo += "\nTraining mode: " + trainingMode.ToString();
            DrawHelper.DrawText(spriteBatch, debugInfo, StaticColors.textColor, new Vector2(50, 50), Vector2.One * 2, false);

            DrawMinimap(spriteBatch, Main.ScreenSize * new Vector2(0.9f, 0.8f), Vector2.One * 4);
        }

        public void Update(GameTime gameTime)
        {
            Toggles.ForEach(n => n.Update(this));

            simulation?.Update();
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
            Cell newCell = new Cell(new Color(0, 0, 1.0f), Vector2.One * 32, pos, 300, dna);
            return newCell;
        }

        private void DrawMinimap(SpriteBatch spriteBatch, Vector2 center, Vector2 scale)
        {
            Vector2 MapToMap(Vector2 worldPos, Vector2 mapSize)
            {
                Vector2 ret = center;
                ret.X += MathHelper.Lerp(-mapSize.X * 0.5f, mapSize.X * 0.5f, worldPos.X / grid.mapSize.X);
                ret.Y += MathHelper.Lerp(-mapSize.Y * 0.5f, mapSize.Y * 0.5f, worldPos.Y / grid.mapSize.Y);
                return ret;
            }
            Vector2 mapSize = (grid.gridSize + new Vector2(4,4)) * scale;
            DrawHelper.DrawPixel(spriteBatch, Color.White, center - (mapSize * 0.5f), Vector2.Zero, mapSize.X, mapSize.Y, false);
            for (int i = 0; i < grid.gridWidth; i++)
            {
                for (int j = 0; j < grid.gridHeight; j++)
                {
                    Vector2 drawPos = center + (new Vector2(i - (grid.gridWidth / 2), j - (grid.gridHeight / 2)) * scale);
                    TerrainSquare square = grid.terrainGrid[i, j];
                    DrawHelper.DrawPixel(spriteBatch, square.color, drawPos, Vector2.Zero, scale.X, scale.Y, false);
                }
            }

            mapSize = grid.gridSize * scale;
            Vector2 topLeftCamera = MapToMap(camera.position, mapSize);
            Vector2 topRightCamera = MapToMap(camera.position + new Vector2(Main.ScreenSize.X, 0), mapSize);
            Vector2 bottomLeftCamera = MapToMap(camera.position + new Vector2(0, Main.ScreenSize.Y), mapSize);
            Vector2 bottomRightCamera = MapToMap(camera.position + new Vector2(Main.ScreenSize.X, Main.ScreenSize.Y), mapSize);
            DrawHelper.DrawLine(spriteBatch, Color.Red, topLeftCamera, topRightCamera, scale.X, false);
            DrawHelper.DrawLine(spriteBatch, Color.Red, topRightCamera, bottomRightCamera, scale.Y, false);
            DrawHelper.DrawLine(spriteBatch, Color.Red, bottomRightCamera, bottomLeftCamera, scale.X, false);
            DrawHelper.DrawLine(spriteBatch, Color.Red, bottomLeftCamera, topLeftCamera, scale.Y, false);

            FoodManager.foods.ForEach(n => DrawHelper.DrawPixel(spriteBatch, n.color * 0.3f, MapToMap(n.Center, mapSize), new Vector2(0.5f, 0.5f), n.width * (scale.X / grid.squareWidth), n.height * (scale.Y / grid.squareHeight), false));
            simulation?.Agents.ForEach(n => DrawHelper.DrawPixel(spriteBatch, (n as Cell).color, MapToMap((n as Cell).Center, mapSize), new Vector2(0.5f, 0.5f), (n as Cell).Size.X * (scale.X / grid.squareWidth), (n as Cell).Size.Y * (scale.Y / grid.squareHeight), false));
            
        }
    }
}
