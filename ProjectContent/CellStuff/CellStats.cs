using Microsoft.Xna.Framework;
using EvoSim.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.CellStuff
{
    public class CellStat
    {
        public float Value;

        public float Mutation;

        public float Mutation2;

        public float Min;

        public float Max;

        public float MutationPower;

        public float Weight;

        public bool Multiplicative;

        public float DistanceMult;

        public static float Distance(CellStat a, CellStat b)
        {
            float range = a.Max - a.Min;

            return (MathF.Abs(a.Value - b.Value) / range) * a.DistanceMult;
        }

        public CellStat(float value, float mutation, float mutation2, float min, float max, float mutationPower, float distanceMult, bool multiplicative)
        {
            Value = value;
            Mutation = mutation;
            Mutation2 = mutation2;
            Min = min;
            Max = max;
            MutationPower = mutationPower;
            Multiplicative = multiplicative;
            DistanceMult = distanceMult;
        }

        public static float CreateWeightedMutation(float val, float mutationPower)
        {
            return MathF.Pow(Main.random.NextFloat(), mutationPower) * Main.random.NextSign() * val;
        }

        public void Mutate()
        {
            int mutationAmount = SceneManager.firstMutation ? 100 : 1;
            for (int i = 0; i < mutationAmount; i++)
            {
                if (Multiplicative)
                {
                    Value *= 1 + CreateWeightedMutation(Mutation, MutationPower) + Weight;
                    Weight += CreateWeightedMutation(Mutation2, 3);
                    Weight *= 0.95f;
                }
                else
                {
                    Value += CreateWeightedMutation(Mutation, MutationPower) + Weight;
                    Weight += CreateWeightedMutation(Mutation2, 3);
                    Weight *= 0.95f;
                }

                Value = Math.Clamp(Value, Min, Max);
            }
        }

        public CellStat Combine(CellStat other)
        {
            CellStat ret = new CellStat(Value, Mutation, Mutation2, Min, Max, MutationPower, DistanceMult, Multiplicative);
            ret.Value = MathHelper.Lerp(Value, other.Value, 0.5f);
            ret.Mutation = MathHelper.Lerp(Mutation, other.Mutation, 0.5f);
            ret.Mutation2 = MathHelper.Lerp(Mutation2, other.Mutation2, 0.5f);
            ret.Mutate();
            return ret;
        }

        public CellStat Duplicate()
        {
            CellStat ret = new CellStat(Value, Mutation, Mutation2, Min, Max, MutationPower, DistanceMult, Multiplicative);
            ret.Mutate();
            return ret;
        }
    }
}
