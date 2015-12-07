using System;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.Collections;
using ContentSerialization.Attributes;
using FezEngine.Content;
using Microsoft.Xna.Framework.Graphics;
using FezEngine.Structure.Geometry;
using System.Globalization;
using System.Threading;
using Common;

namespace FezEngine.Mod {
    public struct CacheKey_Type_NodeName {
        public Type Type;
        public string NodeName;
    }
    public struct CacheKey_NodeName_AttribName {
        public string NodeName;
        public string AttribName;
    }

    public static class XmlHelper {

        private readonly static Dictionary<string, Type> CacheTypes = new Dictionary<string, Type>(128);
        private readonly static Dictionary<CacheKey_Type_NodeName, Type> CacheTypesSpecial = new Dictionary<CacheKey_Type_NodeName, Type>(32);
        private readonly static Dictionary<Type, FieldInfo[]> CacheTypesFields = new Dictionary<Type, FieldInfo[]>(128);
        private readonly static Dictionary<Type, PropertyInfo[]> CacheTypesProperties = new Dictionary<Type, PropertyInfo[]>(128);
        private readonly static Dictionary<CacheKey_Type_NodeName, FieldInfo> CacheFields = new Dictionary<CacheKey_Type_NodeName, FieldInfo>(256);
        private readonly static Dictionary<CacheKey_Type_NodeName, PropertyInfo> CacheProperties = new Dictionary<CacheKey_Type_NodeName, PropertyInfo>(256);
        private readonly static Dictionary<CacheKey_NodeName_AttribName, FieldInfo> CacheAttribFields = new Dictionary<CacheKey_NodeName_AttribName, FieldInfo>(256);
        private readonly static Dictionary<CacheKey_NodeName_AttribName, PropertyInfo> CacheAttribProperties = new Dictionary<CacheKey_NodeName_AttribName, PropertyInfo>(256);
        private readonly static Dictionary<CacheKey_Type_NodeName, DynamicMethodDelegate> CacheConstructors = new Dictionary<CacheKey_Type_NodeName, DynamicMethodDelegate>(128);
        private readonly static Dictionary<DynamicMethodDelegate, ParameterInfo[]> CacheConstructorsParameters = new Dictionary<DynamicMethodDelegate, ParameterInfo[]>(128);
        private readonly static Dictionary<Type, MethodInfo> CacheAddMethods = new Dictionary<Type, MethodInfo>(64);
        private readonly static Dictionary<Type, MethodInfo> CacheParseMethods = new Dictionary<Type, MethodInfo>(32);


        public static List<string> BlacklistedAssemblies = new List<string>() {
            "SDL2-CS", //OpenTK
            "System.Drawing" //Thanks Rectangle!
        };
        public static List<Type> HatedTypesNew = new List<Type>() {
            typeof(VertexPositionNormalTextureInstance), //Thanks parameter Normal being of type Vector3, but being a byte in XML...
            typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>), //Thanks for your children causing typecast exceptions...
            typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>) //Also for your children for not knowing where to be assigned to...
        };
        public static List<Type> HatedTypesSpecial = new List<Type>() {
            typeof(AnimatedTexture), //Thanks for Frames requiring to be specially parsed...
            typeof(ArtObject) //Thanks for basically everything requiring to be specially parsed...
        };

        private static CacheKey_Type_NodeName deserialize_key;
        private static CacheKey_NodeName_AttribName deserialize_key_attrib;
        public static object Deserialize(this XmlNode node, Type parent = null, ContentManager cm = null, bool descend = true) {
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (cm == null) {
                cm = ServiceHelper.Get<IContentManagerProvider>().Global;
            }

            if (node == null) {
                return null;
            }

            if (node is XmlDeclaration) {
                return node.NextSibling.Deserialize(parent, cm, descend);
            }

            XmlElement elem = node as XmlElement;

            if (node.Name == "Entry") {
                return node.ChildNodes[0].Deserialize(parent, cm, descend);
            }

            Type type = null;

            if (type == null && parent != null) {
                FieldInfo field_;
                deserialize_key.Type = parent;
                deserialize_key.NodeName = node.Name.ToLower();
                if (CacheFields.TryGetValue(deserialize_key, out field_)) {
                    type = field_.FieldType;
                }
                if (type == null) {
                    FieldInfo[] fields;
                    if (!CacheTypesFields.TryGetValue(parent, out fields)) {
                        CacheTypesFields[parent] = fields = parent.GetFields();
                    }
                    string nodeName = node.Name.ToLower();
                    foreach (FieldInfo field in fields) {
                        if (field.Name.ToLower() == nodeName) {
                            CacheFields[deserialize_key] = field;
                            type = field.FieldType;
                            break;
                        }
                    }
                }
                if (type != null) {
                    string typeName = type.Name.ToLower();
                    foreach (XmlNode child in node.ChildNodes) {
                        if (child.Name.ToLower() == typeName) {
                            return child.Deserialize(parent, cm, descend);
                        }
                    }
                }
            }

            if (type == null && parent != null && (typeof(IList).IsAssignableFrom(parent) || parent.IsArray)) {
                type = parent.GetElementType();
            }

            if (type == null && parent != null) {
                PropertyInfo property_;
                deserialize_key.Type = parent;
                deserialize_key.NodeName = node.Name.ToLower();
                if (CacheProperties.TryGetValue(deserialize_key, out property_)) {
                    type = property_.PropertyType;
                }
                if (type == null) {
                    PropertyInfo[] properties;
                    if (!CacheTypesProperties.TryGetValue(parent, out properties)) {
                        CacheTypesProperties[parent] = properties = parent.GetProperties();
                    }
                    string nodeName = node.Name.ToLower();
                    foreach (PropertyInfo property in properties) {
                        if (property.Name.ToLower() == node.Name.ToLower()) {
                            CacheProperties[deserialize_key] = property;
                            type = property.PropertyType;
                            break;
                        }
                    }
                }
                if (type != null) {
                    string typeName = type.Name.ToLower();
                    foreach (XmlNode child in node.ChildNodes) {
                        if (child.Name.ToLower() == typeName) {
                            return child.Deserialize(parent, cm, descend);
                        }
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

            if (obj is string) {
                return obj;
            }

            XmlAttributeCollection attribs = node.Attributes;

            foreach (XmlAttribute attrib in attribs) {
                deserialize_key_attrib.NodeName = node.Name;
                deserialize_key_attrib.AttribName = attrib.Name.ToLower();

                FieldInfo field_;
                if (CacheAttribFields.TryGetValue(deserialize_key_attrib, out field_)) {
                    ReflectionHelper.SetValue(field_, obj, field_.FieldType.Parse(attrib.InnerText));
                    continue;
                }

                PropertyInfo property_;
                if (CacheAttribProperties.TryGetValue(deserialize_key_attrib, out property_)) {
                    ReflectionHelper.SetValue(property_, obj, property_.PropertyType.Parse(attrib.InnerText));
                    continue;
                }

                FieldInfo[] fields;
                if (!CacheTypesFields.TryGetValue(type, out fields)) {
                    CacheTypesFields[type] = fields = type.GetFields();
                }
                foreach (FieldInfo field in fields) {
                    if (field.Name.ToLower() == deserialize_key_attrib.AttribName) {
                        CacheAttribFields[deserialize_key_attrib] = field;
                        ReflectionHelper.SetValue(field, obj, field.FieldType.Parse(attrib.InnerText));
                        fields = null;
                        break;
                    }
                }
                if (fields == null) {
                    continue;
                }

                PropertyInfo[] properties;
                if (!CacheTypesProperties.TryGetValue(type, out properties)) {
                    CacheTypesProperties[type] = properties = type.GetProperties();
                }
                foreach (PropertyInfo property in properties) {
                    if (property.Name.ToLower() == attrib.Name.ToLower()) {
                        if (!property.CanWrite) {
                            ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + attrib.Name + " in " + type.FullName);
                            continue;
                        }
                        CacheAttribProperties[deserialize_key_attrib] = property;
                        ReflectionHelper.SetValue(property, obj, property.PropertyType.Parse(attrib.InnerText));
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

            if (HatedTypesNew.Contains(type)) {
                return obj;
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
                MethodInfo add;
                Type[] types = null;
                if (!CacheAddMethods.TryGetValue(type, out add)) {
                    types = type.GetGenericArguments();
                    CacheAddMethods[type] = add = type.GetMethod("Add", types);
                }
                if (add != null) {
                    if (types == null) {
                        types = type.GetGenericArguments();
                    }
                    foreach (XmlNode child in node.ChildNodes) {
                        string attribKey = child is XmlElement ? ((XmlElement) child).GetAttribute("key") : null;
                        if (!string.IsNullOrEmpty(attribKey)) {
                            object key = types[0].Parse(attribKey);
                            ReflectionHelper.InvokeMethod(add, obj, ArrayObject(key, child.Deserialize(parent, cm)));
                        } else if (child.ChildNodes.Count > 1 && child.ChildNodes.Count == types.Length) {
                            ReflectionHelper.InvokeMethod(add, obj, ArrayObject(
                                child.ChildNodes[0].Deserialize(parent, cm, descend),
                                child.ChildNodes[1].Deserialize(parent, cm, descend)
                            ));
                        } else {
                            object obj_ = child.Deserialize(parent, cm, descend);
                            try {
                                ReflectionHelper.InvokeMethod(add, obj, ArrayObject(obj_));
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
                        children = ArrayXmlNode(child_, child_.FirstChild);
                    } else {
                        children = ArrayXmlNode(child_);
                    }

                    foreach (XmlNode child in children) {
                        deserialize_key.Type = type;
                        deserialize_key.NodeName = child.Name.ToLower();

                        FieldInfo field_;
                        if (CacheFields.TryGetValue(deserialize_key, out field_)) {
                            ReflectionHelper.SetValue(field_, obj, child.Deserialize(type, cm));
                            continue;
                        }

                        PropertyInfo property_;
                        if (CacheProperties.TryGetValue(deserialize_key, out property_)) {
                            ReflectionHelper.SetValue(property_, obj, child.Deserialize(type, cm));
                            continue;
                        }

                        if ((field_ = type.GetField(child.Name)) != null) {
                            CacheFields[deserialize_key] = field_;
                            ReflectionHelper.SetValue(field_, obj, child.Deserialize(type, cm));
                            break;
                        }

                        if ((property_ = type.GetProperty(child.Name)) != null) {
                            if (!property_.CanWrite) {
                                ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + child.Name + " in " + type.FullName);
                                break;
                            }
                            CacheProperties[deserialize_key] = property_;
                            ReflectionHelper.SetValue(property_, obj, child.Deserialize(type, cm));
                            break;
                        }
                        
                        Type childType = child.Name.FindSpecialType(type);

                        FieldInfo[] fields;
                        if (!CacheTypesFields.TryGetValue(type, out fields)) {
                            CacheTypesFields[type] = fields = type.GetFields();
                        }
                        foreach (FieldInfo field in fields) {
                            if (field.Name.ToLower() == deserialize_key.NodeName || field.FieldType.IsAssignableFrom(childType)) {
                                CacheFields[deserialize_key] = field;
                                ReflectionHelper.SetValue(field, obj, child.Deserialize(type, cm));
                                fields = null;
                                break;
                            }
                        }
                        if (fields == null) {
                            break;
                        }

                        PropertyInfo[] properties = type.GetProperties();
                        foreach (PropertyInfo property in properties) {
                            if (property.Name.ToLower() == deserialize_key.NodeName || property.PropertyType.IsAssignableFrom(childType)) {
                                if (!property.CanWrite) {
                                    ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + child.Name + " in " + type.FullName);
                                    continue;
                                }
                                CacheProperties[deserialize_key] = property;
                                ReflectionHelper.SetValue(property, obj, child.Deserialize(type, cm));
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
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
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
                //ModLogger.Log("FEZMod", "Deserializing the inner pieces of ArtObject...");
                //DateTime timeStart = DateTime.UtcNow;
                ArtObject ao = (ArtObject) obj;
                ao.Name = elem.GetAttribute("name");
                ao.Cubemap = cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + "-fm-Texture2D").MixAlpha(cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + "_alpha"));
                ao.Size = (Vector3) elem.ChildNodes[0].FirstChild.Deserialize();
                foreach (XmlNode childNode in elem.ChildNodes) {
                    if (childNode.Name == "ShaderInstancedIndexedPrimitives") {
                        //FIXME FEZENGINE MIGRATION
                        //FezEngine/Mod/XmlHelper.cs(502,129): error CS0029: Cannot implicitly convert type `FezEngine.Structure.Geometry.ShaderInstancedIndexedPrimitives<FezEngine.Structure.Geometry.VertexPositionNormalTextureInstance,Microsoft.Xna.Framework.Matrix> [FezEngine.Mod.mm, Version=1.0.5819.39244, Culture=neutral, PublicKeyToken=null]' to `FezEngine.Structure.Geometry.ShaderInstancedIndexedPrimitives<FezEngine.Structure.Geometry.VertexPositionNormalTextureInstance,Microsoft.Xna.Framework.Matrix> [FezEngine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]'
                        //ao.Geometry = (ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>) childNode.Deserialize(typeof(ArtObject));
                        break;
                    }
                }
                ao.ActorType = (ActorType) typeof(ActorType).Parse(elem.GetAttribute("actorType"));
                ao.NoSihouette = bool.Parse(elem.GetAttribute("noSilhouette"));
                //DateTime timeEnd = DateTime.UtcNow;
                //ModLogger.Log("FEZMod", "Deserialized the inner pieces of ArtObject in " + (timeEnd - timeStart).TotalMilliseconds + "ms");
            }

            if (obj is TrileSet) {
                ((TrileSet) obj).TextureAtlas = cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + "-fm-Texture2D").MixAlpha(cm.Load<Texture2D>(elem.OwnerDocument.DocumentElement.GetAttribute("assetName") + "_alpha"));
            }

            MethodInfo onDeserialization = obj.GetType().GetMethod("OnDeserialization");
            if (onDeserialization != null) {
                if (onDeserialization.GetParameters().Length == 0) {
                    ReflectionHelper.InvokeMethod(onDeserialization, obj, ArrayObject());
                } else {
                    //ModLogger.Log("FEZMod", "XmlHelper can't call OnDeserialization on " + obj + " of type " + obj.GetType().FullName + " because it requires parameters. XmlHelper can't pass parameters.");
                }
            }
        }

        public static Type FindType(this string name) {
            Type type_ = null;
            if (CacheTypes.TryGetValue(name, out type_)) {
                return type_;
            }

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
                            CacheTypes[name] = type;
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
                            CacheTypes[name] = type;
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

            CacheTypes[name] = null;
            return null;
        }

        private static CacheKey_Type_NodeName findSpecialType_key;
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

            Type type_ = null;
            findSpecialType_key.Type = parent;
            findSpecialType_key.NodeName = name;
            if (CacheTypesSpecial.TryGetValue(findSpecialType_key, out type_)) {
                return type_;
            }

            //Use the following logging method call to specify the conditions for a special type.
            //ModLogger.Log("FEZMod", "XmlHelper FindSpecialType debug name: " + name + "; parent: " + (parent == null ? "null" : parent.FullName));

            if (typeof(NpcInstance).IsAssignableFrom(parent) && name == "Action") {
                return CacheTypesSpecial[findSpecialType_key] = null;
            }

            if ((typeof(MapTree).IsAssignableFrom(parent) || typeof(MapNode).IsAssignableFrom(parent)) && name == "Node") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(MapNode);
            }

            if (typeof(MapNode).IsAssignableFrom(parent) && name == "Connection") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(MapNode.Connection);
            }

            if (typeof(WinConditions).IsAssignableFrom(parent) && name == "Scripts") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(List<int>);
            }

            if (typeof(WinConditions).IsAssignableFrom(parent) && name == "Script") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(int);
            }

            if (parent == null && name == "AnimatedTexturePC") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(AnimatedTexture);
            }

            if (typeof(AnimatedTexture).IsAssignableFrom(parent) && name == "FramePC") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(FrameContent);
            }

            if (typeof(ArtObject).IsAssignableFrom(parent) && name == "ShaderInstancedIndexedPrimitives") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>);
            }

            if (typeof(Trile).IsAssignableFrom(parent) && name == "ShaderInstancedIndexedPrimitives") {
                return CacheTypesSpecial[findSpecialType_key] = typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>);
            }

            return name.FindType();
        }

        private static CacheKey_Type_NodeName new_key;
        public static object New(this Type type, XmlElement elem = null) {
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            if (type == typeof(VertexPositionNormalTextureInstance)) {
                //TODO make this method automatically pass the child nodes when needed
                return new VertexPositionNormalTextureInstance(
                    (Vector3) elem.ChildNodes[0].FirstChild.Deserialize(),
                    byte.Parse(elem.ChildNodes[1].InnerText),
                    (Vector2) elem.ChildNodes[2].FirstChild.Deserialize()
                );
            }

            if (type == typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>)) {
                //PrimitiveType type = (PrimitiveType) typeof(PrimitiveType).Parse(geometryElem.GetAttribute("type"));
                //Let's just assume all art objects use TriangleLists.
                //Note the suffix s in TriangleLists...
                ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>(PrimitiveType.TriangleList, 60);
                XmlNode geometryNode = elem.Name == "Geometry" ? elem.FirstChild : elem;
                #if !FNA
                geometry.NeedsEffectCommit = true;
                #endif
                geometry.Vertices = (VertexPositionNormalTextureInstance[]) geometryNode.ChildNodes[0].Deserialize(typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>));
                geometry.Indices = (int[]) geometryNode.ChildNodes[1].Deserialize(typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix>));
                return geometry;
            }

            if (type == typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>)) {
                //PrimitiveType type = (PrimitiveType) typeof(PrimitiveType).Parse(geometryElem.GetAttribute("type"));
                //Let's just assume all trile sets use TriangleLists.
                //Note the suffix s in TriangleLists...
                ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = new ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>(PrimitiveType.TriangleList, 220);
                XmlNode geometryNode = elem.Name == "Geometry" ? elem.FirstChild : elem;
                #if !FNA
                geometry.NeedsEffectCommit = true;
                #endif
                geometry.Vertices = (VertexPositionNormalTextureInstance[]) geometryNode.ChildNodes[0].Deserialize(typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>));
                geometry.Indices = (int[]) geometryNode.ChildNodes[1].Deserialize(typeof(ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4>));
                return geometry;
            }

            DynamicMethodDelegate constructor_ = null;
            new_key.Type = type;
            new_key.NodeName = elem.Name;
            if (CacheConstructors.TryGetValue(new_key, out constructor_)) {
                ParameterInfo[] parameters = CacheConstructorsParameters[constructor_];
                if (parameters == null || parameters.Length == 0) {
                    return constructor_(null, ArrayObject());
                }

                XmlAttributeCollection attribs_ = elem.Attributes;

                object[] objs = ArrayObjectOfLen(parameters.Length);
                int i = 0;
                foreach (ParameterInfo parameter in parameters) {
                    objs[i] = parameter.ParameterType.Parse(attribs_[i].InnerText);
                    if (objs[i] == null) {
                        //TODO the result of parsing the string can be null...
                        break;
                    }
                    i++;
                }
                if (i >= objs.Length) {
                    return constructor_(null, objs);
                }
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
                ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
                if (constructor != null) {
                    return constructor.Invoke(ArrayObject());
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
                    CacheConstructors[new_key] = constructor_ = ReflectionHelper.CreateDelegate(constructor);
                    CacheConstructorsParameters[constructor_] = null;
                    return constructor_(null, ArrayObject());
                } else if (!elem.HasAttributes || parameters.Length == 0) {
                    continue;
                }
                if (attribs.Count != parameters.Length) {
                    continue;
                }
                object[] objs = ArrayObjectOfLen(parameters.Length);
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

                CacheConstructors[new_key] = constructor_ = ReflectionHelper.CreateDelegate(constructor);
                CacheConstructorsParameters[constructor_] = parameters;
                return constructor_(null, objs);
            }

            ConstructorInfo constructorDefault = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
            if (constructorDefault != null) {
                CacheConstructors[new_key] = constructor_ = ReflectionHelper.CreateDelegate(constructorDefault);
                CacheConstructorsParameters[constructor_] = null;
                return constructor_(null, ArrayObject());
            }

            ModLogger.Log("FEZMod", "XmlHelper can't find a constructor for element " + elem.Name + " of type " + type.FullName);
            return null;
        }

        public static object Parse(this Type type, string str) {
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
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
                MethodInfo parse;
                if (!CacheAddMethods.TryGetValue(type, out parse)) {
                    CacheParseMethods[type] = parse = type.GetMethod("Parse", new Type[] { typeof(String) });
                }
                return ReflectionHelper.InvokeMethod(parse, null, ArrayObject(str));
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
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
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
                            append &= null != entry.AppendChildIfNotNull(getItem.Invoke(obj, ArrayObject(key)).Serialize(document));
                            if (append) {
                                elem.AppendChild(entry);
                            }
                        }
                    } else {
                        PropertyInfo propertyCount = type.GetProperty("Count");
                        int count = (int) propertyCount.GetGetMethod().Invoke(obj, new object[0]);
                        if (getItem != null) {
                            for (int i = 0; i < count; i++) {
                                elem.AppendChildIfNotNull(getItem.Invoke(obj, ArrayObject(i)).Serialize(document));
                            }
                        } else {
                            //Selfnote from Maik: I usually hate Linq, but meh.
                            MethodInfo elementAt = typeof(System.Linq.Enumerable).GetMethod("ElementAt").MakeGenericMethod(types[0]);
                            for (int i = 0; i < count; i++) {
                                elem.AppendChildIfNotNull(elementAt.Invoke(null, ArrayObject(obj, i)).Serialize(document));
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

            FieldInfo[] fields;
            if (!CacheTypesFields.TryGetValue(type, out fields)) {
                CacheTypesFields[type] = fields = type.GetFields();
            }
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
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
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
            if (FEZModEngine.OverrideCultureManuallyBecauseMonoIsA_____) {
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

        //ARRAY HELPERS

        public static void FillArray<T>(T[][] array) {
            if (array[0] != null) {
                return;
            }
            for (int i = 0; i < array.Length; i++) {
                array[i] = new T[i];
            }
        }

        //TODO find a way to automatically generate this.
        private static object[][] arrayObject = new object[8][];
        public static object[] ArrayObjectOfLen(int len) {
            FillArray<object>(arrayObject);
            return len < arrayObject.Length ? arrayObject[len] : new object[len];
        }
        public static object[] ArrayObject() {
            object[] array = ArrayObjectOfLen(0);
            return array;
        }
        public static object[] ArrayObject(object a) {
            object[] array = ArrayObjectOfLen(1);
            array[0] = a;
            return array;
        }
        public static object[] ArrayObject(object a, object b) {
            object[] array = ArrayObjectOfLen(2);
            array[0] = a; array[1] = b;
            return array;
        }
        public static object[] ArrayObject(object a, object b, object c) {
            object[] array = ArrayObjectOfLen(3);
            array[0] = a; array[1] = b; array[2] = c;
            return array;
        }
        public static object[] ArrayObject(object a, object b, object c, object d) {
            object[] array = ArrayObjectOfLen(4);
            array[0] = a; array[1] = b; array[2] = c; array[3] = d;
            return array;
        }
        public static object[] ArrayObject(object a, object b, object c, object d, object e) {
            object[] array = ArrayObjectOfLen(5);
            array[0] = a; array[1] = b; array[2] = c; array[3] = d; array[4] = e;
            return array;
        }
        public static object[] ArrayObject(object a, object b, object c, object d, object e, object f) {
            object[] array = ArrayObjectOfLen(6);
            array[0] = a; array[1] = b; array[2] = c; array[3] = d; array[4] = e; array[5] = f;
            return array;
        }
        public static object[] ArrayObject(object a, object b, object c, object d, object e, object f, object g) {
            object[] array = ArrayObjectOfLen(7);
            array[0] = a; array[1] = b; array[2] = c; array[3] = d; array[4] = e; array[5] = f; array[6] = g;
            return array;
        }
        public static object[] ArrayObject(object a, object b, object c, object d, object e, object f, object g, object h) {
            object[] array = ArrayObjectOfLen(8);
            array[0] = a; array[1] = b; array[2] = c; array[3] = d; array[4] = e; array[5] = f; array[6] = g; array[7] = h;
            return array;
        }

        private static XmlNode[][] arrayXmlNode = new XmlNode[8][];
        public static XmlNode[] ArrayXmlNodeOfLen(int len) {
            FillArray<XmlNode>(arrayXmlNode);
            return len < arrayXmlNode.Length ? arrayXmlNode[len] : new XmlNode[len];
        }
        public static XmlNode[] ArrayXmlNode() {
            XmlNode[] array = ArrayXmlNodeOfLen(0);
            return array;
        }
        public static XmlNode[] ArrayXmlNode(XmlNode a) {
            XmlNode[] array = ArrayXmlNodeOfLen(1);
            array[0] = a;
            return array;
        }
        public static XmlNode[] ArrayXmlNode(XmlNode a, XmlNode b) {
            XmlNode[] array = ArrayXmlNodeOfLen(2);
            array[0] = a; array[1] = b;
            return array;
        }

    }
}

