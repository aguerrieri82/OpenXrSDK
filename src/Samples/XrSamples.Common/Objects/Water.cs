using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples
{
    public sealed class Water : TriangleMesh
    {
        public Water(uint simulationSize = 256, uint gridSize = 256)
        {
            var grid = new Grid3D(new Size2I(gridSize, gridSize));
            grid.ComputeTangents();
            Geometry = grid;
            
            CompressionMode = MeshCompressionMode.Never;
            Flags |= EngineObjectFlags.NoFrustumCulling;
            
            Material = new WaterMaterial(simulationSize) { WaterSize = Vector2.One };
            Materials.Add(Material);
            
            Interaction = this.AddComponent<WaterInteraction>();
            this.AddComponent<WaterStepAudio>();

            SurfaceWaveSpeed = 300f;
            SurfacePersistence = 0.995f;
            BackgroundStrength = 0f;
            BackgroundSpacing = 0.065f;
            FineRippleSpacing = 0.008f;
            FineRippleExcitationRate = 9.966f;
            FineRippleExcitation = 0.143f;
            FineRipplePersistence = 0.98f;
        }

        public void ResetSimulation()
        {
            Interaction.Reset();
            Simulation?.RequestReset();
        }

        public WaterMaterial Material { get; }

        public WaterInteraction Interaction { get; }

        public GlWaterSimulationPass? Simulation { get; internal set; }

        public float SurfaceWaveSpeed { get; set; }

        public float SurfacePersistence { get; set; }

        public float BackgroundStrength { get; set; }

        public float BackgroundSpacing { get; set; }

        public float FineRippleSpacing { get; set; }

        public float FineRippleExcitationRate { get; set; }

        public float FineRippleExcitation { get; set; }

        public float FineRipplePersistence { get; set; }
    }
}
