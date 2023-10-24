using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Interfaces
{
    internal interface IDraw
    {
        public float DrawPriority => 1.0f;

        public void Draw(SpriteBatch spriteBatch);
    }
}
