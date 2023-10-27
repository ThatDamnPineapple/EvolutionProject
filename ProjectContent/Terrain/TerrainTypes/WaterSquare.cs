using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.Terrain.TerrainTypes
{
    internal class WaterSquare : TerrainSquare
    {
        public override int ID => 2;

        public override Color color => Color.Aqua;

        public WaterSquare(Vector2 position) : base(position) { }
    }
}
