using Project1.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.CellStuff
{
    public class CellStat
    {
        public float Value;

        public float Mutation;

        public float Mutation2;

        public CellStat(float value, float mutation, float mutation2)
        {
            Value = value;
            Mutation = mutation;
            Mutation2 = mutation2;
        }

        public void Mutate()
        {
            Value += Game1.random.NextFloat(-Mutation, Mutation);
            Mutation += Game1.random.NextFloat(-Mutation2, Mutation2);
        }
    }
}
