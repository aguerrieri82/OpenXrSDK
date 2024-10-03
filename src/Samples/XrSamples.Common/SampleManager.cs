using System.Reflection;
using XrEngine.OpenXr;

namespace XrSamples
{
    public class AppSample
    {
        public string? Name { get; set; }

        public Func<XrEngineAppBuilder, XrEngineAppBuilder>? Build { get; set; }
    }

    public class HDRInfo
    {
        public string? Name { get; set; }

        public string? Uri { get; set; }
    }

    public class SampleManager
    {
        protected IList<AppSample>? _samples;

        public IEnumerable<HDRInfo> GetHDRs()
        {
            yield return new HDRInfo
            {
                Name = "Pisa",
                Uri = "res://asset/Envs/pisa.hdr"
            };
            yield return new HDRInfo
            {
                Name = "Camera",
                Uri = "res://asset/Envs/CameraEnv.jpg"
            };
            yield return new HDRInfo
            {
                Name = "Neutral",
                Uri = "res://asset/Envs/neutral.hdr"
            };
            yield return new HDRInfo
            {
                Name = "Light Room",
                Uri = "res://asset/Envs/lightroom_14b.hdr"
            };

            yield return new HDRInfo
            {
                Name = "Court",
                Uri = "res://asset/Envs/footprint_court.hdr"
            };
        }

        public IList<AppSample> List()
        {
            if (_samples == null)
            {
                _samples = new List<AppSample>();
                foreach (var method in typeof(SampleScenes).GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    var sample = method.GetCustomAttribute<SampleAttribute>();
                    if (sample == null)
                        continue;
                    _samples.Add(new AppSample
                    {
                        Name = sample.Name,
                        Build = (XrEngineAppBuilder builder) =>
                        {
                            method.Invoke(null, [builder]);
                            return builder;
                        }
                    });
                }
            }

            return _samples;
        }

        public AppSample GetSample(string name)
        {
            return List().First(a => a.Name == name);
        }
    }
}
