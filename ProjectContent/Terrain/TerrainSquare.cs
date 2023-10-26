using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EvoSim.Interfaces;
using EvoSim.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.ProjectContent.Terrain.TerrainTypes;

namespace EvoSim.ProjectContent.Terrain
{
    public abstract class TerrainSquare
    {
        public virtual Color color => Color.White;

        public virtual float width => SceneManager.grid.squareWidth;

        public virtual float height => SceneManager.grid.squareHeight;

        public Vector2 position = Vector2.Zero;

        public TerrainSquare(Vector2 _position)
        {
            position = _position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawPixel(spriteBatch, color, position, Vector2.Zero, width, height);
        }

        public static float GetTerrainID(TerrainSquare square)
        {
            if (square is GrassSquare)
                return 1.0f;
            if (square is WaterSquare)
                return 2.0f;
            if (square is RockSquare)
                return 3.0f;
            return 0;
        }
    }
}
