using OpenXr.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class UIWebPanel : TriangleMesh
    {
        bool _isFirstPose = true;

        public UIWebPanel(XrBoolInput moveInput, PerspectiveCamera camera)
            : base(Quad3D.Default)
        {
            Materials.Add(new ColorMaterial(Color.Transparent)
            {
                Alpha = AlphaMode.Opaque
            });

            Name = "ui";

            Transform.Scale = new Vector3(0.51f, 0.32f, 1f);

            this.AddComponent<QuadCollider>();

            this.AddBehavior((_, _) =>
            {
                if (_isFirstPose || (moveInput.IsChanged && moveInput.Value))
                {
                    WorldPosition = camera.WorldPosition + camera.Forward * 0.5f;
                    WorldOrientation = camera.WorldOrientation;
                    _isFirstPose = false;
                }
            });
        }
    }
}
