using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.Terrain.TerrainTypes
{
    internal class GrassSquare : TerrainSquare
    {
        public override int ID => 3;
        public override Color color => Color.Green;

        public GrassSquare(Vector2 position, float width, float height) : base(position, width, height) { }
    }
}
