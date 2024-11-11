using XrEngine;
using XrEngine.Services;


[assembly: Module(typeof(XrEngine.Module))]

namespace XrEngine
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement(AssetLoader.Instance);
            Context.Implement(ModuleManager.Instance);
            Context.Implement(ObjectManager.Instance);
            Context.Implement(TypeStateManager.Instance);

            var assetLoader = AssetLoader.Instance;

            assetLoader.Register(DdsReader.Instance);
            assetLoader.Register(ExrReader.Instance);
            assetLoader.Register(HdrReader.Instance);
            assetLoader.Register(ImageReader.Instance);
            assetLoader.Register(Ktx2Reader.Instance);
            assetLoader.Register(KtxReader.Instance);
            assetLoader.Register(PkmReader.Instance);
            assetLoader.Register(PvrTranscoder.Instance);

            var typeState = TypeStateManager.Instance;

            typeState.Register(Vector3StateManager.Instance);
            typeState.Register(ColorStateManager.Instance);
            typeState.Register(Matrix4x4StateManager.Instance);
            typeState.Register(QuaternionStateManager.Instance);
            typeState.Register(ObjectIdStateManager.Instance);

            typeState.Register(EngineObjectStateManager.Instance);
            typeState.Register(new StateObjectManager<IStateObject>());
            typeState.Register(DefaultStateManager.Instance);
            typeState.Register(ObjectStateManager.Instance);
        }

        public void Shutdown()
        {

        }
    }
}

