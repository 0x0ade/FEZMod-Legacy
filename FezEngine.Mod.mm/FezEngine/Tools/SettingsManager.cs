using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FezGame.Mod;

namespace FezEngine.Tools {
    public static class SettingsManager {

        private static float viewScale;

        public static void SetupViewport(this GraphicsDevice device, bool forceLetterbox = false) {
            int backBufferWidth = device.PresentationParameters.BackBufferWidth;
            int backBufferHeight = device.PresentationParameters.BackBufferHeight;
            if (!forceLetterbox) {
                RenderTargetBinding[] renderTargets = device.GetRenderTargets();
                if (renderTargets.Length > 0 && renderTargets[0].RenderTarget is Texture2D) {
                    return;
                }
            }
            device.ScissorRectangle = new Rectangle(0, 0, backBufferWidth, backBufferHeight);
            device.Viewport = new Viewport {
                X = 0,
                Y = 0,
                Width = backBufferWidth,
                Height = backBufferHeight,
                MinDepth = 0,
                MaxDepth = 1
            };
            if (FEZMod.EnablePPHD) {
                SettingsManager.viewScale = 1f;
            } else {
                SettingsManager.viewScale = ((float) device.Viewport.Width / 1280f + (float) device.Viewport.Height / 720f) / 2f;
            }
        }

    }
}

