using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Interfaces
{
    internal interface IUpdatable
    {
        public float UpdatePriority => 1.0f;

        public void Update(GameTime gameTime);
    }
}
