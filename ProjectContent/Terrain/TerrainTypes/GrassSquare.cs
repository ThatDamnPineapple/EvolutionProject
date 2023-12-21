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
        public override Color color => GetColor();

        public GrassSquare(Vector2 position, float width, float height) : base(position, width, height) { }

        public static Color GetColor()
        {
            Color spring = new Color(49, 206, 77);
            Color summer = new Color(199, 255, 82);
            Color fall = new Color(255, 152, 62);
            Color winter = new Color(171, 238, 255);
            float sin = SeasonManager.SeasonSin;
            float cos = SeasonManager.SeasonCos;
            if (MathF.Sign(sin) == 1 && MathF.Sign(cos) == 1) //Spring => summer
            {
                return Color.Lerp(spring, summer, sin);
            }
            if (MathF.Sign(sin) == 1 && MathF.Sign(cos) != 1) //Summer => fall
            {
                return Color.Lerp(fall, summer, sin);
            }
            if (MathF.Sign(sin) != 1 && MathF.Sign(cos) != 1) //fall => winter
            {
                return Color.Lerp(fall, winter, -sin);
            }
            if (MathF.Sign(sin) != 1 && MathF.Sign(cos) == 1) //winter => spring
            {
                return Color.Lerp(winter, spring, cos);
            }
            return Color.White;
        }
    }
}
