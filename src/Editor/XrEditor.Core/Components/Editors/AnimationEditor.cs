using XrEngine;
using XrEngine.Animation;

namespace XrEditor
{
    public class AnimationEditor : BaseEditor<IAnimation, IAnimation>, IDisposable
    {
        public AnimationEditor()
        {
            Properties = [];
            Player = new PlayerView();
        }

        protected override void OnEditValueChanged(IAnimation newValue)
        {
            base.OnEditValueChanged(newValue);

            var result = new List<PropertyView>();

            PropertyView.CreateProperties(newValue, null, result);

            Properties = result.ToArray();

            if (Player.EditValue is IDisposable disposable)
                disposable.Dispose();

            IAnimable? animableHost;

            if (Host is AnimationsHost animHost)
                animableHost = animHost.Host;
            else if (Host is IAnimable anim)
                animableHost = anim;
            else
                throw new NotSupportedException();

            Player.EditValue = new AnimationPlayer(newValue, animableHost);
        }

        public void Dispose()
        {
            if (Player.EditValue is IDisposable disposable)
                disposable.Dispose();

            GC.SuppressFinalize(this);
        }

        public PlayerView Player { get; }

        public PropertyView[] Properties { get; set; }

    }
}
