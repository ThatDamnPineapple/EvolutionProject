using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Interfaces
{
    internal interface ILoadable
    {
        public float LoadPriority => 1.0f;
        public void Load();

        public void Unload();
    }
}
