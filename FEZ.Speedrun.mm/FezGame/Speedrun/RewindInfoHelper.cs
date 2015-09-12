using System;
using System.Reflection;
using System.Collections.Generic;
using FezGame.Structure;
using FezGame.Mod;

namespace FezGame.Speedrun {
    public struct CacheKey_Info_Value {
        public RewindInfo Key;
        public object Value;
    }

    public static class RewindInfoHelper {

        private static readonly Dictionary<string, MemberInfo> memberCache = new Dictionary<string, MemberInfo>();

        public static MemberInfo GetFieldOrProperty(this Type type, string memberName) {
            MemberInfo member;
            if (memberCache.TryGetValue(memberName, out member)) {
                return member;
            }
            return memberCache[memberName] = (((MemberInfo) type.GetField(memberName)) ?? type.GetProperty(memberName));
        }

    }
}

