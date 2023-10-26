using Microsoft.Xna.Framework;
using EvoSim.Helpers;
using EvoSim.ProjectContent.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.CellStuff
{
    internal class SightRay
    {

        readonly float MaxLength = 300;
        readonly float Presision = 15;
        public float rotation;

        public float distance;

        public float similarity = 0;

        public Color color = Color.Black;

        public bool pickedUp = false;

        public float scale;

        public float health;
        public float energy;

        public Vector2 velocity;

        public float fitness;

        public float age;

        public float child;

        public SightRay(float _rotation)
        {
            rotation = _rotation;
        }

        public void CastRay(Cell parent)
        {
            distance = 10000;
            similarity = 100;
            color = Color.Black;
            health = 0;
            energy = 0;
            fitness = 0;
            velocity = Vector2.Zero;
            age = -100;
            child = 0;
            for (float i = 0; i < MaxLength; i+= Presision) 
            {
                Vector2 offset = Vector2.One.RotatedBy(rotation) * i;
                Vector2 checkPos = offset + parent.Center;     

                while (checkPos.X > SceneManager.grid.mapSize.X)
                {
                    checkPos.X -= SceneManager.grid.mapSize.X;
                }

                while (checkPos.X < 0)
                {
                    checkPos.X += SceneManager.grid.mapSize.X;
                }

                while (checkPos.Y > SceneManager.grid.mapSize.Y)
                {
                    checkPos.Y -= SceneManager.grid.mapSize.Y;
                }

                while (checkPos.Y < 0)
                {
                    checkPos.Y += SceneManager.grid.mapSize.Y;
                }

                var closestCell = SceneManager.simulation?.Agents.Where(n => CollisionHelper.CheckBoxvPointCollision((n as Cell).position, (n as Cell).Size, checkPos)).FirstOrDefault();
                if (closestCell != default)
                {
                    var closestCellCast = closestCell as Cell;
                    distance = i;
                    if (closestCellCast.GetGenome() != null)
                        similarity = (float)parent.Distance(closestCellCast);
                    else
                        similarity = 0;
                    scale = closestCellCast.Size.Length();
                    color = closestCellCast.color;
                    energy = closestCellCast.energy;
                    health = closestCellCast.health;
                    pickedUp = true;
                    fitness = closestCellCast.GetFitness(false, true);
                    velocity = closestCellCast.velocity;
                    age = closestCellCast.lifeCounter;

                    if (parent.children.Contains(closestCellCast))
                        child = 100;

                    if (parent.parents.Contains(closestCellCast))
                        child = -100;
                    return;
                }

                var closestFood = FoodManager.foods.Where(n => CollisionHelper.CheckBoxvPointCollision(n.position, n.size, checkPos)).FirstOrDefault();
                if (closestFood != default)
                {
                    distance = i;
                    similarity = 100;
                    energy = closestFood.energy;
                    health = 0;
                    scale = closestFood.size.Length();
                    color = closestFood.color;
                    pickedUp = true;
                    return;
                }
            }

        }

        public void FeedData(List<float> data)
        {
            data.Add(distance - (MaxLength / 2.0f));
            data.Add(color.R - 128);
            data.Add(color.G - 128);
            data.Add(color.B - 128);
            data.Add(similarity * 1000);
            data.Add(health);
            data.Add(energy);
            data.Add(scale * 100);
            data.Add(fitness);
            data.Add(velocity.X);
            data.Add(velocity.Y);
            data.Add(age);
            data.Add(child);
        }
    }
}
