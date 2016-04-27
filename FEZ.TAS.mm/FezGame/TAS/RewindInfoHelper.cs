using System;
using System.Reflection;
using System.Collections.Generic;

namespace FezGame.TAS {
    public struct CacheKey_Info_Value {
        public RewindInfo Key;
        public object Value;
    }

    public struct RewindValue_CurrentSegmentIndex_sinceSegmentStarted {
        public object CurrentSegmentIndex; //originally int, but typecasting is useless
        public object sinceSegmentStarted; //originally TimeSpan, but typecasting is useless
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

