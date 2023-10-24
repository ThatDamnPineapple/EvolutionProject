using Microsoft.Xna.Framework;
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

        public float Min;

        public float Max;

        public bool Multiplicative;

        public CellStat(float value, float mutation, float mutation2, float min, float max, bool multiplicative)
        {
            Value = value;
            Mutation = mutation;
            Mutation2 = mutation2;
            Min = min;
            Max = max;
            Multiplicative = multiplicative;
        }

        public void Mutate()
        {
            if (Multiplicative)
            {
                Value *= 1 + Game1.random.NextFloat(-Mutation, Mutation);
                Mutation *= 1 + Game1.random.NextFloat(-Mutation2, Mutation2);
            }
            else
            {
                Value += Game1.random.NextFloat(-Mutation, Mutation);
                Mutation += Game1.random.NextFloat(-Mutation2, Mutation2);
            }

            Value = Math.Clamp(Value, Min, Max);
        }

        public CellStat Combine(CellStat other)
        {
            CellStat ret = new CellStat(Value, Mutation, Mutation2, Min, Max, Multiplicative);
            ret.Value = MathHelper.Lerp(Value, other.Value, 0.5f);
            ret.Mutation = MathHelper.Lerp(Mutation, other.Mutation, 0.5f);
            ret.Mutation2 = MathHelper.Lerp(Mutation2, other.Mutation2, 0.5f);
            ret.Mutate();
            return ret;
        }

        public CellStat Duplicate()
        {
            CellStat ret = new CellStat(Value, Mutation, Mutation2, Min, Max, Multiplicative);
            ret.Mutate();
            return ret;
        }
    }
}
