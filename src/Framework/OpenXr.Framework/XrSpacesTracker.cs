using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSpaceTrackInfo
    {
        public TimeSpan Interval { get; set; }

        public Space Space { get; set; }

        public XrSpaceLocation? LastLocation { get; set; }

        public DateTime LastUpdateTime { get; set; }
    }

    public class XrSpacesTracker
    {
        readonly Dictionary<Space, XrSpaceTrackInfo> _trackData = [];
        readonly XrApp _app;

        public XrSpacesTracker(XrApp app)
        {
            _app = app;
        }

        public void Add(Space space, TimeSpan updateInterval)
        {
            if (!_trackData.TryGetValue(space, out var info))
            {
                info = new XrSpaceTrackInfo
                {
                    Space = space,
                    Interval = updateInterval,
                };
                _trackData[space] = info;
            }
            else
                info.Interval = new TimeSpan(Math.Min(info.Interval.Ticks, updateInterval.Ticks));
        }

        public void Remove(Space space)
        {
            _trackData.Remove(space);
        }

        public void Update(Space baseSpace, long time)
        {
            var now = DateTime.Now;

            /*
            var spaces = _trackData.Values
                .Where(a => now > a.LastUpdateTime + a.Interval)
                .Select(a => a.Space)
                .ToArray();

            var result = _app.LocateSpaces(spaces, baseSpace, time);

            for (var i = 0; i < result.Length; i++)
            {
                var data = _trackData[spaces[i]];
                data.LastLocation = result[i];
                data.LastUpdateTime = now;
            }
            */

            foreach (var item in _trackData.Values)
            {
                if (now < item.LastUpdateTime + item.Interval)
                    continue;
                item.LastLocation = _app.LocateSpace(item.Space, baseSpace, time);
                item.LastUpdateTime = now;
            }

        }


        public XrSpaceLocation? GetLastLocation(Space space)
        {
            if (_trackData.TryGetValue(space, out var info))
                return info.LastLocation;
            return null;
        }

        public void Clear()
        {
            _trackData.Clear();
        }
    }
}
