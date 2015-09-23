using System;
using FezEngine.Tools;
using FezEngine.Components;
using System.Collections;
using System.Reflection;
using Common;
using FezGame.Mod;

namespace FezGame.Speedrun.TAS {
    public class MovingGroupsRewindInfo : RewindInfo {

        private IList MovingGroups_trackedGroups;
        private readonly static Type type_MovingGroupsHost_MovingGroupState = typeof(MovingGroupsHost).GetNestedType("MovingGroupState", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        private readonly static FieldInfo field_MovingGroupState_CurrentSegmentIndex = type_MovingGroupsHost_MovingGroupState.GetField("CurrentSegmentIndex", BindingFlags.Instance | BindingFlags.Public);
        private readonly static FieldInfo field_MovingGroupState_sinceSegmentStarted = type_MovingGroupsHost_MovingGroupState.GetField("sinceSegmentStarted", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly static MethodInfo method_MovingGroupState_ChangeSegment = type_MovingGroupsHost_MovingGroupState.GetMethod("ChangeSegment", BindingFlags.Instance | BindingFlags.NonPublic);

        public MovingGroupsRewindInfo() 
            : base() {
        }

        public override object Get() {
            if (Instance == null && InstanceGetter != null) {
                Instance = ServiceHelper.Get(typeof(MovingGroupsHost));
            }

            if (Instance == null) {
                return null;
            }

            if (MovingGroups_trackedGroups == null) {
                MovingGroups_trackedGroups = (IList) ReflectionHelper.GetValue(typeof(MovingGroupsHost).GetField("trackedGroups", BindingFlags.Instance | BindingFlags.NonPublic), Instance);
            }

            RewindValue_CurrentSegmentIndex_sinceSegmentStarted[] values = new RewindValue_CurrentSegmentIndex_sinceSegmentStarted[MovingGroups_trackedGroups.Count];

            for (int i = 0; i < MovingGroups_trackedGroups.Count; i++) {
                values[i] = new RewindValue_CurrentSegmentIndex_sinceSegmentStarted() {
                    CurrentSegmentIndex = ReflectionHelper.GetValue((MemberInfo) field_MovingGroupState_CurrentSegmentIndex, MovingGroups_trackedGroups[i]),
                    sinceSegmentStarted = ReflectionHelper.GetValue((MemberInfo) field_MovingGroupState_sinceSegmentStarted, MovingGroups_trackedGroups[i])
                };
            }

            return values;
        }

        public override void Set(object value) {
            if (Instance == null && InstanceGetter != null) {
                Instance = ServiceHelper.Get(typeof(MovingGroupsHost));
            }

            if (Instance == null) {
                return;
            }

            if (MovingGroups_trackedGroups == null) {
                MovingGroups_trackedGroups = (IList) ReflectionHelper.GetValue(typeof(MovingGroupsHost).GetField("trackedGroups", BindingFlags.Instance | BindingFlags.NonPublic), Instance);
            }

            RewindValue_CurrentSegmentIndex_sinceSegmentStarted[] values = (RewindValue_CurrentSegmentIndex_sinceSegmentStarted[]) value;
            if (values.Length != MovingGroups_trackedGroups.Count) {
                return;
            }
            for (int i = 0; i < values.Length; i++) {
                ReflectionHelper.SetValue((MemberInfo) field_MovingGroupState_CurrentSegmentIndex, MovingGroups_trackedGroups[i], values[i].CurrentSegmentIndex);
                ReflectionHelper.SetValue((MemberInfo) field_MovingGroupState_sinceSegmentStarted, MovingGroups_trackedGroups[i], values[i].sinceSegmentStarted);
                ReflectionHelper.InvokeMethod(method_MovingGroupState_ChangeSegment, MovingGroups_trackedGroups[i], new object[0]);
            }
        }

    }
}

