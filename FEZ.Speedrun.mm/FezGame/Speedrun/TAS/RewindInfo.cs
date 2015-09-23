using System;
using FezGame.Structure;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FezEngine;
using System.Collections.Generic;
using System.Reflection;
using Common;
using FezGame.Mod;

namespace FezGame.Speedrun.TAS {
    public class RewindInfo {

        public object Instance;
        public Func<object> InstanceGetter;
        public MemberInfo Member;
        public Func<object, object> Getter;
        public Action<object, object> Setter;

        public RewindInfo() {
        }

        public RewindInfo(MemberInfo member)
            : this() {
            Member = member;
        }

        public RewindInfo(Func<object, object> getter, Action<object, object> setter)
            : this() {
            Getter = getter;
            Setter = setter;
        }

        public virtual object Get() {
            if (Instance == null && InstanceGetter != null) {
                Instance = InstanceGetter();
            }

            if (Getter != null) {
                return Getter(Instance);
            }

            if (Member == null) {
                return null;
            }
            if (Member is PropertyInfo && ((PropertyInfo) Member).GetGetMethod() == null) {
                return null;
            }
            if (Member is PropertyInfo && ((PropertyInfo) Member).GetSetMethod() == null && Setter == null) {
                return null;
            }

            if (Instance != null) {
                return ReflectionHelper.GetValue(Member, Instance);
            }

            return null;
        }

        public virtual void Set(object value) {
            if (Instance == null && InstanceGetter != null) {
                Instance = InstanceGetter();
            }

            if (Setter != null) {
                Setter(Instance, value);
                return;
            }

            if (Member == null) {
                return;
            }
            if (Member is PropertyInfo && ((PropertyInfo) Member).GetSetMethod() == null) {
                return;
            }
            if (Member is PropertyInfo && ((PropertyInfo) Member).GetGetMethod() == null && Getter == null) {
                return;
            }
            
            if (Instance != null) {
                ReflectionHelper.SetValue(Member, Instance, value);
                return;
            }
        }

    }
}

