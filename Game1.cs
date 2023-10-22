using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Project1.Helpers;
using Project1.Interfaces;
using SharpDX.X3DAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Project1
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public static Random random;

        public static float delta;
        private static float oldTime;

        internal static List<ILoadable> loadCache;

        internal static List<IDraw> drawables = new List<IDraw>();

        internal static List<IUpdatable> updatables = new List<IUpdatable>();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            random = new Random();

            loadCache = new List<ILoadable>();

            foreach (Type type in Assembly.GetAssembly(typeof(Game1)).GetTypes())
            {
                if (!type.IsAbstract && type.GetInterfaces().Contains(typeof(ILoadable)))
                {
                    object instance = Activator.CreateInstance(type);
                    loadCache.Add(instance as ILoadable);
                }

                loadCache.Sort((n, t) => n.LoadPriority.CompareTo(t.LoadPriority));
            }

            for (int k = 0; k < loadCache.Count; k++)
            {
                loadCache[k].Load();
            }

            DrawHelper.MagicPixel = Content.Load<Texture2D>("sprites/MagicPixel");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            delta = (float)gameTime.ElapsedGameTime.TotalSeconds - oldTime;

            foreach (IUpdatable updatable in updatables)
            {
                updatable.Update(gameTime);
            }

            oldTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            
            foreach (IDraw drawable in drawables)
            {
                drawable.Draw(_spriteBatch);
            }    

            _spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}