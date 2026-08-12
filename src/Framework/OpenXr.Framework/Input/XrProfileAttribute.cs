using System;
using System.Collections.Generic;
using System.Text;

namespace OpenXr.Framework.Input
{
    public static class XrProfiles
    {
        public const string TouchPlus = "/interaction_profiles/meta/touch_controller_plus";
        public const string Touch = "/interaction_profiles/oculus/touch_controller";
        public const string TouchPro = "/interaction_profiles/oculus/touch_controller_pro";
        public const string TouchProFb = "/interaction_profiles/facebook/touch_controller_pro";
        public const string Simple = "/interaction_profiles/khr/simple_controller";
        public const string Hand = "/interaction_profiles/ext/hand_interaction_ext";
    }

        [AttributeUsage(AttributeTargets.Field)]
        public class XrProfileAttribute : Attribute
        {
            public XrProfileAttribute(params string[] profiles)
            {
                Profiles = profiles;
            }


            public string[] Profiles { get; }
        }
}
