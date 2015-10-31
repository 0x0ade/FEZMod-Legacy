using System.IO;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Mod {
    public struct CacheKey_RGB_A {
        public Texture2D RGB;
        public Texture2D A;
    }

    public static class FEZModContentHelper {

        #if FNA
        public static int Size(this SurfaceFormat surfaceFormat) {
            switch (surfaceFormat) {
            case SurfaceFormat.Dxt1:
                return 8;
            case SurfaceFormat.Dxt3:
            case SurfaceFormat.Dxt5:
                return 16;
            case SurfaceFormat.Alpha8:
                return 1;
            case SurfaceFormat.Bgr565:
            case SurfaceFormat.Bgra4444:
            case SurfaceFormat.Bgra5551:
            case SurfaceFormat.HalfSingle:
            case SurfaceFormat.NormalizedByte2:
                return 2;
            case SurfaceFormat.Color:
            case SurfaceFormat.Single:
            case SurfaceFormat.Rg32:
            case SurfaceFormat.HalfVector2:
            case SurfaceFormat.NormalizedByte4:
            case SurfaceFormat.Rgba1010102:
                return 4;
            case SurfaceFormat.HalfVector4:
            case SurfaceFormat.Rgba64:
            case SurfaceFormat.Vector2:
                return 8;
            case SurfaceFormat.Vector4:
                return 16;
            default:
                return 0;
            }
        }
        #endif

        private readonly static Dictionary<CacheKey_RGB_A, Texture2D> CacheMixAlpha = new Dictionary<CacheKey_RGB_A, Texture2D>();

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

        private static CacheKey_RGB_A mixAlpha_key;
        public static Texture2D MixAlpha(this Texture2D textureRGB, Texture2D textureA) {
            mixAlpha_key.RGB = textureRGB;
            mixAlpha_key.A = textureA;

            Texture2D textureRGBA;

            if (CacheMixAlpha.TryGetValue(mixAlpha_key, out textureRGBA)) {
                //return textureRGBA;
                textureRGBA.Dispose();
            }

            textureRGBA = new Texture2D(ServiceHelper.Game.GraphicsDevice, textureRGB.Width, textureRGB.Height);

            Color[] dataRGBA = new Color[textureRGBA.Width * textureRGBA.Height];
            textureRGB.GetData(dataRGBA);

            Color[] dataA = new Color[textureA.Width * textureA.Height];
            textureA.GetData(dataA);

            for (int i = 0; i < dataRGBA.Length; i++) {
                dataRGBA[i].A = dataA[i].A;
            }
            textureRGBA.SetData(dataRGBA);

            CacheMixAlpha[mixAlpha_key] = textureRGBA;

            return textureRGBA;
        }

    }
}

