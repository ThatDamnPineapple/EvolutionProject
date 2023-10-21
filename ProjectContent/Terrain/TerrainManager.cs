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

        private readonly int width = 128;

        private readonly int height = 128;
        public float LoadPriority => 1.1f;

        public float DrawPriority => 0.1f;

        internal static TerrainSquare[,] terrainGrid;

        public void Load() 
        {
            terrainGrid = new TerrainSquare[width,height];
            PopulateGrid();
            Game1.drawables.Add(this);
        } 

        public void PopulateGrid()
        {
            float waterThreshhold = 0.5f;
            FastNoiseLite noise = new FastNoiseLite(Game1.random.Next());
            noise.SetFrequency(0.1f);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
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
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
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
