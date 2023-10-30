using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using EvoSim.Helpers;
using EvoSim.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.Camera
{
    internal class CameraObject : IUpdate
    {

        public float UpdatePriority => 0.1f;

        public Vector2 position = Vector2.Zero;

        public float acceleration = 0.25f;

        public float maxSpeed = 1500;

        public Vector2 velocity = Vector2.Zero;

        public void Update(GameTime gameTime)
        {
            MovementLogic(gameTime);
        }

        public void MovementLogic(GameTime gameTime)
        {
            Vector2 newVelocity = Vector2.Zero;
            KeyboardState state = Keyboard.GetState();
            if (state.IsKeyDown(Keys.W))
            {
                newVelocity.Y = -1;
            }

            if(state.IsKeyDown(Keys.S))
            {
                newVelocity.Y = 1;
            }

            if (state.IsKeyDown(Keys.A))
            {
                newVelocity.X = -1;
            }

            if (state.IsKeyDown(Keys.D))
            {
                newVelocity.X = 1;
            }

            newVelocity = newVelocity.SafeNormalize() * maxSpeed;

            velocity = Vector2.Lerp(velocity, newVelocity, acceleration);

            position += velocity * Main.delta;

            while (position.X > SceneManager.grid.mapSize.X)
            {
                position.X -= SceneManager.grid.mapSize.X;
            }

            while (position.X < 0)
            {
                position.X += SceneManager.grid.mapSize.X;
            }

            while (position.Y > SceneManager.grid.mapSize.Y)
            {
                position.Y -= SceneManager.grid.mapSize.Y;
            }

            while (position.Y < 0)
            {
                position.Y += SceneManager.grid.mapSize.Y;
            }
        }
    }
}
