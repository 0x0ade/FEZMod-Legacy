#pragma warning disable 436
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Xml;
using FezEngine.Mod;
using MonoMod;

namespace FezEngine.Tools {
    public class SharedContentManager {

        //non-existent in FEZ 1.11 for Windows; didn't test on Linux, but Mono didn't complain.
        #if FNA
        [MonoModLinkTo(typeof(ContentManager), "Dispose")]
        public extern void Dispose();
        #else
        public void Dispose() {
            //uuuhhh... didn't SharedContentManger have Close()?
        }
        #endif
        
        private class CommonContentManager {

            private extern T orig_ReadAsset<T>(string assetName);
            private T ReadAsset<T>(string assetName) where T : class {
                if (assetName.Contains("-fm-")) {
                    assetName = assetName.Substring(0, assetName.IndexOf("-fm-"));
                }

                if (typeof(T) == typeof(Texture2D)) {
                    string imagePath = assetName.Externalize();
                    string imageExtension = null;

                    if (File.Exists(imagePath + ".png")) {
                        imageExtension = ".png";
                    } else if (File.Exists(imagePath + ".jpg")) {
                        imageExtension = ".jpg";
                    } else if (File.Exists(imagePath + ".jpeg")) {
                        imageExtension = ".jpeg";
                    } else if (File.Exists(imagePath + ".gif")) {
                        imageExtension = ".gif";
                    }

                    if (imageExtension != null) {
                        using (FileStream fs = new FileStream(imagePath + imageExtension, FileMode.Open)) {
                            return Texture2D.FromStream(ServiceHelper.Game.GraphicsDevice, fs) as T;
                        }
                    }
                }

                string xmlPath = assetName.Externalize() + ".xml";
                if (File.Exists(xmlPath)) {
                    FileStream fis = new FileStream(xmlPath, FileMode.Open);
                    XmlReader xmlReader = XmlReader.Create(fis);
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(xmlReader);
                    xmlReader.Close();
                    fis.Close();
                    xmlDocument.DocumentElement.SetAttribute("assetName", assetName);

                    return xmlDocument.Deserialize(null, null, true) as T;
                }

                return orig_ReadAsset<T>(assetName);
            }

        }

    }
}

