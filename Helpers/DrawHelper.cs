using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1.ProjectContent.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Helpers
{
    internal static class DrawHelper
    {
        public static Texture2D MagicPixel;

        public static void DrawPixel(SpriteBatch spriteBatch, Color color, Vector2 position, float width, float height, bool inWorld = true)
        {
            Vector2 offset = inWorld ? CameraManager.camera.position : Vector2.Zero;
            spriteBatch.Draw(MagicPixel, position + offset, null, color, 0, Vector2.Zero, new Vector2(width, height), SpriteEffects.None, 0f);
        }
    }
}
