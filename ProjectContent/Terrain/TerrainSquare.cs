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
using Aardvark.Base;

namespace EvoSim.ProjectContent.Terrain
{
    public abstract class TerrainSquare
    {
        public virtual Color color => Color.White;

        public float width;

        public float height;

        public Vector2 position = Vector2.Zero;

        public Box2d box = new Box2d();

        public virtual int ID => 0;

        public TerrainSquare(Vector2 _position, float width, float height)
        {
            this.width = width; 
            this.height = height;
            box.Min = _position.ToV2d();
            box.Max = box.Min + new V2d(width, height);
            position = _position;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawPixel(spriteBatch, color, position, Vector2.Zero, width, height);
        }
    }
}
