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

namespace FezGame.Mod {
    public static class XmlHelper {

        [Obsolete("Use AutoParse instead.")]
        public static object Parse(this XmlElement elem, bool descend = true) {
            return AutoParse(elem, descend);
        }

        public static object AutoParse(this XmlElement elem, bool descend = true) {
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

            if (descend) {
                return 
                    AutoParse(elem["TrileEmplacement"]) ??
                    AutoParse(elem["Vector3"]) ??
                    AutoParse(elem["Quaternion"]);
            } else {
                return null;
            }
        }

        public static object Deserialize(this XmlNode node, Type parent = null, ContentManager cm = null, bool descend = true) {
            if (node == null) {
                return null;
            }

            XmlElement elem = node as XmlElement;

            if (node.Name == "Entry") {
                return node.ChildNodes[0].Deserialize(parent, cm, descend);
            }

            object parsed = elem.AutoParse(false);
            if (parsed != null) {
                return parsed;
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
                ModLogger.Log("FEZMod", "XmlHelper found no Type for " + node.Name);
                return node.InnerText;
            } else {
                //ModLogger.Log("FEZMod", "elem: " + elem.Name + "; type: " + type.FullName);
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

            if (obj is ICollection) {
                Type[] types = obj.GetType().GetGenericArguments();
                ModLogger.Log("FEZMod", "XmlHelper got " + type.FullName + " with " + types.Length + " generic arguments.");
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
                foreach (XmlNode child in node.ChildNodes) {
                    //ModLogger.Log("FEZMod", "child: "+child.Name);

                    FieldInfo field_;
                    if ((field_ = type.GetField(child.Name)) != null) {
                        field_.SetValue(obj, child.Deserialize(type, cm));
                        continue;
                    }

                    PropertyInfo property_;
                    if ((property_ = type.GetProperty(child.Name)) != null) {
                        MethodInfo setter = property_.GetSetMethod();
                        if (setter == null) {
                            ModLogger.Log("FEZMod", "XmlHelper found no setter for Property " + child.Name + " in " + type.FullName);
                            continue;
                        }
                        setter.Invoke(obj, new object[] { child.Deserialize(type, cm) });
                        continue;
                    }

                    FieldInfo[] fields = type.GetFields();
                    foreach (FieldInfo field in fields) {
                        if (field.FieldType.Name.ToLower() == child.Name.ToLower()) {
                            field.SetValue(obj, child.Deserialize(type, cm));
                            fields = null;
                            break;
                        }
                    }
                    if (fields == null) {
                        continue;
                    }

                    PropertyInfo[] properties = type.GetProperties();
                    foreach (PropertyInfo property in properties) {
                        if (property.PropertyType.Name.ToLower() == child.Name.ToLower()) {
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
                        continue;
                    }

                    ModLogger.Log("FEZMod", "XmlHelper found no Field or Property named or for type " + child.Name + " for " + type.FullName);
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
                ModLogger.Log("FEZMod", "HSD: TrileInstance");
                ((TrileInstance) obj).SetPhiLight(byte.Parse(elem.GetAttribute("orientation")));
            }

            if (obj is ArtObjectInstance) {
                ModLogger.Log("FEZMod", "HSD: ArtObjectInstance");
                ((ArtObjectInstance) obj).ArtObject = cm.Load<ArtObject>("Art objects/"+elem.GetAttribute("name"));
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
                    ModLogger.Log("FEZMod", "XmlHelper creates an array of exact type " + type.FullName);
                    ModLogger.Log("FEZMod", "XmlHelper creates an array of element type " + type.GetElementType().FullName);
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
                    return elem.AutoParse(true);//worst-case hack
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

            return elem.AutoParse(true);//worst-case hack
        }

        public static object Parse(this Type type, string str) {
            if (type == null || str == null) {
                return null;
            }
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

