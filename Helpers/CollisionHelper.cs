using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Helpers
{
    public static class CollisionHelper
    {
        public static bool CheckBoxvBoxCollision(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2) //uses center
        {
            return (pos1.X < pos2.X + size2.X &&
                pos1.X + size1.X > pos2.X &&
                pos1.Y < pos2.Y + size2.Y &&
                pos1.Y + size1.Y > pos2.Y);
        }

        public static bool CheckBoxvPointCollision(Vector2 boxPos, Vector2 boxSize, Vector2 pointPos) //uses corner
        {
            return (pointPos.X > boxPos.X &&
                pointPos.X < boxPos.X + boxSize.X &&
                pointPos.Y > boxPos.Y &&
                pointPos.Y < boxPos.Y + boxSize.Y);
        }

        public static Vector2 StopBox(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2, ref Vector2 vel) //uses center
        {
            if (!CheckBoxvBoxCollision(pos1, size1, pos2, size2))
                return pos1;

            int velsignX = MathF.Sign(vel.X);
            int collisionsignX = MathF.Sign(pos2.X - pos1.X);
            if (velsignX == collisionsignX)
            {
                vel.X = 0;
                //pos1.X = pos2.X - (((size2.X / 2) + (size1.X / 2)) * collisionsignX);
            }

            int velsignY = MathF.Sign(vel.Y);
            int collisionsignY = MathF.Sign(pos2.Y - pos1.Y);
            if (velsignY == collisionsignY)
            {
                vel.Y = 0;
                //pos1.Y = pos2.Y - (((size2.Y / 2) + (size1.Y / 2)) * collisionsignY);
            }

            return pos1;
        }
    }
}
