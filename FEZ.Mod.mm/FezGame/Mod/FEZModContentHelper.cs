using System;
using System.IO;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using Common;
using System.Collections.Generic;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Mod {
    public static class FEZModContentHelper {

        private readonly static Dictionary<Texture2D[], Texture2D> mixAlphaMap = new Dictionary<Texture2D[], Texture2D>();
        private readonly static Texture2D[][] mixAlphaCaches = new Texture2D[32][];
        private static int mixAlphaIndex = 0;

        public static string Externalize(this string assetName) {
            return ("Resources\\" + (assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString());
        }

        public static System.Drawing.Bitmap ToBitmap(this Texture2D texture) {
            var data = new byte[texture.Width * texture.Height * texture.Format.Size()];
            texture.GetData(data);

            PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
            int[] map = {2, 1, 0};

            switch (texture.Format) {
            case SurfaceFormat.Bgr565:
                pixelFormat = PixelFormat.Format16bppRgb565;
                break;
            case SurfaceFormat.Bgra4444:
                break;
            case SurfaceFormat.Bgra5551:
                pixelFormat = PixelFormat.Format16bppArgb1555;
                map = new int[] {2, 1, 0, 3};
                break;
            case SurfaceFormat.Color:
                pixelFormat = PixelFormat.Format32bppArgb;
                map = new int[] {2, 1, 0, 3};
                break;
            }

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(texture.Width, texture.Height, pixelFormat);
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            for (int i = 0; i < data.Length / texture.Format.Size(); i++) {
                for (int ii = 0; ii < texture.Format.Size(); ii++) {
                    Marshal.WriteByte(bitmapData.Scan0, i * texture.Format.Size() + ii, data[i * texture.Format.Size() + map[ii]]);
                }
            }

            bitmap.UnlockBits(bitmapData);
            //bitmap.Save(stream, outputFormat);
            return bitmap;
        }

        public static Texture2D MixAlpha(this Texture2D textureRGB, Texture2D textureA) {
            Texture2D[] cache = mixAlphaCaches[mixAlphaIndex];
            if (cache == null) {
                cache = mixAlphaCaches[mixAlphaIndex] = new Texture2D[2];
            }
            mixAlphaIndex = (mixAlphaIndex + 1) % mixAlphaCaches.Length;
            cache[0] = textureRGB;
            cache[1] = textureA;

            Texture2D textureRGBA;

            if (mixAlphaMap.TryGetValue(cache, out textureRGBA)) {
                return textureRGBA;
            }

            textureRGBA = new Texture2D(ServiceHelper.Game.GraphicsDevice, textureRGB.Width, textureRGB.Height);
            Color[] dataRGBA = new Color[textureRGBA.Width * textureRGBA.Height];
            textureRGB.GetData(dataRGBA);
            Color[] dataA = new Color[textureA.Width * textureA.Height];
            textureA.GetData(dataA);
            textureRGBA.GetData(dataRGBA);
            for (int i = 0; i < dataRGBA.Length; i++) {
                dataRGBA[i].A = dataA[i].A;
            }
            textureRGBA.SetData(dataRGBA);

            mixAlphaMap[cache] = textureRGBA;

            return textureRGBA;
        }

    }
}

