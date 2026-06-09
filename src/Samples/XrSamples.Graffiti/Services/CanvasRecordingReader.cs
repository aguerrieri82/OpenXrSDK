using System.Numerics;
using System.Text.Json;
using XrMath;

namespace XrSamples.Graffiti
{
    public abstract record CanvasRecordCommand(float Time);

    public sealed record SprayCommand(
        float Time,
        Pose3 CanPose,
        float Aperture
    ) : CanvasRecordCommand(Time);

    public sealed record SprayCloseCommand(
        float Time
    ) : CanvasRecordCommand(Time);

    public sealed record UndoCommand(
        float Time
    ) : CanvasRecordCommand(Time);

    public sealed record ClearCommand(
        float Time
    ) : CanvasRecordCommand(Time);


    public sealed record ChangeColorCommand(
        float Time,
        Color Color
    ) : CanvasRecordCommand(Time);

    public sealed record CanvasCommand(
        float Time,
        Vector2 Size,
        Pose3 Pose
    ) : CanvasRecordCommand(Time);

    public sealed record ParamsCommand(
        float Time,

        float DryRoughness,
        float WetRoughness,
        float NormalScale,
        float DryRate,
        float DripRate,
        float PaintOpacityScale,

        float SpreadAngle,
        Vector3 SprayCenter,
        Vector3 SprayDirection,
        float SprayRadius,
        float RadialFalloff,
        float BaseDensity
    ) : CanvasRecordCommand(Time);

    public sealed class CanvasRecording
    {
        public List<CanvasRecordCommand> Commands { get; } = new();
    }

    public static class CanvasRecordingReader
    {
        public static CanvasRecording ReadFile(string filePath)
        {
            return Read(File.ReadAllText(filePath));
        }


        public static CanvasRecording Read(string json)
        {
            using var doc = JsonDocument.Parse(RepairRecordingJson(json));
            return Read(doc.RootElement);
        }

        static string RepairRecordingJson(string json)
        {
            json = json.TrimEnd();

            if (json.Length == 0)
                throw new FormatException("Recording file is empty.");

            if (json[0] != '[')
                throw new FormatException("Recording root does not start with '['.");

            if (json.EndsWith("\n]", StringComparison.Ordinal))
                return json;

            return json + "\n]";
        }

        public static CanvasRecording Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Array)
                throw new FormatException("Recording root must be an array.");

            var result = new CanvasRecording();
            var index = 0;

            foreach (var entry in root.EnumerateArray())
            {
                result.Commands.Add(ReadCommand(entry, index));
                index++;
            }

            return result;
        }

        static CanvasRecordCommand ReadCommand(JsonElement e, int index)
        {
            if (e.ValueKind != JsonValueKind.Array)
                throw new FormatException($"Entry {index} is not an array.");

            if (e.GetArrayLength() < 2)
                throw new FormatException($"Entry {index} has less than 2 elements.");

            var op = (CanvasRecorder.OpType)e[0].GetInt32();
            var time = e[1].GetSingle();

            return op switch
            {
                CanvasRecorder.OpType.Spray =>
                    ReadSpray(e, index, time),

                CanvasRecorder.OpType.ChangeColor =>
                    ReadChangeColor(e, index, time),

                CanvasRecorder.OpType.Canvas =>
                    ReadCanvas(e, index, time),

                CanvasRecorder.OpType.Params =>
                    ReadParams(e, index, time),

                CanvasRecorder.OpType.SprayClose =>
                    ReadSprayClose(e, index, time),

                CanvasRecorder.OpType.Undo =>
                    ReadUndo(e, index, time),

                CanvasRecorder.OpType.Clear =>
                    ReadClear(e, index, time),

                _ =>
                    throw new FormatException($"Unknown op type {(int)op} at entry {index}.")
            };
        }

        static SprayCommand ReadSpray(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 4);

            return new SprayCommand(
                time,
                ReadPose(e[2]),
                e[3].GetSingle()
            );
        }

        static ChangeColorCommand ReadChangeColor(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 3);

            return new ChangeColorCommand(
                time,
                ReadColor(e[2])
            );
        }

        static CanvasCommand ReadCanvas(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 4);

            return new CanvasCommand(
                time,
                ReadVector2(e[2]),
                ReadPose(e[3])
            );
        }


        static SprayCloseCommand ReadSprayClose(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 2);

            return new SprayCloseCommand(
                time
            );
        }

        static UndoCommand ReadUndo(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 2);

            return new UndoCommand(
                time
            );
        }

        static ClearCommand ReadClear(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 2);

            return new ClearCommand(
                time
            );
        }

        static ParamsCommand ReadParams(JsonElement e, int index, float time)
        {
            RequireCount(e, index, 14);

            return new ParamsCommand(
                time,

                DryRoughness: e[2].GetSingle(),
                WetRoughness: e[3].GetSingle(),
                NormalScale: e[4].GetSingle(),
                DryRate: e[5].GetSingle(),
                DripRate: e[6].GetSingle(),
                PaintOpacityScale: e[7].GetSingle(),

                SpreadAngle: e[8].GetSingle(),
                SprayCenter: ReadVector3(e[9]),
                SprayDirection: ReadVector3(e[10]),
                SprayRadius: e[11].GetSingle(),
                RadialFalloff: e[12].GetSingle(),
                BaseDensity: e[13].GetSingle()
            );
        }

        static Vector2 ReadVector2(JsonElement e)
        {
            RequireArray(e, 2);

            return new Vector2(
                e[0].GetSingle(),
                e[1].GetSingle()
            );
        }

        static Vector3 ReadVector3(JsonElement e)
        {
            RequireArray(e, 3);

            return new Vector3(
                e[0].GetSingle(),
                e[1].GetSingle(),
                e[2].GetSingle()
            );
        }

        static Quaternion ReadQuaternion(JsonElement e)
        {
            RequireArray(e, 4);

            return new Quaternion(
                e[0].GetSingle(),
                e[1].GetSingle(),
                e[2].GetSingle(),
                e[3].GetSingle()
            );
        }

        static Pose3 ReadPose(JsonElement e)
        {
            RequireArray(e, 2);

            return new Pose3
            {
                Position = ReadVector3(e[0]),
                Orientation = ReadQuaternion(e[1])
            };
        }

        static Color ReadColor(JsonElement e)
        {
            RequireArray(e, 4);

            return new Color(
                e[0].GetSingle(),
                e[1].GetSingle(),
                e[2].GetSingle(),
                e[3].GetSingle()
            );
        }

        static void RequireArray(JsonElement e, int expectedCount)
        {
            if (e.ValueKind != JsonValueKind.Array)
                throw new FormatException("Expected JSON array.");

            var count = e.GetArrayLength();

            if (count != expectedCount)
                throw new FormatException($"Array has {count} elements, expected {expectedCount}.");
        }

        static void RequireCount(JsonElement e, int entryIndex, int expectedCount)
        {
            var count = e.GetArrayLength();

            if (count != expectedCount)
            {
                throw new FormatException(
                    $"Entry {entryIndex} has {count} elements, expected {expectedCount}."
                );
            }
        }
    }
}