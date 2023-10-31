using Aardvark.Base;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Helpers
{
    public static class CollisionHelper
    {
        public static bool CheckBoxvBoxCollision(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2) //uses position
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

        public static V2d StopBox(Box2d box1, Box2d box2, ref Vector2 vel) //uses position
        {
            if (box1.Intersects(box2))
            {
                V2d shift = new V2d();

                int dirX = 0;
                int dirY = 0;
                if (box1.Center.X > box2.Center.X)
                {
                    dirX = 1;
                    shift.X = box2.Max.X - box1.Min.X;
                }
                else
                {
                    dirX = -1;
                    shift.X = box2.Min.X - box1.Max.X;
                }

                if (box1.Center.Y > box2.Center.Y)
                {
                    dirY = 1;
                    shift.Y = box2.Max.Y - box1.Min.Y;
                }
                else
                {
                    dirY = -1;
                    shift.Y = box2.Min.Y - box1.Max.Y;
                }

                int xShift = (int)shift.X;
                int yShift = (int)shift.Y;
                int xVel = (int)vel.X;
                int yVel = (int)vel.Y;

                if (MathF.Sign(vel.X) != MathF.Sign((float)(-shift.X)))
                {
                    //shift.X = 0;
                }
                else
                {
                    vel.X = 0;
                }

                if (MathF.Sign(vel.Y) != MathF.Sign((float)(-shift.Y)))
                {
                    //shift.Y = 0;
                }
                else
                {
                    vel.Y = 0;
                }

                

                double absX = Math.Abs(vel.X);
                double absY = Math.Abs(vel.Y);

                if (absX > absY)
                {
                    shift.Y *= (absY / absX);
                }
                else if (absX < absY)
                {
                    shift.X *= (absX / absY);
                }

                shift.X += 2 * Math.Sign(shift.X);
                shift.Y += 2 * Math.Sign(shift.Y);

                if (shift.Length != 0)
                {
                    int y = 0;
                }
                return shift;
            }
            return new V2d(0, 0);
        }
    }
}
