using OpenXr.Framework;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenXr.Oculus.Helpers
{
    public static class XrExtensions
    {
        public static Joint3D BuildScheleton(this BodySkeletonJointFB[] self, string name, out Dictionary<int, Joint3D> outMap)
        {
            Dictionary<int, Joint3D> map = [];

            Joint3D Visit(int index)
            {
                if (map.TryGetValue(index, out var result))
                    return result;

                var current = self[index];

                result = new Joint3D
                {
                    Name = ((FullBodyJointMETA)index).ToString()
                };

                result.SetWorldPose(current.Pose.ToPose3());
                map[index] = result;

                if (current.ParentJoint != -1)
                    Visit(current.ParentJoint).AddChild(result, true);

                return result;
            }

            for (var i = 0; i < self.Length; i++)
                Visit(i);

            var root = map[0];
            root.Name = name;

            outMap = map;

            return root;
        }
    }
}
