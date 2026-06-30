using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Lighting
{
    public class VoxelRayMarcher : IDisposable
    {
        private EngineNativeLib.VoxelRayMarcher _handle;

        public VoxelRayMarcher(VoxelLightBaker backer)
        {
            _handle = EngineNativeLib.VoxelRayMarcherCreate(backer.Handle);
        }

        public bool Create(VoxelLightRay ray)
        {
            return EngineNativeLib.VoxelRayMarcherCreateRay(_handle, ref ray);
        }

        public bool Step()
        {
            return EngineNativeLib.VoxelRayMarcherStep(_handle);

        }

        public VoxelRayDebugState GetState()
        {
            VoxelRayDebugState state = new();
            EngineNativeLib.VoxelRayMarcherGetState(_handle, ref state);
            return state;
        }

        public void Dispose()
        {
            if (_handle.Handle == 0)
                return;
            
            EngineNativeLib.VoxelRayMarcherDestroy(_handle);

            _handle = default;

            GC.SuppressFinalize(this);
        }
    }
}
