using System;
using System.IO;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Runtime.InteropServices;
using Common;

namespace FezGame.Mod {
    public static class FEZModContentHelper {

        public static string Externalize(this string assetName) {
            return ("Resources\\" + (assetName.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString());
        }

        public static Bitmap ToBitmap(this Texture2D texture) {
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

            Bitmap bitmap = new Bitmap(texture.Width, texture.Height, pixelFormat);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            for (int i = 0; i < data.Length / texture.Format.Size(); i++) {
                for (int ii = 0; ii < texture.Format.Size(); ii++) {
                    Marshal.WriteByte(bitmapData.Scan0, i * texture.Format.Size() + ii, data[i * texture.Format.Size() + map[ii]]);
                }
            }

            bitmap.UnlockBits(bitmapData);
            //bitmap.Save(stream, outputFormat);
            return bitmap;
        }

    }
}

