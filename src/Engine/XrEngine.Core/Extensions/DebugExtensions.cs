using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace XrEngine
{
    public static class DebugExtensions
    {
        public static void Print(this Joint3D self, StringBuilder result, string indent = "")
        {
            result.AppendLine($"{indent}{self.Name}");

            foreach (var child in self.Children)
            {
                if (child is Joint3D joint)
                    joint.Print(result, indent + "  ");
            }
        }

        public static void PrintPose(this Joint3D self, StringBuilder result, string indent = "")
        {
            static string V3(Vector3 value) =>
                FormattableString.Invariant($"{value.X:R},{value.Y:R},{value.Z:R}");

            static string Q(Quaternion value) =>
                FormattableString.Invariant($"{value.X:R},{value.Y:R},{value.Z:R},{value.W:R}");

            static string M(Matrix4x4 value) =>
                FormattableString.Invariant($"{value.M11:R},{value.M12:R},{value.M13:R},{value.M14:R},{value.M21:R},{value.M22:R},{value.M23:R},{value.M24:R},{value.M31:R},{value.M32:R},{value.M33:R},{value.M34:R},{value.M41:R},{value.M42:R},{value.M43:R},{value.M44:R}");

            result.AppendLine(
                $"{indent}{self.Name} | " +
                $"parent={(self.Parent is Joint3D parent ? parent.Name : "<root>")} | " +
                $"localPos={V3(self.Transform.Position)} | " +
                $"localRot={Q(self.Transform.Orientation)} | " +
                $"localScale={V3(self.Transform.Scale)} | " +
                $"worldPos={V3(self.WorldPosition)} | " +
                $"worldRot={Q(self.WorldOrientation)} | " +
                $"world={M(self.WorldMatrix)}");

            foreach (var child in self.Children)
            {
                if (child is Joint3D joint)
                    joint.PrintPose(result, indent + "  ");
            }
        }
    }
}
