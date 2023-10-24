using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.Terrain.TerrainTypes
{
    internal class RockSquare : TerrainSquare
    {
        public override Color color => Color.Gray;

        public RockSquare(Vector2 position) : base(position) { }
    }
}
