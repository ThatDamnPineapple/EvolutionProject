﻿using Project1.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.ProjectContent.Camera
{
    internal class CameraManager : ILoadable
    {
        public static Camera camera;
        public float LoadPriority => 1.0f;

        public void Load() 
        {
            camera = new Camera();
            Game1.updatables.Add(camera);
        }

        public void Unload() { }
    }
}