using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EvoSim.ProjectContent.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.Helpers
{
    internal static class DrawHelper
    {
        public static Texture2D MagicPixel;

        public static SpriteFont Arial;

        public static void DrawPixel(SpriteBatch spriteBatch, Color color, Vector2 position, float width, float height, bool inWorld = true)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 offset = inWorld ? SceneManager.camera.position : Vector2.Zero;
                    offset += inWorld ? SceneManager.grid.mapSize * new Vector2(i, j) : Vector2.Zero;
                    spriteBatch.Draw(MagicPixel, position + offset, null, color, 0, Vector2.Zero, new Vector2(width, height), SpriteEffects.None, 0f);
                }
            }
        }

        public static void DrawText(SpriteBatch spriteBatch, string text, Color color, Vector2 position, Vector2 scale, bool inWorld = true)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 offset = inWorld ? SceneManager.camera.position : Vector2.Zero;
                    offset += inWorld ? SceneManager.grid.mapSize * new Vector2(i, j) : Vector2.Zero;
                    spriteBatch.DrawString(Arial, text, position + offset, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}
