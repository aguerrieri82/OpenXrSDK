using Android.Content;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test.Android
{
    public class OpenVrPlugin : BaseXrPlugin
    {
        private MainActivity _activity;
        private VrActivity? _vrActivity;

        public OpenVrPlugin(MainActivity activity)
        {
            _activity = activity;
        }

        public override void OnSessionBegin()
        {
            _activity.StartActivity(new Intent(_activity, typeof(VrActivity)));

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

        public void RegisterVrActivity(VrActivity? vrActivity)
        {
            _vrActivity = vrActivity;
        }
    }
}
