using System.Globalization;

var options = Options.Parse(args);

if (options.Sweep)
{
    Console.WriteLine("Wave-speed sweep using the shader's maximum step time (1/60 s)");
    Console.WriteLine("speed\tCFL\tstable\tfailure\tmax height\tmax velocity\troughness");

    foreach (var waveSpeed in new[] { 100f, 200f, 300f, 400f, 450f, 475f, 500f, 550f, 600f })
    {
        var parameters = options.Parameters with { WaveSpeed = waveSpeed, DeltaTime = 1f / 60f };
        PrintResult(Simulator.Run(options.Size, options.Frames, parameters, options.UseHalf));
    }

    Console.WriteLine();
    Console.WriteLine("Disturbance-strength sweep at the current defaults");
    Console.WriteLine("impact\tplayer\twake\tstable\tfailure\tmax height\tmax velocity\troughness");

    foreach (var impactStrength in new[] { 0.3f, 0.7f, 1f, 2f, 4f, 8f })
    {
        var parameters = options.Parameters with { ImpactStrength = impactStrength };
        PrintStrengthResult(Simulator.Run(options.Size, options.Frames, parameters, options.UseHalf));
    }

    foreach (var playerStrength in new[] { 1f, 4f, 7f, 12f, 30f })
    {
        var parameters = options.Parameters with { PlayerDisturbanceStrength = playerStrength };
        PrintStrengthResult(Simulator.Run(options.Size, options.Frames, parameters, options.UseHalf));
    }

    foreach (var wakeDetailGeneration in new[] { 0f, 0.35f, 0.7f, 1f, 2f })
    {
        var parameters = options.Parameters with { WakeDetailGeneration = wakeDetailGeneration };
        PrintStrengthResult(Simulator.Run(options.Size, options.Frames, parameters, options.UseHalf));
    }
}
else
{
    Console.WriteLine("speed\tCFL\tstable\tfailure\tmax height\tmax velocity\troughness");
    PrintResult(Simulator.Run(options.Size, options.Frames, options.Parameters, options.UseHalf));
}

static void PrintResult(Result result)
{
    Console.WriteLine(FormattableString.Invariant($"{result.Parameters.WaveSpeed:0.###}\t{result.Cfl:0.000}\t{result.Stable}\t{result.FailureFrame}\t{result.MaxHeight:0.000000}\t{result.MaxVelocity:0.000000}\t{result.Roughness:0.000000}"));
}

static void PrintStrengthResult(Result result)
{
    Console.WriteLine(FormattableString.Invariant($"{result.Parameters.ImpactStrength:0.###}\t{result.Parameters.PlayerDisturbanceStrength:0.###}\t{result.Parameters.WakeDetailGeneration:0.###}\t{result.Stable}\t{result.FailureFrame}\t{result.MaxHeight:0.000000}\t{result.MaxVelocity:0.000000}\t{result.Roughness:0.000000}"));
}

readonly record struct Parameters(float DeltaTime, float WaveSpeed, float Damping, float ImpactStrength, float PlayerDisturbanceStrength, float WalkSpeed, float WaterWidth, float WaterHeight, float PlayerDisturbanceRadius, float WakeDetailGeneration, float WakeDetailPersistence)
{
    public static Parameters Defaults => new(1f / 72f, 300f, 0.995f, 0.3f, 7f, 1.2f, 5f, 3f, 0.16f, 0.35f, 0.9995f);
}

readonly record struct Result(Parameters Parameters, bool Stable, int FailureFrame, float MaxHeight, float MaxVelocity, float Roughness)
{
    public float Cfl => Parameters.WaveSpeed * Parameters.DeltaTime * Parameters.DeltaTime;
}

sealed class Options
{
    public int Size { get; private set; } = 128;

    public int Frames { get; private set; } = 1800;

    public bool Sweep { get; private set; }

    public bool UseHalf { get; private set; } = true;

    public Parameters Parameters { get; private set; } = Parameters.Defaults;

    public static Options Parse(string[] args)
    {
        var result = new Options();
        var parameters = result.Parameters;

        for (var index = 0; index < args.Length; index++)
        {
            var name = args[index];

            if (name == "--sweep")
                result.Sweep = true;
            else if (name == "--float32")
                result.UseHalf = false;
            else if (name == "--size")
                result.Size = int.Parse(args[++index], CultureInfo.InvariantCulture);
            else if (name == "--frames")
                result.Frames = int.Parse(args[++index], CultureInfo.InvariantCulture);
            else if (name == "--dt")
                parameters = parameters with { DeltaTime = ParseFloat(args[++index]) };
            else if (name == "--wave-speed")
                parameters = parameters with { WaveSpeed = ParseFloat(args[++index]) };
            else if (name == "--damping")
                parameters = parameters with { Damping = ParseFloat(args[++index]) };
            else if (name == "--impact-strength")
                parameters = parameters with { ImpactStrength = ParseFloat(args[++index]) };
            else if (name == "--player-strength")
                parameters = parameters with { PlayerDisturbanceStrength = ParseFloat(args[++index]) };
            else if (name == "--walk-speed")
                parameters = parameters with { WalkSpeed = ParseFloat(args[++index]) };
            else if (name == "--water-width")
                parameters = parameters with { WaterWidth = ParseFloat(args[++index]) };
            else if (name == "--water-height")
                parameters = parameters with { WaterHeight = ParseFloat(args[++index]) };
            else if (name == "--player-radius")
                parameters = parameters with { PlayerDisturbanceRadius = ParseFloat(args[++index]) };
            else if (name == "--wake-generation")
                parameters = parameters with { WakeDetailGeneration = ParseFloat(args[++index]) };
            else if (name == "--wake-persistence")
                parameters = parameters with { WakeDetailPersistence = ParseFloat(args[++index]) };
            else
                throw new ArgumentException($"Unknown argument: {name}");
        }

        result.Parameters = parameters;
        return result;
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }
}

static class Simulator
{
    public static Result Run(int size, int frames, Parameters parameters, bool useHalf)
    {
        var count = size * size;
        var heightIn = new float[count];
        var velocityIn = new float[count];
        var wakeHeightIn = new float[count];
        var wakeVelocityIn = new float[count];
        var heightOut = new float[count];
        var velocityOut = new float[count];
        var wakeHeightOut = new float[count];
        var wakeVelocityOut = new float[count];
        var maxHeight = 0f;
        var maxVelocity = 0f;
        var failureFrame = -1;
        var time = 0f;
        var playerPosition = -parameters.WaterWidth * 0.4f;
        var playerDirection = 1f;

        for (var frame = 0; frame < frames; frame++)
        {
            var damping = MathF.Pow(parameters.Damping, parameters.DeltaTime * 60f);
            var wakePersistence = MathF.Pow(parameters.WakeDetailPersistence, parameters.DeltaTime * 60f);
            var firstImpact = CreateImpact(time, 1.7f, 0f, 3.7f);
            var secondImpact = CreateImpact(time, 2.3f, 0.9f, 41.3f);
            var playerDelta = parameters.WalkSpeed * parameters.DeltaTime * playerDirection;
            playerPosition += playerDelta;

            if (MathF.Abs(playerPosition) >= parameters.WaterWidth * 0.4f)
            {
                playerPosition = Math.Clamp(playerPosition, -parameters.WaterWidth * 0.4f, parameters.WaterWidth * 0.4f);
                playerDirection = -playerDirection;
            }

            var playerUvX = playerPosition / parameters.WaterWidth + 0.5f;
            var playerRadiusUvX = parameters.PlayerDisturbanceRadius / parameters.WaterWidth;
            var playerRadiusUvY = parameters.PlayerDisturbanceRadius / parameters.WaterHeight;
            var playerMotion = frame > 0 ? Math.Clamp(parameters.WalkSpeed / 1.2f, 0f, 1f) : 0f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var height = heightIn[index];
                    var velocity = velocityIn[index];
                    var wakeHeight = wakeHeightIn[index];
                    var wakeVelocity = wakeVelocityIn[index];
                    var left = heightIn[y * size + Math.Max(x - 1, 0)];
                    var right = heightIn[y * size + Math.Min(x + 1, size - 1)];
                    var down = heightIn[Math.Max(y - 1, 0) * size + x];
                    var up = heightIn[Math.Min(y + 1, size - 1) * size + x];
                    var laplacian = left + right + down + up - 4f * height;
                    var wakeLeft = wakeHeightIn[y * size + Math.Max(x - 1, 0)];
                    var wakeRight = wakeHeightIn[y * size + Math.Min(x + 1, size - 1)];
                    var wakeDown = wakeHeightIn[Math.Max(y - 1, 0) * size + x];
                    var wakeUp = wakeHeightIn[Math.Min(y + 1, size - 1) * size + x];
                    var wakeLaplacian = wakeLeft + wakeRight + wakeDown + wakeUp - 4f * wakeHeight;
                    var uvX = (x + 0.5f) / size;
                    var uvY = (y + 0.5f) / size;
                    var source = SampleImpact(firstImpact, uvX, uvY) + SampleImpact(secondImpact, uvX, uvY) * 0.65f;
                    var playerSource = SamplePlayerDisturbance(uvX, uvY, playerUvX, playerRadiusUvX, playerRadiusUvY) * playerMotion;

                    velocity += laplacian * parameters.WaveSpeed * parameters.DeltaTime;
                    velocity += source * parameters.ImpactStrength * parameters.DeltaTime * 30f;
                    velocity += playerSource * parameters.PlayerDisturbanceStrength * parameters.DeltaTime;
                    velocity *= damping;
                    height += velocity * parameters.DeltaTime;

                    wakeVelocity += wakeLaplacian * parameters.WaveSpeed * 0.4f * parameters.DeltaTime;
                    wakeVelocity += source * parameters.ImpactStrength * parameters.WakeDetailGeneration * parameters.DeltaTime * 30f;
                    wakeVelocity += playerSource * parameters.PlayerDisturbanceStrength * parameters.WakeDetailGeneration * parameters.DeltaTime;
                    wakeVelocity *= wakePersistence;
                    wakeHeight += wakeVelocity * parameters.DeltaTime;

                    heightOut[index] = Store(height, useHalf);
                    velocityOut[index] = Store(velocity, useHalf);
                    wakeHeightOut[index] = Store(wakeHeight, useHalf);
                    wakeVelocityOut[index] = Store(wakeVelocity, useHalf);
                    maxHeight = MathF.Max(maxHeight, MathF.Abs(heightOut[index] + wakeHeightOut[index]));
                    maxVelocity = MathF.Max(maxVelocity, MathF.Max(MathF.Abs(velocityOut[index]), MathF.Abs(wakeVelocityOut[index])));
                }
            }

            if (!AllFinite(heightOut, velocityOut, wakeHeightOut, wakeVelocityOut) || maxHeight > 1000f || maxVelocity > 1000f)
            {
                failureFrame = frame;
                break;
            }

            (heightIn, heightOut) = (heightOut, heightIn);
            (velocityIn, velocityOut) = (velocityOut, velocityIn);
            (wakeHeightIn, wakeHeightOut) = (wakeHeightOut, wakeHeightIn);
            (wakeVelocityIn, wakeVelocityOut) = (wakeVelocityOut, wakeVelocityIn);
            time += parameters.DeltaTime;
        }

        var roughness = ComputeRoughness(heightIn, wakeHeightIn, size);
        return new Result(parameters, failureFrame < 0, failureFrame, maxHeight, maxVelocity, roughness);
    }

    private static Impact CreateImpact(float time, float period, float offset, float seed)
    {
        var clock = time + offset;
        var cycle = MathF.Floor(clock / period);
        var age = clock - cycle * period;
        var centerX = Mix(0.12f, 0.88f, Hash11(cycle + seed));
        var centerY = Mix(0.12f, 0.88f, Hash11(cycle + seed + 19.19f));
        var temporalPulse = MathF.Exp(-MathF.Pow(age / 0.035f, 2f));
        return new Impact(centerX, centerY, temporalPulse);
    }

    private static float SampleImpact(Impact impact, float uvX, float uvY)
    {
        var deltaX = uvX - impact.CenterX;
        var deltaY = uvY - impact.CenterY;
        var spatialPulse = MathF.Exp(-(deltaX * deltaX + deltaY * deltaY) / (0.012f * 0.012f));
        return impact.TemporalPulse * spatialPulse;
    }

    private static float SamplePlayerDisturbance(float uvX, float uvY, float playerUvX, float playerRadiusUvX, float playerRadiusUvY)
    {
        var deltaX = (uvX - playerUvX) / playerRadiusUvX;
        var deltaY = (uvY - 0.5f) / playerRadiusUvY;
        var radius2 = deltaX * deltaX + deltaY * deltaY;
        return (radius2 - 1f) * MathF.Exp(-radius2);
    }

    private static float Store(float value, bool useHalf)
    {
        return useHalf ? (float)(Half)value : value;
    }

    private static bool AllFinite(params float[][] fields)
    {
        foreach (var field in fields)
        {
            foreach (var value in field)
            {
                if (!float.IsFinite(value))
                    return false;
            }
        }

        return true;
    }

    private static float ComputeRoughness(float[] height, float[] wakeHeight, int size)
    {
        var sum = 0d;
        var count = 0;

        for (var y = 0; y < size - 1; y++)
        {
            for (var x = 0; x < size - 1; x++)
            {
                var index = y * size + x;
                var value = height[index] + wakeHeight[index];
                var deltaX = height[index + 1] + wakeHeight[index + 1] - value;
                var deltaY = height[index + size] + wakeHeight[index + size] - value;
                sum += deltaX * deltaX + deltaY * deltaY;
                count += 2;
            }
        }

        return count == 0 ? 0f : (float)Math.Sqrt(sum / count);
    }

    private static float Hash11(float value)
    {
        var raw = MathF.Sin(value * 127.1f) * 43758.5453f;
        return raw - MathF.Floor(raw);
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static float Mix(float from, float to, float amount)
    {
        return from + (to - from) * amount;
    }

    private readonly record struct Impact(float CenterX, float CenterY, float TemporalPulse);
}
