using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using EvoSim.Helpers;
using EvoSim.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.Helpers.HelperClasses;

namespace EvoSim.ProjectContent.Camera
{
    internal class CameraObject : IUpdate
    {

        public float UpdatePriority => 0.1f;

        public Vector2 position = Vector2.Zero;

        public float acceleration = 0.25f;

        public float maxSpeed = 1500;

        public Vector2 velocity = Vector2.Zero;

        ButtonToggle centerToggle;

        public bool centerCamera = false;

        public CameraObject()
        {
            centerToggle = new ButtonToggle(new PressingButton(() => Keyboard.GetState().IsKeyDown(Keys.C)), new ButtonAction((object o) => centerCamera = !centerCamera));
        }

        public void Update(GameTime gameTime)
        {
            centerToggle.Update(this);
            MovementLogic(gameTime);
        }

        public void CenterCamera()
        {
            if (!centerCamera)
                return;
            Vector2 newPos = Vector2.Zero;
            int highestCaptured = 0;
            for (int i = 0; i < 200; i++)
            {
                newPos.X = Main.random.NextFloat(SceneManager.grid.mapSize.X);
                newPos.Y = Main.random.NextFloat(SceneManager.grid.mapSize.Y);

                int currentlyCaptured = SceneManager.cellSimulation.PList.basicList.Count(n => n.IsActive() && CollisionHelper.CheckBoxvBoxCollision(newPos, Main.ScreenSize, n.position, n.Size));
                if (currentlyCaptured > highestCaptured)
                {
                    highestCaptured = currentlyCaptured;
                    SceneManager.camera.position = newPos;
                }
            }
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
