using Android.Content;
using OpenXr.Framework;

namespace OpenXr.Test.Android
{
    public class OpenVrPlugin : BaseXrPlugin
    {
        private readonly MainActivity _activity;
        private GameActivity? _vrActivity;

        public OpenVrPlugin(MainActivity activity)
        {
            _activity = activity;
        }

        public override void OnSessionBegin()
        {
            _activity.StartActivity(new Intent(_activity, typeof(GameActivity)));

            base.OnSessionBegin();
        }

        public override void OnSessionEnd()
        {
            if (_vrActivity != null)
            {
                _vrActivity.Finish();
                _vrActivity = null;
            }

            base.OnSessionEnd();
        }

        public void RegisterVrActivity(GameActivity? vrActivity)
        {
            _vrActivity = vrActivity;
        }
    }
}
