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

        public virtual int ID => 0;

        public TerrainSquare(Vector2 _position)
        {
            position = _position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawPixel(spriteBatch, color, position, Vector2.Zero, width, height);
        }
    }
}
