
using Common.Interop;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using XrEngine.Animation;
using XrEngine.Gltf;
using XrInteraction;

namespace XrEngine.OpenXr
{
    public class VirtualKeyboardView : Group3D, IXrVirtualKeyboardEventDispatcher, ITextInputProvider
    {
        event Action<TextInputEvent>? _textInput;

        XrVirtualKeyboard? _keyboard;
        XrApp? _xrApp;
        private Object3D? _model;
        private readonly Dictionary<ulong, Texture2D> _textureMap =[];
        private AnimationManager? _animationManager;
        private IReadOnlyList<IAnimation>? _animations;
        bool _isInit;
        bool _lastVisible;

        public VirtualKeyboardView()
        {
            Name = "Keyboard Container";

            Context.Implement<ITextInputProvider>(this);
        }

        protected bool TryInitialize()
        {
            _xrApp ??= XrEngineApp.Current?.XrApp;

            if (_xrApp == null || !_xrApp.IsStarted)
                return false;

            _xrApp.TextInput = this;

            _keyboard ??= new XrVirtualKeyboard(_xrApp);
            _keyboard.EventDispatcher = this;

            if (!_keyboard.IsSupported())
                return false;

            _keyboard.Create(isFar: false);

            using var stream = _keyboard.LoadModel();

            using var loader = new GltfLoader(_ => throw new NotSupportedException());

            _model = loader.Load(stream, new GltfLoaderOptions
            {
                GeometryHandler = geo =>
                {

                },

                TextureLoader = (url, tex, out data) =>
                {
                    data = new();

                    if (!_keyboard.TryParseTextureUri(url, out var textureId, out var width, out var height))
                        return false;

                    data.Width = (uint)width;
                    data.Height = (uint)height;
                    data.Format = TextureFormat.SRgba8;
                    data.Content = MemoryBuffer.Create(new byte[width * height * 4]);

                    lock (_textureMap)
                        _textureMap[textureId] = tex;
 
                    return true;
                }
            });

            _model.Name = "Keyboard";

            foreach (var mesh in _model.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                mesh.CompressionMode = MeshCompressionMode.Never;
                mesh.Flags |= EngineObjectFlags.NoFrustumCulling;

                foreach (var mat in mesh.Materials)
                {
                    if (mat.Alpha == AlphaMode.Blend) 
                    {
                        mat.UseDepth = false;
                        mat.WriteDepth = false;
                        ((PbrMaterial)mat).Roughness = 1;
                    }

                    if (mat.UseMorph)
                        mat.Morph = MorphMode.NotEmptyTargets;
                }

                if (mesh.Name == "collision")
                    mesh.AddComponent<BoxCollider>();
            }    

            AddChild(_model);

            _animationManager = _scene!.EnsureComponent<AnimationManager>();

            _animations = _model.Component<AnimationsHost>().Animations;

            _isInit = true;

            return true;
        }

        public void SetText(string text)
        {
            _keyboard?.SetTextContext(text);
        }

        protected override void UpdateSelf(RenderContext ctx)
        {
            if (_isInit || TryInitialize())
            {
                if (!_xrApp!.IsStarted)
                {
                    _keyboard?.Dispose();
                    _keyboard = null;
                    _textureMap.Clear();
                    _isInit = false;
                    return;
                }

                _keyboard!.UpdateInputs();

                foreach (var tex in _keyboard.GetDirtyTextures())
                {
                    var curTex = _textureMap![tex.Key];

                    curTex.Data = [new TextureData
                    {
                        Content = MemoryBuffer.Create(tex.Value),
                        Width = curTex.Width,
                        Height = curTex.Height,
                        Format = TextureFormat.SRgba8,
                    }];

                    curTex.Invalidate();

                }

                var states = _keyboard.GetAnimationStates();

                foreach (var state in states)
                {
                    var anim = _animations![state.AnimationIndex];

                    var control = _animationManager!.Create(anim);

                    control.Seek(state.Fraction);
                    control.Stop();
                }

                if (states.Length > 0)
                    Log.Info(this, "States {0}", states.Length);

                if (_lastVisible != _model!.IsVisible)
                {
                    _keyboard.SetVisible(_model.IsVisible);
                    _lastVisible = _model.IsVisible;
                }

                (var pose, var scale) = _keyboard.GetLocation();
                
                _model!.SetWorldPoseIfChanged(pose);

                if (scale != _model!.Transform.Scale.X)
                    _model.Transform.SetScale(scale);

            }

            base.UpdateSelf(ctx);
        }

        #region IXrVirtualKeyboardEventDispatcher

        void IXrVirtualKeyboardEventDispatcher.OnCommitText(string text)
        {
            _textInput?.Invoke(new TextInputEvent(TextInputEventType.CommitText, text));
        }

        void IXrVirtualKeyboardEventDispatcher.OnBackspace()
        {
            _textInput?.Invoke(new TextInputEvent(TextInputEventType.Backspace));
        }

        void IXrVirtualKeyboardEventDispatcher.OnEnter()
        {
            _textInput?.Invoke(new TextInputEvent(TextInputEventType.Enter));
        }

        void IXrVirtualKeyboardEventDispatcher.OnShown()
        {
            _model!.IsVisible = true;
            _keyboard!.SetLocation(VirtualKeyboardLocationTypeMETA.DirectMeta);
        }

        void IXrVirtualKeyboardEventDispatcher.OnHidden()
        {
            _model!.IsVisible = false;
        }

        #endregion

        #region ITextInputProvider

        event Action<TextInputEvent>? ITextInputProvider.Input
        {
            add => _textInput += value;
            remove => _textInput -= value;
        }

        bool ITextInputProvider.IsVisible => IsVisible;

        void ITextInputProvider.Show()
        {
            _model?.IsVisible = true;

        }

        void ITextInputProvider.Hide()
        {
            _model?.IsVisible = false;
        }

        void ITextInputProvider.SetText(string? text)
        {
            _keyboard?.SetTextContext(text ?? "");
        }

        #endregion
    }
}
