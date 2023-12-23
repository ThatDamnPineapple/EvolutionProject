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
using EvoSim.ProjectContent.CellStuff.SightRayStuff;

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
            if (cellSimulation == null)
                return 0;
            return cellSimulation.Agents.Count;
        }

        public const int PARTITIONROWS = 9;
        public const int PARTITIONCOLUMNS = 9;

        public int StartingCells => 90;

        public static bool trainingMode = false;

        public static CellNeatSimulation<Cell> cellSimulation;

        public static Simulation sightRaySimulation;

        private List<ButtonToggle> Toggles = new List<ButtonToggle>();

        public static CameraObject camera;
        public static TerrainGrid grid;

        public static int successfulMates = 0;

        public static bool firstMutation = false;

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
            cellSimulation?.Agents.ForEach(cell => (cell as Cell).Draw(spriteBatch));

            float totalEnergy = 0;
            cellSimulation?.Agents.ForEach(n => totalEnergy += (n as Cell).energy);

            string debugInfo = "Total Cells: " + cellSimulation?.Agents.Count.ToString();
            debugInfo += "\nTotal living cells: " + cellSimulation?.Agents.Where(n => (n as Cell).IsActive()).Count().ToString();
            debugInfo += "\nTotal cell energy: " + ((int)totalEnergy).ToString();
            totalEnergy = 0;
            FoodManager.foods.basicList.ForEach(n => totalEnergy += n.energy);
            debugInfo += "\nTotal food energy: " + ((int)totalEnergy).ToString();
            debugInfo += "\nSuccessful mates:" + successfulMates.ToString();

            if (cellSimulation != null)
            {
                debugInfo += "\nTotal cell species: " + (cellSimulation as NEATSimulation).neatHost.species.Count().ToString();
            }

            if (sightRaySimulation != null)
            {
                debugInfo += "\nTotal ray species: " + (sightRaySimulation as NEATSimulation).neatHost.species.Count().ToString();
            }

            if (cellSimulation != null && cellSimulation.Agents.Count > 0)
                debugInfo += "\nHighest generation: " + (cellSimulation?.Agents.OrderBy(n => (n as Cell).generation).LastOrDefault() as Cell).generation;
            if (cellSimulation != null && cellSimulation != default)
            {
                debugInfo += "\nGlobal Sharing: " + (cellSimulation as NEATSimulation).globalSharing.ToString();
            }
            debugInfo += "\nTraining mode: " + trainingMode.ToString();
            DrawHelper.DrawText(spriteBatch, debugInfo, ColorHelper.textColor, new Vector2(50, 50), Vector2.One * 2, false);

            DrawMinimap(spriteBatch, Main.ScreenSize * new Vector2(0.9f, 0.8f), Vector2.One * 2);
        }

        public void Update(GameTime gameTime)
        {
            Toggles.ForEach(n => n.Update(this));
            cellSimulation?.Update();
            sightRaySimulation?.Update();
        }

        private void NewCells(int numCells)
        {
            firstMutation = true;
            sightRaySimulation = new SightRayNeatSimulation<SightRay>(SightRay.INPUTNUM, SightRay.OUTPUTNUM, numCells, (IDna) => CreateRawSightRay(IDna), 1000000f);
            (sightRaySimulation as NEATSimulation).neatHost.Reset(SightRay.INPUTNUM, SightRay.OUTPUTNUM, 0);
            var newsim = new CellNeatSimulation<Cell>(Cell.INPUTNUM, Cell.OUTPUTNUM, numCells, (IDna) => CreateRawCell(IDna), 16f);
            newsim.Deploy();
            cellSimulation = newsim;

            (cellSimulation as NEATSimulation).Time = 30;
        }

        private Cell CreateRawCell(IDna dna)
        {
            Vector2 pos = Vector2.Zero;
            pos.X = Main.random.Next((int)(SceneManager.grid.mapSize.X));
            pos.Y = Main.random.Next((int)(SceneManager.grid.mapSize.Y));

            int tries = 0;
            while (grid.TileID(pos) != 3 && tries < 30)
            {
                tries++;
                pos.X = Main.random.Next((int)(SceneManager.grid.squareWidth * SceneManager.grid.gridWidth));
                pos.Y = Main.random.Next((int)(SceneManager.grid.squareHeight * SceneManager.grid.gridHeight));
            }
            Cell newCell = new Cell(new Color(0, 0, 1.0f), Vector2.One * 32, pos, 0.75f, 0, dna);
            return newCell;
        }

        private SightRay CreateRawSightRay(IDna dna)
        {
            return new SightRay();
        }

        private void DrawMinimap(SpriteBatch spriteBatch, Vector2 center, Vector2 scale)
        {
            Vector2 MapToMap(Vector2 worldPos, Vector2 mapSize)
            {
                Vector2 ret = center;
                ret.X += MathHelper.Lerp(-mapSize.X, mapSize.X, worldPos.X / grid.mapSize.X);
                ret.Y += MathHelper.Lerp(-mapSize.Y, mapSize.Y, worldPos.Y / grid.mapSize.Y);
                return ret;
            }
            Vector2 mapSize = (grid.gridSize + new Vector2(4,4)) * scale;
            Vector2 halfMapSize = mapSize * 0.5f;

            Vector2 halfGridSize = new Vector2(grid.gridWidth / 2, grid.gridHeight / 2);
            DrawHelper.DrawPixel(spriteBatch, Color.White, center - halfMapSize, Vector2.Zero, mapSize.X, mapSize.Y, false);
            for (int i = 0; i < grid.gridWidth; i++)
            {
                for (int j = 0; j < grid.gridHeight; j++)
                {
                    Vector2 drawPos = center + ((new Vector2(i,j) - halfGridSize) * scale);
                    TerrainSquare square = grid.terrainGrid[i, j];
                    DrawHelper.DrawPixel(spriteBatch, square.color, drawPos, Vector2.Zero, scale.X, scale.Y, false);
                }
            }

            mapSize = grid.gridSize * scale;
            Vector2 topLeftCamera = MapToMap(camera.position, halfMapSize);
            Vector2 topRightCamera = MapToMap(camera.position + new Vector2(Main.ScreenSize.X, 0), halfMapSize);
            Vector2 bottomLeftCamera = MapToMap(camera.position + new Vector2(0, Main.ScreenSize.Y), halfMapSize);
            Vector2 bottomRightCamera = MapToMap(camera.position + new Vector2(Main.ScreenSize.X, Main.ScreenSize.Y), halfMapSize);
            DrawHelper.DrawLine(spriteBatch, Color.Red, topLeftCamera, topRightCamera, scale.X, false);
            DrawHelper.DrawLine(spriteBatch, Color.Red, topRightCamera, bottomRightCamera, scale.Y, false);
            DrawHelper.DrawLine(spriteBatch, Color.Red, bottomRightCamera, bottomLeftCamera, scale.X, false);
            DrawHelper.DrawLine(spriteBatch, Color.Red, bottomLeftCamera, topLeftCamera, scale.Y, false);

            float xShrink = (scale.X / grid.squareWidth);
            float yShrink = (scale.Y / grid.squareHeight);

            Vector2 genericCenter = new Vector2(0.5f, 0.5f);
            FoodManager.foods.basicList.ForEach(n => DrawHelper.DrawPixel(spriteBatch, n.color * 0.3f, MapToMap(n.Center, halfMapSize), genericCenter, n.width * xShrink, n.height * yShrink, false));
            cellSimulation?.Agents.ForEach(n => DrawHelper.DrawPixel(spriteBatch, (n as Cell).color, MapToMap((n as Cell).Center, halfMapSize), genericCenter, (n as Cell).Size.X * xShrink * 4, (n as Cell).Size.Y * yShrink * 4, false));
            
        }
    }
}
