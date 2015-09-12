//#define COMPILE_SIIP
#if COMPILE_SIIP
using System;
using FezEngine.Effects;
using Microsoft.Xna.Framework.Graphics;
using MonoMod;
using FezGame.Mod;

namespace FezEngine.Structure.Geometry {
    public class ShaderInstancedIndexedPrimitives<TemplateType, InstanceType> : IndexedPrimitiveCollectionBase<TemplateType, int>, IFakeDisposable where TemplateType : struct, IShaderInstantiatableVertex where InstanceType : struct {

        public int PredictiveBatchSize = 16;
        private int[] tempIndices = new int[0];
        private TemplateType[] tempVertices = new TemplateType[0];
        private readonly int InstancesPerBatch;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        public InstanceType[] Instances;
        public int InstanceCount;
        private int oldInstanceCount;
        private InstanceType[] tempInstances;

        [MonoModIgnore]
        public ShaderInstancedIndexedPrimitives(PrimitiveType type, int instancesPerBatch)
            : base(type) {
            InstancesPerBatch = instancesPerBatch;
        }

        public void orig_Draw(BaseEffect effect) {
        }

        public override void Draw(BaseEffect effect) {
            //Only tested on v1.11
            if (FEZMod.FEZVersion < new Version(1, 11)) {
                orig_Draw(effect);
                return;
            }

            if (device == null || primitiveCount == 0 || vertices == null || vertices.Length == 0 || indexBuffer == null || vertexBuffer == null || Instances == null || InstanceCount == 0) {
                return;
            }

            IShaderInstantiatableEffect<InstanceType> siEffect = effect as IShaderInstantiatableEffect<InstanceType>;
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            int num;
            for (int i = InstanceCount; i > 0; i -= num) {
                num = Math.Min(i, InstancesPerBatch);
                int sourceIndex = InstanceCount - i;
                if (tempInstances == null || tempInstances.Length < num) {
                    tempInstances = new InstanceType[Math.Min((int) Math.Ceiling((double) num / (double) PredictiveBatchSize) * PredictiveBatchSize, InstancesPerBatch)];
                }
                Array.Copy(Instances, sourceIndex, tempInstances, 0, Math.Min(num, tempInstances.Length));
                siEffect.SetInstanceData(tempInstances, Math.Min(num, tempInstances.Length));
                effect.Apply();
                device.DrawIndexedPrimitives(primitiveType, 0, 0, num * vertices.Length, 0, num * primitiveCount);
            }
        }

        [MonoModIgnore]
        public void Dispose() {
        }

        [MonoModIgnore]
        public override IIndexedPrimitiveCollection Clone() {
            return null;
        }

    }
}
#endif
