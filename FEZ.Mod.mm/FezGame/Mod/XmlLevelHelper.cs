using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace FezGame.Mod {
    public class XmlLevelHelper {
        private XmlLevelHelper() {
        }

        public static Object Parse(XmlElement elem) {
            if (elem == null) {
                return null;
            }

            if (elem.Name == "TrileEmplacement") {
                return new TrileEmplacement(
                    int.Parse(elem.GetAttribute("x")),
                    int.Parse(elem.GetAttribute("y")),
                    int.Parse(elem.GetAttribute("z"))
                );
            }

            if (elem.Name == "Vector3") {
                return new Vector3(
                    float.Parse(elem.GetAttribute("x")),
                    float.Parse(elem.GetAttribute("y")),
                    float.Parse(elem.GetAttribute("z"))
                );
            }

            if (elem.Name == "Quaternion") {
                return new Quaternion(
                    float.Parse(elem.GetAttribute("x")),
                    float.Parse(elem.GetAttribute("y")),
                    float.Parse(elem.GetAttribute("z")),
                    float.Parse(elem.GetAttribute("w"))
                );
            }

            return 
                Parse(elem["TrileEmplacement"]) ?? 
                Parse(elem["Vector3"]) ?? 
                Parse(elem["Quaternion"]);
        }
    }
}

