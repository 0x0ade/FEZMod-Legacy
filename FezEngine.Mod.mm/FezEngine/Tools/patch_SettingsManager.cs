#pragma warning disable 436
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FezEngine.Mod;
using MonoMod;

namespace FezEngine.Tools {
    public static class patch_SettingsManager {

        private static float viewScale;
        
        //originally public but must be private as it's an extension method
        #if FNA
        private static extern void orig_SetupViewport(this GraphicsDevice device);
        private static void SetupViewport(this GraphicsDevice device) {
            //FEZ 1.12 disables letterboxing (at least during the beta)
            orig_SetupViewport(device);
        #else
        private static void SetupViewport(this GraphicsDevice device, bool forceLetterbox = false) {
            int backBufferWidth = device.PresentationParameters.BackBufferWidth;
            int backBufferHeight = device.PresentationParameters.BackBufferHeight;
            /*if (!forceLetterbox) {
                RenderTargetBinding[] renderTargets = device.GetRenderTargets();
                if (renderTargets.Length > 0 && renderTargets[0].RenderTarget is Texture2D) {
                    return;
                }
            }*/
            device.ScissorRectangle = new Rectangle(0, 0, backBufferWidth, backBufferHeight);
            device.Viewport = new Viewport {
                X = 0,
                Y = 0,
                Width = backBufferWidth,
                Height = backBufferHeight,
                MinDepth = 0,
                MaxDepth = 1
            };
        #endif
            
            if (FEZModEngine.EnablePPHD) {
                viewScale = 1f;
            } else {
                float scale = (float) backBufferWidth / (float) backBufferHeight;
                if (scale > (16f/9f)) {
                    viewScale = (float) device.Viewport.Height / (1280f * scale);
                } else {
                    viewScale = (float) device.Viewport.Width / (720f * scale);
                }
                if (viewScale < 1f) {
                    viewScale = 1f;
                }
            }
        }
        
    }
}