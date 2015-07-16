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

namespace FezGame.Mod {
    public static class XmlHelper {

        public static object Deserialize(this XmlNode node, Type parent = null, ContentManager cm = null, bool descend = true) {
            if (node == null) {
                return null;
            }

            //TODO make it work with all enums
            if (node.Name == "FaceOrientation") {
                return Enum.Parse(typeof(FaceOrientation), node.InnerText);
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
                type = node.Name.FindType();
            }

            if (type == null) {
                if (descend) {
                    foreach (XmlNode child in node.ChildNodes) {
                        //childNode can be a XmlText...
                        object obj_ = child.Deserialize(parent, cm, true);
                        if (obj_ != null) {
                            return obj_;
                        }
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

            object obj = type.New(elem) ?? node.InnerText;

            if (obj is string) {
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
                        setter.Invoke(obj, new object[] { property.PropertyType.Parse(attrib.InnerText) });
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
                            add.Invoke(obj, new object[] { key, child.Deserialize(parent, cm, descend) });
                        } else if (child.ChildNodes.Count > 1 && child.ChildNodes.Count == types.Length) {
                            add.Invoke(obj, new object[] {
                                child.ChildNodes[0].Deserialize(parent, cm, descend),
                                child.ChildNodes[1].Deserialize(parent, cm, descend)
                            });
                        } else {
                            add.Invoke(obj, new object[] { child.Deserialize(parent, cm, descend) });
                        }
                    }
                } else if (obj is IList) {
                    IList list = (IList) obj;
                    int i = 0;
                    foreach (XmlNode child in node.ChildNodes) {
                        list[i] = child.Deserialize(parent, cm, descend);
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
                            setter.Invoke(obj, new object[] { child.Deserialize(type, cm) });
                            break;
                        }

                        FieldInfo[] fields = type.GetFields();
                        foreach (FieldInfo field in fields) {
                            if (field.Name.ToLower() == child.Name.ToLower() || field.FieldType.Name.ToLower() == child.Name.ToLower()) {
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
                            if (property.Name.ToLower() == child.Name.ToLower() || property.PropertyType.Name.ToLower() == child.Name.ToLower()) {
                                MethodInfo setter = property.GetSetMethod();
                                if (setter == null) {
                                    ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + child.Name + " in " + type.FullName);
                                    continue;
                                }
                                setter.Invoke(obj, new object[] { child.Deserialize(type, cm) });
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

            obj.HandleSpecialData(elem, cm);

            return obj;
        }

        public static void HandleSpecialData(this object obj, XmlElement elem, ContentManager cm) {
            if (obj == null || elem == null || cm == null) {
                return;
            }

            if (obj is TrileInstance) {
                ((TrileInstance) obj).SetPhiLight(byte.Parse(elem.GetAttribute("orientation")));
            }

            if (obj is ArtObjectInstance) {
                ((ArtObjectInstance) obj).ArtObject = cm.Load<ArtObject>("Art objects/"+elem.GetAttribute("name"));
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

            MethodInfo onDeserialization = obj.GetType().GetMethod("OnDeserialization");
            if (onDeserialization != null) {
                if (onDeserialization.GetParameters().Length == 0) {
                    onDeserialization.Invoke(obj, new object[0]);
                } else {
                    ModLogger.Log("FEZMod", "XmlHelper can't call OnDeserialization on " + obj + " of type " + obj.GetType().FullName + " because it requires parameters. XmlHelper can't pass parameters.");
                }
            }
        }

        public static Type FindType(this string name) {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies) {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types) {
                    if (type.Name == name && type.FullName.EndsWith("."+name)) {
                        return type;
                    }
                }
            }

            return null;
        }

        public static object New(this Type type, XmlElement elem = null) {
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
                if (attribs == null && parameters == null) {
                    return constructor.Invoke(new object[0]);
                } else if (attribs == null || parameters == null) {
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

            ModLogger.Log("FEZMod", "XmlHelper can't find a constructor for element " + elem.Name + " of type " + type.FullName);
            return null;
        }

        public static object Parse(this Type type, string str) {
            if (type == null || str == null) {
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
            ModLogger.Log("FEZMod", "XmlHelper can't parse " + type.FullName + " from the following data: " + str);
            return null;
        }

    }
}

