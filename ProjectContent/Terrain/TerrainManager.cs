using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1.Interfaces;
using Project1.ProjectContent.Terrain.TerrainTypes;
using Project1.Core.Noise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.Terrain
{
    internal class TerrainManager : ILoadable, IDraw
    {
        public readonly static float squareWidth = 48;

        public readonly static float squareHeight = 48;

        public readonly static int gridWidth = 32;

        public readonly static int gridHeight = 32;
        public float LoadPriority => 1.1f;

        public float DrawPriority => 0.1f;

        internal static TerrainSquare[,] terrainGrid;

        public void Load() 
        {
            terrainGrid = new TerrainSquare[gridWidth,gridHeight];
            PopulateGrid();
            Game1.drawables.Add(this);
        } 

        public void PopulateGrid()
        {
            float waterThreshhold = 0.5f;
            FastNoiseLite noise = new FastNoiseLite(Game1.random.Next());
            noise.SetFrequency(0.1f);

            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    float threshhold = noise.GetNoise(i, j);

                    if (threshhold > waterThreshhold)
                        terrainGrid[i, j] = new WaterSquare(new Vector2(i * squareWidth, j * squareHeight));
                    else
                        terrainGrid[i, j] = new GrassSquare(new Vector2(i * squareWidth, j * squareHeight));
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
