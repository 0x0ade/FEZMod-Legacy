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

        public static object Deserialize(this XmlElement elem, Type parent = null, bool descend = true) {
            if (elem == null) {
                return null;
            }

            if (elem.Name == "Entry") {
                return (elem.ChildNodes[0] as XmlElement).Deserialize(parent, descend);
            }

            object parsed = elem.AutoParse();
            if (parsed != null) {
                return parsed;
            }

            Type type = elem.Name.FindType();

            if (type == null && parent != null) {
                FieldInfo field = null;
                if ((field = parent.GetField(elem.Name)) != null) {
                    type = field.FieldType;
                }
            }

            if (type == null && parent != null) {
                PropertyInfo property = null;
                if ((property = parent.GetProperty(elem.Name)) != null) {
                    type = property.PropertyType;
                }
            }

            if (type == null) {
                if (descend) {
                    foreach (XmlNode child in elem.ChildNodes) {
                        //childNode can be a XmlText...
                        object obj_ = (child as XmlElement).Deserialize(parent, true);
                        if (obj_ != null) {
                            return obj_;
                        }
                    }
                }
            }

            if (type == null) {
                ModLogger.Log("FEZMod", "XmlHelper found no Type for " + elem.Name);
            }

            object obj = type.New(elem);

            XmlAttributeCollection attribs = elem.Attributes;

            foreach (XmlAttribute attrib in attribs) {
                FieldInfo field = null;
                if ((field = type.GetField(attrib.Name)) != null) {
                    field.SetValue(obj, field.FieldType.Parse(attrib.InnerText));
                    continue;
                }

                PropertyInfo property = null;
                if ((property = type.GetProperty(attrib.Name)) != null) {
                    property.GetSetMethod().Invoke(obj, new object[] { property.PropertyType.Parse(attrib.InnerText) });
                    continue;
                }
            }

            if (obj == null) {
                ModLogger.Log("FEZMod", "XmlHelper couldn't create a new object for " + elem.Name + " of type " + type.FullName);
                obj = type.New();
            }

            if (obj is ICollection) {
                Type[] types = obj.GetType().GetGenericArguments();
                ModLogger.Log("FEZMod", "XmlHelper got " + type.FullName + " with " + types.Length + " generic arguments.");
                MethodInfo add = type.GetMethod("Add", types);
                if (add != null) {
                    foreach (XmlElement child in elem.ChildNodes) {
                        string attribKey = child.GetAttribute("key");
                        if (!string.IsNullOrEmpty(attribKey)) {
                            object key = types[0].Parse(attribKey);
                            add.Invoke(obj, new object[] { key, child.Deserialize(parent, descend) });
                        } else if (child.ChildNodes.Count > 1 && child.ChildNodes.Count == types.Length) {
                            add.Invoke(obj, new object[] {
                                (child.ChildNodes[0] as XmlElement).Deserialize(parent, descend),
                                (child.ChildNodes[1] as XmlElement).Deserialize(parent, descend)
                            });
                        } else {
                            add.Invoke(obj, new object[] { child.Deserialize(parent, descend) });
                        }
                    }
                } else if (obj is IList) {
                    IList list = (IList) obj;
                    int i = 0;
                    foreach (XmlElement child in elem.ChildNodes) {
                        list[i] = child.Deserialize(parent, descend);
                        i++;
                    }
                } else {
                    ModLogger.Log("FEZMod", "XmlHelper could not add entries to " + elem.Name + " of type " + type.FullName);
                }
            } else {
                foreach (XmlElement child in elem.ChildNodes) {
                    FieldInfo field = null;
                    if ((field = type.GetField(child.Name)) != null) {
                        field.SetValue(obj, child.Deserialize(type));
                        continue;
                    }

                    PropertyInfo property = null;
                    if ((property = type.GetProperty(child.Name)) != null) {
                        property.GetSetMethod().Invoke(obj, new object[] { child.Deserialize(type) });
                        continue;
                    }

                    ModLogger.Log("FEZMod", "XmlHelper found no Field or Property named " + child.Name + " for " + type.FullName);
                }
            }

            return obj;
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
                    ModLogger.Log("FEZMod", "XmlHelper can't create an array of exact type " + type.FullName);
                    return null;
                }
            }

            if (elem == null) {
                ConstructorInfo constructor = type.GetDefaultConstructor();
                if (constructor != null) {
                    return constructor.Invoke(new object[0]);
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

            return null;
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
            } else if (type.IsPrimitive) {
                return type.GetMethod("Parse", new Type[] { typeof(String) }).Invoke(null, new object[] { str });
            } else {
                ModLogger.Log("FEZMod", "XmlHelper can't parse " + type.FullName + " from the following data: " + str);
                return null;
            }
        }

    }
}

