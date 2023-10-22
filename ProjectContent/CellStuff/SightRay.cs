using Microsoft.Xna.Framework;
using Project1.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.CellStuff
{
    internal class SightRay
    {

        readonly float MaxLength = 500;
        readonly float Presision = 25;
        public float rotation;

        public float distance;

        public Color color;

        public bool pickedUp = false;

        public SightRay(float _rotation)
        {
            rotation = _rotation;
        }

        public void CastRay(Cell parent)
        {
            for (float i = 0; i < MaxLength; i+= Presision) 
            {
                Vector2 offset = Vector2.One.RotatedBy(rotation) * i;
                Vector2 checkPos = offset + parent.Center;
                var closestCell = CellManager.cells.Where(n => CollisionHelper.CheckBoxvPointCollision(n.position, n.size, checkPos)).FirstOrDefault();
                if (closestCell != default)
                {
                    distance = i;
                    color = closestCell.color;
                    pickedUp = true;
                    return;
                }
            }
        }
    }
}
