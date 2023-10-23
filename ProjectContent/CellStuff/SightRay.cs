using Microsoft.Xna.Framework;
using Project1.Helpers;
using Project1.ProjectContent.Resources;
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
        readonly float Presision = 5;
        public float rotation;

        public float distance;

        public float similarity = 0;

        public Color color = Color.Black;

        public bool pickedUp = false;

        public float scale;

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
                var closestFood = FoodManager.foods.Where(n => CollisionHelper.CheckBoxvPointCollision(n.position, n.size, checkPos)).FirstOrDefault();
                if (closestFood != default)
                {
                    distance = i;
                    similarity = 100;
                    scale = closestFood.size.Length();
                    color = closestFood.color;
                    pickedUp = true;
                    return;
                }

                var closestCell = CellManager.cells.Where(n => CollisionHelper.CheckBoxvPointCollision(n.position, n.size, checkPos)).FirstOrDefault();
                if (closestCell != default)
                {
                    distance = i;
                    if (closestCell.GetGenome() != null)
                        similarity = (float)parent.Distance(closestCell);
                    else
                        similarity = 0;
                    scale = closestCell.size.Length();
                    color = closestCell.color;
                    pickedUp = true;
                    return;
                }

                if (!CollisionHelper.CheckBoxvPointCollision(Vector2.Zero, Terrain.TerrainManager.mapSize, checkPos))
                {
                    distance = i;
                    similarity = 100;
                    scale = 990;
                    color = Color.Green;
                    return;
                }

                distance = 10000;
                similarity = 100;
                color = Color.Black;
                
            }
        }

        public void FeedData(List<float> data)
        {
            data.Add(distance - (MaxLength / 2.0f));
            data.Add(color.R - 128);
            data.Add(color.G - 128);
            data.Add(color.B - 128);
            data.Add(similarity);
            data.Add(scale);
        }
    }
}
