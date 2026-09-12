using Common.Interop;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrEngine.Animation;
using XrEngine.Gltf;
using XrInteraction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class VirtualKeyboardView : Group3D, IXrVirtualKeyboardEventDispatcher, ITextInputProvider
    {
        event Action<TextInputEvent>? _textInput;
        protected XrVirtualKeyboard? _keyboard;
        protected XrApp? _xrApp;
        protected Object3D? _model;
        protected readonly Dictionary<ulong, Texture2D> _textureMap = [];
        protected AnimationManager? _animationManager;
        protected IReadOnlyList<IAnimation>? _animations;
        protected bool _isInit;
        protected bool _lastVisible;

        public VirtualKeyboardView()
        {
            Name = "Keyboard Container";
            Scale = 0.35f;

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

            _keyboard.Create(Pose3.Identity);
            _keyboard.SetVisible(false);

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
            _model.IsVisible = false;
      
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

                var isVisible = _model!.IsVisible;

                if (_lastVisible != isVisible)
                {
                    _keyboard.SetVisible(isVisible);
                    _lastVisible = isVisible;
                }

                if (_model.Transform.Scale.X != Scale)
                    _model.Transform.SetScale(Scale);

                if (isVisible)
                    _keyboard.SetLocation(_model.GetWorldPose(), Scale);

            }

            base.UpdateSelf(ctx);
        }

        #region IXrVirtualKeyboardEventDispatcher

        void IXrVirtualKeyboardEventDispatcher.OnCommitText(string text)
        {
            _ = EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                _textInput?.Invoke(new TextInputEvent(TextInputEventType.CommitText, text));
            });
        }

        void IXrVirtualKeyboardEventDispatcher.OnBackspace()
        {
            _ = EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                _textInput?.Invoke(new TextInputEvent(TextInputEventType.Backspace));
            });
        }

        void IXrVirtualKeyboardEventDispatcher.OnEnter()
        {
            _ = EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                _textInput?.Invoke(new TextInputEvent(TextInputEventType.Enter));
            });
        }

        void IXrVirtualKeyboardEventDispatcher.OnShown()
        {
            _ = EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                Debug.Assert(_keyboard != null && _model != null);

                /*
                 (var pose, Scale) = _keyboard.GetLocation();
                 _model.SetWorldPoseIfChanged(pose);
                 */

                _model.IsVisible = true;
            });
        }

        void IXrVirtualKeyboardEventDispatcher.OnHidden()
        {
            _ = EngineApp.Current.Dispatcher.ExecuteAsync(() =>
            {
                Debug.Assert( _model != null);

                _model.IsVisible = false;
            });
        }

        #endregion

        #region ITextInputProvider

        event Action<TextInputEvent>? ITextInputProvider.Input
        {
            add => _textInput += value;
            remove => _textInput -= value;
        }

        bool ITextInputProvider.IsVisible => _model?.IsVisible ?? false;

        void ITextInputProvider.Show()
        {
            _keyboard?.SetVisible(true);
        }

        void ITextInputProvider.Hide()
        {
            _keyboard?.SetVisible(false);
        }

        void ITextInputProvider.SetText(string? text)
        {
            _keyboard?.SetTextContext(text ?? "");
        }

        #endregion


        public float Scale { get; set; }
    }
}