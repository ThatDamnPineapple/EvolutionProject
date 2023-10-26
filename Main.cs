
#region global usings
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using EvoSim.ProjectContent.CellStuff;
global using EvoSim.ProjectContent.Camera;
global using EvoSim.ProjectContent.SceneStuff;
global using EvoSim.Interfaces;
global using System;
global using System.Collections.Generic;
global using System.Linq;
#endregion

using Microsoft.Xna.Framework.Input;
using EvoSim.Helpers;
using SharpDX.X3DAudio;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace EvoSim
{
    public class Main : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public static Random random;

        public static float delta;
        private static float oldTime;

        public static Vector2 ScreenSize => new Vector2(1900, 950);

        internal static List<ILoadable> loadCache;

        internal static List<IDraw> drawables = new List<IDraw>();

        internal static List<IUpdate> updatables = new List<IUpdate>();

        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = (int)ScreenSize.X;
            _graphics.PreferredBackBufferHeight = (int)ScreenSize.Y;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            random = new Random();

            loadCache = new List<ILoadable>();

            foreach (Type type in Assembly.GetAssembly(typeof(Main)).GetTypes())
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
            DrawHelper.Arial = Content.Load<SpriteFont>("fonts/Arial");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (IUpdate updatable in updatables.OrderBy(n => n.UpdatePriority))
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
            
            foreach (IDraw drawable in drawables.OrderBy(n => n.DrawPriority))
            {
                drawable.Draw(_spriteBatch);
            }    

            _spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}