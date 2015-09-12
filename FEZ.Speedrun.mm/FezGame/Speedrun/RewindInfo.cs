using System;
using FezGame.Structure;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FezEngine;
using System.Collections.Generic;
using System.Reflection;
using Common;

namespace FezGame.Speedrun {
    public class RewindInfo {

        public object Instance;
        public Func<object> InstanceGetter;
        public MemberInfo Member;
        public Func<object, object> Getter;
        public Action<object, object> Setter;

        public RewindInfo(MemberInfo member) {
            Member = member;
        }

        public object Get() {
            if (Instance == null && InstanceGetter != null) {
                Instance = InstanceGetter();
            }

            if (Getter != null) {
                return Getter(Instance);
            }

            return ReflectionHelper.GetValue(Member, Instance);
        }

        public void Set(object value) {
            if (Instance == null && InstanceGetter != null) {
                Instance = InstanceGetter();
            }

            if (Setter != null) {
                Setter(Instance, value);
                return;
            }

            ReflectionHelper.SetValue(Member, Instance, value);
        }

    }
}

