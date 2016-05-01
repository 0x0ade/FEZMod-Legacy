using System;

//Straight outta trashbin (shadow-engine)
//It's so horrible, I initially didn't want to implement
//this. But as FEZMod became somewhat larger than expected,
//a shared Garbage class and its caches don't seem to be that
//bad to keep the GC quiet. Furthermore, FEZ itself already kills
//some Android devices and thus lowering the GC invocation count
//and frequency for FEZMod's garbage should help.
//TL;DR: This class keeps the GC quiet and is horrible.
namespace FezEngine.Mod {
    
    public static class Garbage {
        
        //Caches
        
        public readonly static ArrayCache<object> a_object_1 = new ArrayCache<object>(1);
        public readonly static ArrayCache<object> a_object_2 = new ArrayCache<object>(2);
        public readonly static ArrayCache<object> a_object_3 = new ArrayCache<object>(3);
        public readonly static ArrayCache<object> a_object_4 = new ArrayCache<object>(4);
        
        //Utility / helper fields
        
        public readonly static object[] a_object_0 = new object[0];
        
        public readonly static Type[] a_Type_0 = new Type[0];
        
        
        //Utility / helper methods
        
        //Add more methods as needed. TODO: Automate this.
        public static object[] GetObjectArray(object _0) {
            object[] a_ = Garbage.a_object_1.GetNext();
            a_[0] = _0;
            return a_;
        }
        public static object[] GetObjectArray(object _0, object _1) {
            object[] a_ = Garbage.a_object_2.GetNext();
            a_[0] = _0;
            a_[1] = _1;
            return a_;
        }
        public static object[] GetObjectArray(object _0, object _1, object _2) {
            object[] a_ = Garbage.a_object_3.GetNext();
            a_[0] = _0;
            a_[1] = _1;
            a_[2] = _2;
            return a_;
        }
        public static object[] GetObjectArray(object _0, object _1, object _2, object _3) {
            object[] a_ = Garbage.a_object_4.GetNext();
            a_[0] = _0;
            a_[1] = _1;
            a_[2] = _2;
            a_[3] = _3;
            return a_;
        }
        
        
    }
}