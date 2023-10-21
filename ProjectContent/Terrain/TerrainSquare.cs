using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1.Interfaces;
using Project1.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.Terrain
{
    internal abstract class TerrainSquare
    {
        public virtual Color color => Color.White;

        public virtual float width => TerrainManager.squareWidth;

        public virtual float height => TerrainManager.squareHeight;

        public Vector2 position = Vector2.Zero;

        public TerrainSquare(Vector2 _position)
        {
            position = _position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawPixel(spriteBatch, color, position, width, height);
        }
    }
}
