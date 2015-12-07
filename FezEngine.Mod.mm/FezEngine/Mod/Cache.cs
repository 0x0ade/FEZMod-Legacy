using System;

//Straight outta trashbin (shadow-engine)
namespace FezEngine.Mod {
    
    public class Cache<T> {
    
        protected Type type;
        protected T[] cache;
        protected int pos;
        protected object[] args;
        protected Type[] types;
    
        public Cache()
            : this(8) {
        }
    
        public Cache(int amount)
            : this(amount, Garbage.a_object_0) {
        }
    
        public Cache(object[] args)
            : this(8, args, args != null && args.Length > 1 ? Type.GetTypeArray(args) : Garbage.a_Type_0) {
        }
    
        public Cache(object[] args, Type[] types)
            : this(8, args, types) {
        }
    
        public Cache(int amount, object[] args)
            : this(amount, args, args != null && args.Length > 1 ? Type.GetTypeArray(args) : Garbage.a_Type_0) {
        }
    
        public Cache(int amount, object[] args, Type[] types) {
            type = typeof(T);
            cache = new T[amount];
            this.args = args;
            this.types = types;
            //Combat memory fragmentation by filling the cache on initialization
            FillAll();
        }
    
        public Cache<T> Previous() {
            return Position(pos - 1);
        }
    
        public Cache<T> Next() {
            return Position(pos + 1);
        }
    
        public Cache<T> Move(int i) {
            return Position(pos + i);
        }
    
        public Cache<T> Position(int i) {
            if (i <= 0) {
                pos = cache.Length-1-((-i)%cache.Length);
            } else {
                pos = i%cache.Length;
            }
            return this;
        }
    
        public T GetPrevious() {
            return cache[Position(pos - 1).Fill().pos];
        }
    
        public T GetNext() {
            return cache[Position(pos + 1).Fill().pos];
        }
    
        public T GetPosition(int i) {
            return cache[Position(i).Fill().pos];
        }
    
        public T Get() {
            return cache[Fill().pos];
        }
    
        public virtual Cache<T> Fill() {
            if (cache[pos] == null) {
                cache[pos] = (T) type.GetConstructor(types).Invoke(args);
            }
            return this;
        }
        
        public Cache<T> FillAll() {
            for (int i = 0; i < cache.Length; i++) {
                Position(i);
                Fill();
            }
            return this;
        }
    
        public Cache<T> Set(T obj) {
            cache[pos] = obj;
            return this;
        }

    }
    
    public class ArrayCache<T> : Cache<T[]> {
        
        protected int arrsize;
        
         public ArrayCache(int arrsize)
            : this(8, arrsize) {
        }
    
        public ArrayCache(int amount, int arrsize)
            : this(amount, Garbage.a_object_0, arrsize) {
        }
    
        public ArrayCache(object[] args, int arrsize)
            : this(8, args, args != null && args.Length > 1 ? Type.GetTypeArray(args) : Garbage.a_Type_0, arrsize) {
        }
    
        public ArrayCache(object[] args, Type[] types, int arrsize)
            : this(8, args, types, arrsize) {
        }
    
        public ArrayCache(int amount, object[] args, int arrsize)
            : this(amount, args, args != null && args.Length > 1 ? Type.GetTypeArray(args) : Garbage.a_Type_0, arrsize) {
        }
    
        public ArrayCache(int amount, object[] args, Type[] types, int arrsize)
            : base(amount, args, types) {
                this.arrsize = arrsize;
                FillAll();
        }
        
        public override Cache<T[]> Fill() {
            if (arrsize <= 0) {
                return this;
            }
            if (cache[pos] == null) {
                cache[pos] = new T[arrsize];
            }
            return this;
        }
        
    }
    
}