using EvoSim.Core.Noise;
using EvoSim.Helpers.HelperClasses;
using EvoSim.ProjectContent.Terrain.TerrainTypes;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.Terrain
{
    public class TerrainGrid : IDraw, IUpdate
    {
        #region priorities
        public float DrawPriority => 0.1f;

        public float UpdatePriority => 0.1f;

        #endregion

        public float squareWidth = 48;

        public float squareHeight = 48;

        public int gridWidth = 80;

        public int gridHeight = 80;

        public Vector2 mapSize => new Vector2(squareWidth * gridWidth, squareHeight * gridHeight);

        public Vector2 squareSize => new Vector2(squareWidth, squareHeight);

        public TerrainSquare[,] terrainGrid;

        public TerrainGrid()
        {
            terrainGrid = new TerrainSquare[gridWidth, gridHeight];   
            PopulateGrid();
        }
        public TerrainGrid(float squareWidth, float squareHeight, int gridWidth, int gridHeight)
        {
            this.squareWidth = squareWidth;
            this.squareHeight = squareHeight;
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            terrainGrid = new TerrainSquare[gridWidth, gridHeight];
            PopulateGrid();
        }

        public bool InGrid(int x, int y)
        {
            return (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight);
        }

        public bool ContainsRockWorld(Vector2 pos)
        {
            int x = (int)(pos.X / squareWidth);
            int y = (int)(pos.Y / squareHeight);
            if (InGrid(x, y))
            {
                TerrainSquare square = SceneManager.grid.terrainGrid[x, y];
                if (square is RockSquare)
                    return true;
            }
            return false;
        }

        public void PopulateGrid()
        {
            float rockThreshhold = 0.6f;

            float waterThreshhold = 0.01f;
            FastNoiseLite noise = new FastNoiseLite(Main.random.Next());
            noise.SetFrequency(0.05f);

            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    float threshhold = noise.GetNoise(i, j);

                    if (threshhold > rockThreshhold)
                        terrainGrid[i, j] = new RockSquare(new Vector2(i * squareWidth, j * squareHeight));
                    else if (threshhold > waterThreshhold)
                        terrainGrid[i, j] = new GrassSquare(new Vector2(i * squareWidth, j * squareHeight));
                    else
                        terrainGrid[i, j] = new WaterSquare(new Vector2(i * squareWidth, j * squareHeight));
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    terrainGrid[i, j].Draw(spriteBatch);
                }
            }
        }

        public void Update(GameTime gameTime)
        {

        }

    }
}
