using System;
using FezEngine.Effects;
#if FNA
using Microsoft.Xna.Framework;
#endif
using Microsoft.Xna.Framework.Graphics;
using MonoMod;
using FezGame.Mod;

namespace FezEngine.Structure.Geometry {
    public class ShaderInstancedIndexedPrimitives<TemplateType, InstanceType> : IndexedPrimitiveCollectionBase<TemplateType, int>, IFakeDisposable where TemplateType : struct, IShaderInstantiatableVertex where InstanceType : struct {

        //Reusable version instances, just because
        private readonly static Version V_1_11 = new Version(1, 11);
        private readonly static Version V_1_12 = new Version(1, 12);
        
        //Field list taken from 1.12; Compiles for non-FNA, which won't use the new fields anyways.
        [MonoModIgnore]
        public int PredictiveBatchSize;
        [MonoModIgnore]
        private int[] tempIndices;
        [MonoModIgnore]
        private InstanceType[] tempInstances;
        [MonoModIgnore]
        private TemplateType[] tempVertices;
        [MonoModIgnore]
        private VertexDeclaration vertexDeclaration;
        [MonoModIgnore]
        private bool appendIndex;
        [MonoModIgnore]
        private IndexedVector4[] indexedInstances;
        [MonoModIgnore]
        private bool useHwInstancing;
        [MonoModIgnore]
        private int oldInstanceCount;
        [MonoModIgnore]
        private readonly int InstancesPerBatch;
        [MonoModIgnore]
        private VertexBuffer vertexBuffer;
        [MonoModIgnore]
        private IndexBuffer indexBuffer;
        [MonoModIgnore]
        private DynamicVertexBuffer instanceBuffer;
        [MonoModIgnore]
        public InstanceType[] Instances;
        [MonoModIgnore]
        public int InstanceCount;
        [MonoModIgnore]
        public bool InstancesDirty;

        #if !FNA
        [MonoModIgnore]
        public ShaderInstancedIndexedPrimitives(PrimitiveType type, int instancesPerBatch)
            : base(type) {
            InstancesPerBatch = instancesPerBatch;
        }
        #else
        //Custom fields
        private VertexBufferBinding[] tmpVertexBufferBindingArray;
        
        [MonoModIgnore]
        public ShaderInstancedIndexedPrimitives(PrimitiveType type, int instancesPerBatch, bool appendIndex = false)
            : base(type) {
            InstancesPerBatch = instancesPerBatch;
            appendIndex = appendIndex;
            RefreshInstancingMode(true);
        }
        #endif

        [MonoModIgnore]
        public void RefreshInstancingMode(bool force = false) {
        }

        [MonoModIgnore]
        public void Dispose() {
        }
        
        [MonoModIgnore]
        public void UpdateBuffers() {
            UpdateBuffers(false);
		}

        [MonoModIgnore]
        private void UpdateBuffers(bool rebuild) {
		}

        [MonoModIgnore]
        public override IIndexedPrimitiveCollection Clone() {
            return null;
        }

        public extern void orig_Draw(BaseEffect effect);
        public override void Draw(BaseEffect effect) {
            if (device == null || primitiveCount == 0 || vertices == null || vertices.Length == 0 || indexBuffer == null || vertexBuffer == null || Instances == null || InstanceCount <= 0) {
                return;
            }
            
            #if !FNA
            if (FEZMod.FEZVersion == V_1_11) {
                Draw_1_11(effect);
                return;
            }
            #else
            //FNA is used by 1.12+ anyways, so avoid errors when calling / patching this method.
            //Also, FEZ 1.12+ fixes the Intel bug. Custom SIIP code shouldn't be required anymore.
            if (FEZMod.FEZVersion == V_1_12) {
                Draw_1_12(effect);
                return;
            }
            #endif
            
            orig_Draw(effect);
        }
        
        //Version-dependant drawing code
        #if !FNA
        
        //Tested successfully.
        public void Draw_1_11(BaseEffect effect) {
            device.SetVertexBuffer(vertexBuffer);
            
            device.Indices = indexBuffer;
            
            //The 1.12 and 1.11 software code doesn't seem to differ too much
            //TODO test if it also works in 1.07 (speedrun) just because
            DrawInstances(effect);
        }
        
        #else
        
        //1.12 uses FNA
        public void Draw_1_12(BaseEffect effect) {
            if (useHwInstancing) {
                if (tmpVertexBufferBindingArray == null) {
                    //Required here as MonoMod can't patch the initialization of tmpVertexBufferBindingArray
                    tmpVertexBufferBindingArray = new VertexBufferBinding[2];
                }
                //Re-use the same array every call (useless microoptimization)
                tmpVertexBufferBindingArray[0] = vertexBuffer;
                //TODO for yet another useless microoptimization, reuse [1]
                tmpVertexBufferBindingArray[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
                device.SetVertexBuffers(tmpVertexBufferBindingArray);
            } else {
                device.SetVertexBuffer(vertexBuffer);
            }
            
            device.Indices = indexBuffer;
            
            if (useHwInstancing) {
                DrawInstancesHW(effect);
            } else {
                DrawInstances(effect);
            }
        }
        
        //Shared code
        private void DrawInstancesHW(BaseEffect effect) {
            if (InstancesDirty) {
                if (appendIndex) {
                    for (int i = 0; i < InstanceCount; i++) {
                        indexedInstances[i].Data = (Vector4) ((object) Instances[i]);
                        indexedInstances[i].Index = (float) i;
                    }
                    instanceBuffer.SetData<IndexedVector4>(indexedInstances, 0, InstanceCount);
                } else {
                    if (Instances.Length < InstanceCount || InstanceCount > oldInstanceCount) {
                        //instance count mismatch or too small buffer on draw
                        ModLogger.Log("FEZMod.SIIP", "Forcibly updating instance buffer...");
                        UpdateBuffers(true);
                        return;
                    }
                    
                    instanceBuffer.SetData<InstanceType>(Instances, 0, InstanceCount);
                }
                InstancesDirty = false;
            }
            
            effect.Apply();
            device.DrawInstancedPrimitives(primitiveType, 0, 0, vertices.Length, 0, primitiveCount, InstanceCount);
        }
        
        #endif
        
        private void DrawInstances(BaseEffect effect) {
            IShaderInstantiatableEffect<InstanceType> siEffect = effect as IShaderInstantiatableEffect<InstanceType>;
            
            int batchInstanceCount = InstanceCount;
            int i = InstanceCount;
            int sourceIndex = 0;
            try {
                for (; i > 0; i -= batchInstanceCount) {
                    batchInstanceCount = Math.Min(i, InstancesPerBatch);
                    
                    /*int */sourceIndex = InstanceCount - i;
                    if (tempInstances == null || tempInstances.Length < batchInstanceCount) {
                        tempInstances = new InstanceType[Math.Min((int) Math.Ceiling((double) batchInstanceCount / (double) PredictiveBatchSize) * PredictiveBatchSize, InstancesPerBatch)];
                    }
                    
                    if (Instances.Length - sourceIndex < batchInstanceCount) {
                        //instance count mismatch, non-intel crash
                        i = 0; //otherwise ends up hanging in this loop
                        batchInstanceCount = Instances.Length - sourceIndex;
                    }
                    if (tempInstances.Length < batchInstanceCount) {
                        //instance count mismatch, intel crash
                        i -= batchInstanceCount - tempInstances.Length; //otherwise ends up hanging in this loop
                        batchInstanceCount = tempInstances.Length;
                    }
                    
                    Array.Copy(Instances, sourceIndex, tempInstances, 0, batchInstanceCount);
                    siEffect.SetInstanceData(tempInstances, batchInstanceCount);
                    
                    effect.Apply();
                    device.DrawIndexedPrimitives(primitiveType, 0, 0, batchInstanceCount * vertices.Length, 0, batchInstanceCount * primitiveCount);
                }
            } catch (Exception e) {
                ModLogger.Log("FEZMod.SIIP", "Error: " + e);
                ModLogger.Log("FEZMod.SIIP", "i: " + i);
                ModLogger.Log("FEZMod.SIIP", "batchInstanceCount: " + batchInstanceCount);
                ModLogger.Log("FEZMod.SIIP", "sourceIndex: " + sourceIndex);
                ModLogger.Log("FEZMod.SIIP", "InstanceCount: " + InstanceCount);
                if (tempInstances != null) {
                    ModLogger.Log("FEZMod.SIIP", "tempInstances.Length: " + tempInstances.Length);
                }
            }
        }

    }
}
