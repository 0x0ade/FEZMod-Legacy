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
using Common;
using FezEngine.Effects;
using System.Collections;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using ContentSerialization.Attributes;
using System.CodeDom;
using FezEngine.Content;
using Microsoft.Xna.Framework.Graphics;
using FezEngine.Structure.Geometry;
using System.Globalization;
using System.Threading;

namespace FezGame.Mod {
    public static class XmlHelper {

        //public readonly static Dictionary<string, FieldInfo> CacheFields = new Dictionary<string, FieldInfo>();

        public static List<string> BlacklistedAssemblies = new List<string>() {
            "SDL2-CS", //OpenTK
            "System.Drawing" //Thanks Rectangle!
        };
        public static List<Type> HatedTypesNew = new List<Type>() {
            typeof(VertexPositionNormalTextureInstance) //Thanks parameter Normal being of type Vector3, but being a byte in XML...
        };
        public static List<Type> HatedTypesSpecial = new List<Type>() {
            typeof(AnimatedTexture), //Thanks for Frames requiring to be specially parsed...
            typeof(ArtObject) //Thanks for basically everything requiring to be specially parsed...
        };

        public static object Deserialize(this XmlNode node, Type parent = null, ContentManager cm = null, bool descend = true) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (cm == null) {
                cm = ServiceHelper.Get<IContentManagerProvider>().Global;
            }

            if (node == null) {
                return null;
            }

            if (node is XmlDeclaration) {
                //ModLogger.Log("FEZMod", "XmlHelper found XmlDeclaration; skipping...");
                return node.NextSibling.Deserialize(parent, cm, descend);
            }

            XmlElement elem = node as XmlElement;

            if (node.Name == "Entry") {
                return node.ChildNodes[0].Deserialize(parent, cm, descend);
            }

            Type type = null;

            if (type == null && parent != null) {
                FieldInfo[] fields = parent.GetFields();
                foreach (FieldInfo field in fields) {
                    if (field.Name.ToLower() == node.Name.ToLower()) {
                        type = field.FieldType;
                        foreach (XmlNode child in node.ChildNodes) {
                            if (child.Name.ToLower() == type.Name.ToLower()) {
                                return child.Deserialize(parent, cm, descend);
                            }
                        }
                        break;
                    }
                }
            }

            if (type == null && parent != null && (typeof(IList).IsAssignableFrom(parent) || parent.IsArray)) {
                type = parent.GetElementType();
            }

            if (type == null && parent != null) {
                PropertyInfo[] properties = parent.GetProperties();
                foreach (PropertyInfo property in properties) {
                    if (property.Name.ToLower() == node.Name.ToLower()) {
                        type = property.PropertyType;
                        foreach (XmlNode child in node.ChildNodes) {
                            if (child.Name.ToLower() == type.Name.ToLower()) {
                                return child.Deserialize(parent, cm, descend);
                            }
                        }
                        break;
                    }
                }
            }

            if (type == null) {
                type = node.Name.FindSpecialType(parent);
            }

            if (type == null && descend) {
                foreach (XmlNode child in node.ChildNodes) {
                    //childNode can be a XmlText...
                    object obj_ = child.Deserialize(parent, cm);
                    if (obj_ != null) {
                        return obj_;
                    }
                }
            }

            if (type == null) {
                if (elem != null) {
                    ModLogger.Log("FEZMod", "XmlHelper found no Type for " + node.Name);
                }
                return node.InnerText;
            } else {
                //ModLogger.Log("FEZMod", "elem: " + elem.Name + "; type: " + type.FullName);
                type = Nullable.GetUnderlyingType(type) ?? type;
            }

            object parsed = type.Parse(node.InnerText);

            if (parsed != null) {
                return parsed;
            }

            if (elem != null && elem.HasAttribute("key")) {
                parsed = type.Parse(elem.GetAttribute("key"));

                if (parsed != null) {
                    return parsed;
                }
            }

            object obj = type.New(elem) ?? node.InnerText;

            if (obj is string || HatedTypesNew.Contains(type)) {
                return obj;
            }

            XmlAttributeCollection attribs = node.Attributes;

            foreach (XmlAttribute attrib in attribs) {
                FieldInfo[] fields = type.GetFields();
                foreach (FieldInfo field in fields) {
                    if (field.Name.ToLower() == attrib.Name.ToLower()) {
                        //ModLogger.Log("FEZMod", "field: " + field.Name + "; type: " + field.FieldType.FullName + "; in: " + type.FullName + "; attrib: " + attrib.Name+ "; content: " + attrib.InnerText + "; elem: " + elem.Name);
                        field.SetValue(obj, field.FieldType.Parse(attrib.InnerText));
                        fields = null;
                        break;
                    }
                }
                if (fields == null) {
                    continue;
                }

                PropertyInfo[] properties = type.GetProperties();
                foreach (PropertyInfo property in properties) {
                    if (property.Name.ToLower() == attrib.Name.ToLower()) {
                        MethodInfo setter = property.GetSetMethod();
                        if (setter == null) {
                            ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + attrib.Name + " in " + type.FullName);
                            continue;
                        }
                        //ModLogger.Log("FEZMod", "property: " + property.Name + "; type: " + property.PropertyType.FullName + "; in: " + type.FullName + "; attrib: " + attrib.Name+ "; content: " + attrib.InnerText + "; elem: " + elem.Name);
                        object obj_ = property.PropertyType.Parse(attrib.InnerText);
                        try {
                            setter.Invoke(obj, new object[] { obj_ });
                        } catch (Exception e) {
                            ModLogger.Log("FEZMod", "XmlHelper failed to set property Property " + property.Name + " in " + type.FullName);
                            ModLogger.Log("FEZMod", e.Message);
                            ModLogger.Log("FEZMod", obj_.GetType().FullName);
                        }
                        properties = null;
                        break;
                    }
                }
                if (properties == null) {
                    continue;
                }

            }

            if (obj == null) {
                ModLogger.Log("FEZMod", "XmlHelper couldn't create a new object for " + node.Name + " of type " + type.FullName);
                obj = type.New();
            }

            if (HatedTypesSpecial.Contains(type)) {
                obj.HandleSpecialDataDeserialize(elem, cm);
                return obj;
            }

            bool isGenericICollection = false;
            //TODO find out why HashSets don't trigger the usual obj is ICollection but other types do
            foreach (Type interfaceType in type.GetInterfaces()) {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)) {
                    isGenericICollection = true;
                    break;
                }
            }

            if (obj is ICollection || isGenericICollection) {
                Type[] types = obj.GetType().GetGenericArguments();
                //ModLogger.Log("FEZMod", "XmlHelper got " + type.FullName + " with " + types.Length + " generic arguments.");
                MethodInfo add = type.GetMethod("Add", types);
                if (add != null) {
                    foreach (XmlNode child in node.ChildNodes) {
                        string attribKey = child is XmlElement ? ((XmlElement) child).GetAttribute("key") : null;
                        if (!string.IsNullOrEmpty(attribKey)) {
                            object key = types[0].Parse(attribKey);
                            add.Invoke(obj, new object[] { key, child.Deserialize(parent, cm) });
                        } else if (child.ChildNodes.Count > 1 && child.ChildNodes.Count == types.Length) {
                            add.Invoke(obj, new object[] {
                                child.ChildNodes[0].Deserialize(parent, cm, descend),
                                child.ChildNodes[1].Deserialize(parent, cm, descend)
                            });
                        } else {
                            object obj_ = child.Deserialize(parent, cm, descend);
                            try {
                                add.Invoke(obj, new object[] { obj_ });
                            } catch (Exception e) {
                                ModLogger.Log("FEZMod", "XmlHelper failed to add item in " + type.FullName);
                                ModLogger.Log("FEZMod", e.Message);
                                ModLogger.Log("FEZMod", obj_.GetType().FullName);
                            }
                        }
                    }
                } else if (obj is IList) {
                    IList list = (IList) obj;
                    int i = 0;
                    foreach (XmlNode child in node.ChildNodes) {
                        list[i] = child.Deserialize(type, cm, descend);
                        i++;
                    }
                } else {
                    ModLogger.Log("FEZMod", "XmlHelper could not add entries to " + node.Name + " of type " + type.FullName);
                }
            } else {
                //ModLogger.Log("FEZMod", "elem: "+elem.Name);
                foreach (XmlNode child_ in node.ChildNodes) {
                    //ModLogger.Log("FEZMod", "child: "+child.Name);

                    XmlNode[] children;

                    if (child_.ChildNodes.Count == 1) {
                        children = new XmlNode[] { child_, child_.FirstChild };
                    } else {
                        children = new XmlNode[] { child_ };
                    }

                    foreach (XmlNode child in children) {
                        FieldInfo field_;
                        if ((field_ = type.GetField(child.Name)) != null) {
                            field_.SetValue(obj, child.Deserialize(type, cm));
                            break;
                        }

                        PropertyInfo property_;
                        if ((property_ = type.GetProperty(child.Name)) != null) {
                            MethodInfo setter = property_.GetSetMethod();
                            if (setter == null) {
                                ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + child.Name + " in " + type.FullName);
                                break;
                            }
                            object obj_ = child.Deserialize(type, cm);
                            try {
                                setter.Invoke(obj, new object[] { obj_ });
                            } catch (Exception e) {
                                ModLogger.Log("FEZMod", "XmlHelper failed to set property Property " + child.Name + " in " + type.FullName);
                                ModLogger.Log("FEZMod", e.Message);
                                ModLogger.Log("FEZMod", obj_.GetType().FullName);
                            }
                            break;
                        }
                        
                        Type childType = child.Name.FindSpecialType(type);

                        FieldInfo[] fields = type.GetFields();
                        foreach (FieldInfo field in fields) {
                            if (field.Name.ToLower() == child.Name.ToLower() || field.FieldType.IsAssignableFrom(childType)) {
                                field.SetValue(obj, child.Deserialize(type, cm));
                                fields = null;
                                break;
                            }
                        }
                        if (fields == null) {
                            break;
                        }

                        PropertyInfo[] properties = type.GetProperties();
                        foreach (PropertyInfo property in properties) {
                            if (property.Name.ToLower() == child.Name.ToLower() || property.PropertyType.IsAssignableFrom(childType)) {
                                MethodInfo setter = property.GetSetMethod();
                                if (setter == null) {
                                    ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + child.Name + " in " + type.FullName);
                                    continue;
                                }
                                object obj_ = child.Deserialize(type, cm);
                                try {
                                    setter.Invoke(obj, new object[] { obj_ });
                                } catch (Exception e) {
                                    ModLogger.Log("FEZMod", "XmlHelper failed to set property Property " + child.Name + " in " + type.FullName);
                                    ModLogger.Log("FEZMod", e.Message);
                                    ModLogger.Log("FEZMod", obj_.GetType().FullName);
                                }
                                properties = null;
                                break;
                            }
                        }
                        if (properties == null) {
                            break;
                        }

                        ModLogger.Log("FEZMod", "XmlHelper found no Field or Property named or for type " + child.Name + " for " + type.FullName);
                    }

                }
            }

            obj.HandleSpecialDataDeserialize(elem, cm);

            return obj;
        }

        public static void HandleSpecialDataDeserialize(this object obj, XmlElement elem, ContentManager cm) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (obj == null || elem == null) {
                return;
            }

            if (obj is TrileInstance && !string.IsNullOrEmpty(elem.GetAttribute("orientation"))) {
                ((TrileInstance) obj).SetPhiLight(byte.Parse(elem.GetAttribute("orientation")));
            }

            if (obj is ArtObjectInstance) {
                if (elem.HasAttribute("name")) {
                    ((ArtObjectInstance) obj).ArtObject = cm.Load<ArtObject>("Art objects/" + elem.GetAttribute("name"));
                } else {
                    ((ArtObjectInstance) obj).ArtObject = cm.Load<ArtObject>("Art objects/" + elem.GetAttribute("artobjectname"));
                }
            }

            if (obj is Level) {
                Level levelData = ((Level) obj);
                if (levelData.SkyName != null) {
                    levelData.Sky = cm.Load<Sky>("Skies/" + levelData.SkyName);
                }
                if (levelData.TrileSetName != null) {
                    levelData.TrileSet = cm.Load<TrileSet>("Trile Sets/" + levelData.TrileSetName);
                }
                if (levelData.SongName != null) {
                    levelData.Song = cm.Load<TrackedSong>("Music/" + levelData.SongName);
                    levelData.Song.Initialize();
                }

                //levelData.OnDeserialization();
            }

            if (obj is Entity) {
                ((Entity) obj).Type = ((Entity) obj).Type ?? elem.GetAttribute("entityType");
            }

            if (obj is MapNode) {
                ((MapNode) obj).LevelName = ((MapNode) obj).LevelName ?? elem.GetAttribute("name");
                if (elem.HasAttribute("type")) {
                    ((MapNode) obj).NodeType = (LevelNodeType) Enum.Parse(typeof(LevelNodeType), elem.GetAttribute("type"));
                }
            }

            if (obj is MapNode) {
                ((MapNode) obj).LevelName = ((MapNode) obj).LevelName ?? elem.GetAttribute("name");
                if (elem.HasAttribute("type")) {
                    ((MapNode) obj).NodeType = (LevelNodeType) Enum.Parse(typeof(LevelNodeType), elem.GetAttribute("type"));
                }
            }

            if (obj is WinConditions && elem.HasAttribute("chests")) {
                WinConditions wc = (WinConditions) obj;
                wc.ChestCount = int.Parse(elem.GetAttribute("chests"));
                wc.LockedDoorCount = int.Parse(elem.GetAttribute("lockedDoors"));
                wc.UnlockedDoorCount = int.Parse(elem.GetAttribute("unlockedDoors"));
                wc.CubeShardCount = int.Parse(elem.GetAttribute("cubeShards"));
                wc.SplitUpCount = int.Parse(elem.GetAttribute("splitUp"));
                wc.SecretCount = int.Parse(elem.GetAttribute("secrets"));
                wc.OtherCollectibleCount = int.Parse(elem.GetAttribute("others"));
            }

            if (obj is AnimatedTexture) {
                AnimatedTexture ani = (AnimatedTexture) obj;
                int width = int.Parse(elem.GetAttribute("width"));
                int height = int.Parse(elem.GetAttribute("height"));
                ani.FrameWidth = int.Parse(elem.GetAttribute("actualWidth"));
                ani.FrameHeight = int.Parse(elem.GetAttribute("actualHeight"));
                ani.Texture = cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + ".ani");
                ani.Offsets = new Rectangle[elem.FirstChild.ChildNodes.Count];
                float[] durations = new float[elem.FirstChild.ChildNodes.Count];
                for (int i = 0; i < elem.FirstChild.ChildNodes.Count; i++) {
                    XmlNode child = elem.FirstChild.ChildNodes[i];
                    ani.Offsets[i] = (Rectangle) child.FirstChild.Deserialize(null, cm, false);
                    durations[i] = (float) new TimeSpan(long.Parse(((XmlElement) child).GetAttribute("duration"))).TotalSeconds;
                }
                ani.Timing = new AnimationTiming(0, durations.Length - 1, durations);
                ani.PotOffset = new Vector2((float) (FezMath.NextPowerOfTwo((double) ani.FrameWidth) - ani.FrameWidth), (float) (FezMath.NextPowerOfTwo((double) ani.FrameHeight) - ani.FrameHeight));
            }

            if (obj is ArtObject) {
                ArtObject ao = (ArtObject) obj;
                ao.Name = elem.GetAttribute("name");
                ao.Cubemap = cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + "-fm-Texture2D").MixAlpha(cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + "_alpha"));
                ao.Size = (Vector3) elem.ChildNodes[0].FirstChild.Deserialize();
                XmlNode geometryNode = null;
                foreach (XmlNode childNode in elem.ChildNodes) {
                    if (childNode.Name == "ShaderInstancedIndexedPrimitives") {
                        geometryNode = childNode;
                        break;
                    }
                }
                if (geometryNode != null) {
                    XmlElement geometryElem = (XmlElement) geometryNode;
                    //PrimitiveType type = (PrimitiveType) typeof(PrimitiveType).Parse(geometryElem.GetAttribute("type"));
                    //Let's just assume all art objects use TriangleLists.
                    //Note the suffix s in TriangleLists...
                    ao.Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>(PrimitiveType.TriangleList, 60);
                    ao.Geometry.NeedsEffectCommit = true;
                    ao.Geometry.Vertices = (VertexPositionNormalTextureInstance[]) geometryElem.ChildNodes[0].Deserialize(typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>), cm);
                    ao.Geometry.Indices = (int[]) geometryElem.ChildNodes[1].Deserialize(typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>), cm);
                }
                ao.ActorType = (ActorType) typeof(ActorType).Parse(elem.GetAttribute("actorType"));
                ao.NoSihouette = bool.Parse(elem.GetAttribute("noSilhouette"));
            }

            MethodInfo onDeserialization = obj.GetType().GetMethod("OnDeserialization");
            if (onDeserialization != null) {
                if (onDeserialization.GetParameters().Length == 0) {
                    onDeserialization.Invoke(obj, new object[0]);
                } else {
                    //ModLogger.Log("FEZMod", "XmlHelper can't call OnDeserialization on " + obj + " of type " + obj.GetType().FullName + " because it requires parameters. XmlHelper can't pass parameters.");
                }
            }
        }

        public static Type FindType(this string name) {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> delayedAssemblies = new List<Assembly>();

            foreach (Assembly assembly in assemblies) {
                if (assembly.GetName().Name.EndsWith(".mm") || BlacklistedAssemblies.Contains(assembly.GetName().Name)) {
                    delayedAssemblies.Add(assembly);
                    continue;
                }
                try {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types) {
                        if (type.Name == name && type.FullName.EndsWith("."+name)) {
                            return type;
                        }
                    }
                } catch (ReflectionTypeLoadException e) {
                    ModLogger.Log("FEZMod", "Failed searching a type in XmlHelper's FindType.");
                    ModLogger.Log("FEZMod", "Assembly: " + assembly.GetName().Name);
                    ModLogger.Log("FEZMod", e.Message);
                    foreach (Exception le in e.LoaderExceptions) {
                        ModLogger.Log("FEZMod", le.Message);
                    }
                }
            }

            foreach (Assembly assembly in delayedAssemblies) {
                try {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types) {
                        if (type.Name == name && type.FullName.EndsWith("."+name)) {
                            return type;
                        }
                    }
                } catch (ReflectionTypeLoadException e) {
                    ModLogger.Log("FEZMod", "Failed searching a type in XmlHelper's FindType.");
                    ModLogger.Log("FEZMod", "Assembly: " + assembly.GetName().Name);
                    ModLogger.Log("FEZMod", e.Message);
                    foreach (Exception le in e.LoaderExceptions) {
                        ModLogger.Log("FEZMod", le.Message);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a type based on its special (xnb_parse) name, falling back to FindType otherwise.
        /// </summary>
        /// <returns>The type found.</returns>
        /// <param name="name">Name of the type to find. Usually the name of the node.</param>
        /// <param name="parent">Parent node's type.</param>
        public static Type FindSpecialType(this string name, Type parent) {
            if (string.IsNullOrEmpty(name)) {
                return name.FindType();
            }

            //Use the following logging method call to specify the conditions for a special type.
            //ModLogger.Log("FEZMod", "XmlHelper FindSpecialType debug name: " + name + "; parent: " + parent.FullName);

            if (typeof(NpcInstance).IsAssignableFrom(parent) && name == "Action") {
                return null;
            }

            if ((typeof(MapTree).IsAssignableFrom(parent) || typeof(MapNode).IsAssignableFrom(parent)) && name == "Node") {
                return typeof(MapNode);
            }

            if (typeof(MapNode).IsAssignableFrom(parent) && name == "Connection") {
                return typeof(MapNode.Connection);
            }

            if (typeof(WinConditions).IsAssignableFrom(parent) && name == "Scripts") {
                return typeof(List<int>);
            }

            if (typeof(WinConditions).IsAssignableFrom(parent) && name == "Script") {
                return typeof(int);
            }

            if (parent == null && name == "AnimatedTexturePC") {
                return typeof(AnimatedTexture);
            }

            if (typeof(AnimatedTexture).IsAssignableFrom(parent) && name == "FramePC") {
                return typeof(FrameContent);
            }

            if (typeof(ArtObject).IsAssignableFrom(parent) && name == "ShaderInstancedIndexedPrimitives") {
                return typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>);
            }

            return name.FindType();
        }

        public static object New(this Type type, XmlElement elem = null) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (type.IsArray) {
                if (elem != null) {
                    //ModLogger.Log("FEZMod", "XmlHelper creates an array of exact type " + type.FullName);
                    //ModLogger.Log("FEZMod", "XmlHelper creates an array of element type " + type.GetElementType().FullName);
                    return Array.CreateInstance(type.GetElementType(), elem.ChildNodes.Count);
                } else {
                    ModLogger.Log("FEZMod", "XmlHelper can't create an array of exact type " + type.FullName + " with no length");
                    return null;
                }
            }

            if (type == typeof(string)) {
                return elem.InnerText;
            }

            if (elem == null) {
                ConstructorInfo constructor = type.GetDefaultConstructor();
                if (constructor != null) {
                    return constructor.Invoke(new object[0]);
                } else {
                    ModLogger.Log("FEZMod", "XmlHelper can't find a default constructor for null element of type " + type.FullName);
                    return null;
                }
            }

            XmlAttributeCollection attribs = elem.Attributes;

            ConstructorInfo[] constructors = type.GetConstructors();
            foreach (ConstructorInfo constructor in constructors) {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (!elem.HasAttributes && parameters.Length == 0) {
                    return constructor.Invoke(new object[0]);
                } else if (!elem.HasAttributes || parameters.Length == 0) {
                    continue;
                }
                if (attribs.Count != parameters.Length) {
                    continue;
                }
                object[] objs = new object[parameters.Length];
                int i = 0;
                foreach (ParameterInfo parameter in parameters) {
                    //ModLogger.Log("FEZMod", "parameter: " + parameter.Name + "; type: " + parameter.ParameterType.FullName + "; in: " + type.FullName + "; attrib: " + attribs[i].Name+ "; content: " + attribs[i].InnerText + "; elem: " + elem.Name);
                    objs[i] = parameter.ParameterType.Parse(attribs[i].InnerText);
                    if (objs[i] == null) {
                        //TODO the result of parsing the string can be null...
                        break;
                    }
                    i++;
                }
                if (i < objs.Length) {
                    continue;
                }

                return constructor.Invoke(objs);
            }

            ConstructorInfo constructor_ = type.GetDefaultConstructor();
            if (constructor_ != null) {
                return constructor_.Invoke(new object[0]);
            }

            if (type == typeof(VertexPositionNormalTextureInstance)) {
                //TODO make this method automatically pass the child nodes when needed
                return new VertexPositionNormalTextureInstance(
                    (Vector3) elem.ChildNodes[0].FirstChild.Deserialize(),
                    byte.Parse(elem.ChildNodes[1].InnerText),
                    (Vector2) elem.ChildNodes[2].FirstChild.Deserialize()
                );
            }

            ModLogger.Log("FEZMod", "XmlHelper can't find a constructor for element " + elem.Name + " of type " + type.FullName);
            return null;
        }

        public static object Parse(this Type type, string str) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (type == null || string.IsNullOrEmpty(str)) {
                return null;
            }
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(string)) {
                return str;
            }
            if (type.IsEnum) {
                return Enum.Parse(type, str);
            }
            if (type.IsPrimitive) {
                return type.GetMethod("Parse", new Type[] { typeof(String) }).Invoke(null, new object[] { str });
            }
            if (type == typeof(Color)) {
                Color color = new Color();
                color.PackedValue = Convert.ToUInt32(str.Substring(1), 16);
                return color;
            }
            if (type.IsValueType) {
                //ModLogger.Log("FEZMod", "XmlHelper can't parse ValueType " + type.FullName + " from the following data: " + str);
                //can happen if a TrileInstance gets created and the "position" parameter in the constructor gets parsed (Vector3)
                return null;
            }
            //ModLogger.Log("FEZMod", "XmlHelper can't parse " + type.FullName + " from the following data: " + str);
            //Happens "normally" as Deserialize tries to parse the string first.
            return null;
        }

        public static XmlNode Serialize(this object obj, XmlDocument document, string name = null) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (obj == null || document == null) {
                return null;
            }
            Type type = obj.GetType();
            if (name == null) {
                name = type.Name;
            }

            XmlElement elem = document.CreateElement(name);

            if (type.IsEnum || type.IsPrimitive || obj is string) {
                elem.InnerText = obj.ToString();
                return elem;
            }

            elem = elem.HandleSpecialDataPreSerialize(obj);
            if (elem == null) {
                return null;
            }

            bool isGenericICollection = false;
            //TODO find out why HashSets don't trigger the usual obj is ICollection but other types do
            foreach (Type interfaceType in type.GetInterfaces()) {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)) {
                    isGenericICollection = true;
                    break;
                }
            }

            if (obj is ICollection || isGenericICollection) {
                //TODO handle collections
                Type[] types = obj.GetType().GetGenericArguments();
                //ModLogger.Log("FEZMod", "XmlHelper got " + type.FullName + " with " + types.Length + " generic arguments.");
                if (obj is IList) {
                    IList list = (IList) obj;
                    for (int i = 0; i < list.Count; i++) {
                        elem.AppendChildIfNotNull(list[i].Serialize(document));
                    }
                } else if (obj is ICollection) {
                    ICollection collection = (ICollection) obj;
                    foreach (object item in collection) {
                        XmlElement entry = document.CreateElement("Entry");
                        Type itemType = item.GetType();
                        bool append = true;
                        if (itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                            object key = itemType.GetProperty("Key").GetGetMethod().Invoke(item, new object[0]);
                            object value = itemType.GetProperty("Value").GetGetMethod().Invoke(item, new object[0]);
                            Type keyType = key.GetType();
                            if (keyType.IsEnum ||
                                keyType.IsPrimitive ||
                                typeof(string).IsAssignableFrom(keyType)
                            ) {
                                entry.SetAttribute("key", key.ToString());
                            } else {
                                append &= null != entry.AppendChildIfNotNull(key.Serialize(document));
                            }
                            append &= null != entry.AppendChildIfNotNull(value.Serialize(document));
                        } else {
                            append &= null != entry.AppendChildIfNotNull(item.Serialize(document));
                        }
                        if (append) {
                            elem.AppendChild(entry);
                        }
                    }
                } else if (isGenericICollection) {
                    MethodInfo getItem = type.GetMethod("get_Item");
                    PropertyInfo propertyKeys = type.GetProperty("Keys");
                    if (propertyKeys != null) {
                        IEnumerable keys = (IEnumerable) propertyKeys.GetGetMethod().Invoke(obj, new object[0]);
                        foreach (object key in keys) {
                            XmlElement entry = document.CreateElement("Entry");
                            Type keyType = key.GetType();
                            bool append = true;
                            if (keyType.IsEnum ||
                                keyType.IsPrimitive ||
                                typeof(string).IsAssignableFrom(keyType)
                            ) {
                                entry.SetAttribute("key", key.ToString());
                            } else {
                                append &= null != entry.AppendChildIfNotNull(key.Serialize(document));
                            }
                            append &= null != entry.AppendChildIfNotNull(getItem.Invoke(obj, new object[] { key }).Serialize(document));
                            if (append) {
                                elem.AppendChild(entry);
                            }
                        }
                    } else {
                        PropertyInfo propertyCount = type.GetProperty("Count");
                        int count = (int) propertyCount.GetGetMethod().Invoke(obj, new object[0]);
                        if (getItem != null) {
                            for (int i = 0; i < count; i++) {
                                elem.AppendChildIfNotNull(getItem.Invoke(obj, new object[] { i }).Serialize(document));
                            }
                        } else {
                            //Selfnote from Maik: I usually hate Linq, but meh.
                            MethodInfo elementAt = typeof(System.Linq.Enumerable).GetMethod("ElementAt").MakeGenericMethod(types[0]);
                            for (int i = 0; i < count; i++) {
                                elem.AppendChildIfNotNull(elementAt.Invoke(null, new object[] { obj, i }).Serialize(document));
                            }
                        }
                    }
                } else {
                    ModLogger.Log("FEZMod", "XmlHelper could not get entries from " + elem.Name + " of type " + type.FullName);
                }
                return elem;
            }

            //TODO handle further special types

            //TODO what about this piece of code? (early IKVM port leftover)
            MethodInfo[] methods = type.GetMethods();
            for (int i = 0; i < methods.Length; i++) {
                MethodInfo method = methods[i];
                string methodLowerCase = method.Name.ToLower();
                if (!methodLowerCase.StartsWith("get_") || method.IsStatic || type.GetMethod("s" + method.Name.Substring(1)) == null) {
                    continue;
                }

                Type returnType = method.ReturnType;

                if (!(
                    returnType.IsEnum ||
                    returnType.IsPrimitive ||
                    typeof(string).IsAssignableFrom(returnType)
                )) {
                    continue;
                }

                string attributeName = methodLowerCase.Substring(4);
                object value = method.Invoke(obj, new object[0]);
                if (value != null) {
                    elem.SetAttribute(attributeName, value.ToString());
                }
            }

            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++) {
                FieldInfo field = fields[i];
                if (field.IsInitOnly ||
                    field.IsNotSerialized ||
                    field.IsSpecialName ||
                    field.IsStatic) {
                    continue;
                }
                object[] attribs = field.GetCustomAttributes(typeof(SerializationAttribute), true);
                if (attribs.Length > 0 && ((SerializationAttribute) attribs[0]).Ignore) {
                    continue;
                }
                Type fieldType = field.FieldType;
                if (fieldType.IsEnum ||
                    fieldType.IsPrimitive ||
                    typeof(string).IsAssignableFrom(fieldType)) {
                    elem.SetAttribute(field.Name, field.GetValue(obj).ToString());
                } else {
                    elem.AppendChildIfNotNull(field.GetValue(obj).Serialize(document, field.Name));
                }
            }

            PropertyInfo[] properties = type.GetProperties();
            for (int i = 0; i < properties.Length; i++) {
                PropertyInfo property = properties[i];
                Type propertyType = property.PropertyType;
                if (propertyType.IsEnum ||
                    propertyType.IsPrimitive ||
                    typeof(string).IsAssignableFrom(propertyType)
                ) {
                    continue;
                }
                //ModLogger.Log("FEZMod", "elem: " + name + "; type: " + type.FullName + "; property: " + property.Name + "; propertyType: " + propertyType.FullName);
                MethodInfo getter = property.GetGetMethod();
                if (getter == null || getter.IsPrivate || getter.IsStatic) {
                    continue;
                }
                MethodInfo setter = property.GetSetMethod();
                if (setter == null || setter.IsPrivate) {
                    continue;
                }
                object[] attribs = property.GetCustomAttributes(typeof(SerializationAttribute), true);
                if (attribs.Length > 0 && ((SerializationAttribute) attribs[0]).Ignore) {
                    continue;
                }
                elem.AppendChildIfNotNull(getter.Invoke(obj, new object[0]).Serialize(document, property.Name));
            }

            elem = elem.HandleSpecialDataPostSerialize(obj);
            if (elem == null) {
                return null;
            }

            return elem;
        }

        public static XmlElement HandleSpecialDataPreSerialize(this XmlElement elem, object obj) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (obj == null || elem == null) {
                return elem;
            }

            if (obj is BackgroundPlane && string.IsNullOrEmpty(((BackgroundPlane) obj).TextureName)) {
                return null;
            }

            return elem;
        }

        public static XmlElement HandleSpecialDataPostSerialize(this XmlElement elem, object obj) {
            if (FEZMod.OverrideCulturueManualyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (obj == null || elem == null) {
                return elem;
            }

            if (obj is Color) {
                elem.RemoveAttribute("packedvalue");
            }

            return elem;
        }

        public static XmlNode AppendChildIfNotNull(this XmlNode parent, XmlNode child) {
            if (child == null) {
                return null;
            }
            return parent.AppendChild(child);
        }

    }
}

