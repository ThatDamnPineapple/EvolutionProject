using EvoSim.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.SceneStuff
{
    public class SeasonManager : IUpdate, ILoadable
    {
        public static readonly float YEARLENGTH = 30;

        public static readonly float BASESUNLIGHT = 2000;

        public static readonly float SUNLIGHTVARIATION = 1500;

        public static float SeasonSin => MathF.Sin((SeasonTimer * 6.28f) / YEARLENGTH);
        public static float SeasonCos => MathF.Cos((SeasonTimer * 6.28f) / YEARLENGTH);

        public static float CurrentRegen => BASESUNLIGHT + (SUNLIGHTVARIATION * SeasonSin);

        public static float SeasonTimer;

        public void Update(GameTime gameTime)
        {
            SeasonTimer += Main.delta;
        }

        public void Load()
        {
            Main.updatables.Add(this);
        }

        public void Unload()
        {

        }
    }
}
