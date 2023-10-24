using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EvoSim.Interfaces;
using EvoSim.ProjectContent.Terrain.TerrainTypes;
using EvoSim.Core.Noise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.Terrain
{
    internal class TerrainManager : ILoadable, IDraw
    {
        public readonly static float squareWidth = 48;

        public readonly static float squareHeight = 48;

        public readonly static int gridWidth = 80;

        public readonly static int gridHeight = 80;

        public static Vector2 mapSize => new Vector2(squareWidth * gridWidth, squareHeight * gridHeight);

        public static Vector2 squareSize => new Vector2(squareWidth, squareHeight);
        public float LoadPriority => 1.1f;

        public float DrawPriority => 0.1f;

        internal static TerrainSquare[,] terrainGrid;

        public void Load() 
        {
            terrainGrid = new TerrainSquare[gridWidth,gridHeight];
            PopulateGrid();
            Main.drawables.Add(this);
        } 

        public static bool InGrid(int x, int y)
        {
            return (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight);
        }

        public static bool ContainsRockWorld(Vector2 pos)
        {
            int x = (int)(pos.X / squareWidth);
            int y = (int)(pos.Y / squareHeight);
            if (InGrid(x,y))
            {
                TerrainSquare square = TerrainManager.terrainGrid[x, y];
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

        public void Unload()
        {

        }
    }
}
