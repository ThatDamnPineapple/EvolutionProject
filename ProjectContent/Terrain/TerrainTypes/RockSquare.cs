using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.Terrain.TerrainTypes
{
    internal class GrassSquare : TerrainSquare
    {
        public override Color color => Color.Green;

        public GrassSquare(Vector2 position) : base(position) { }
    }
}
