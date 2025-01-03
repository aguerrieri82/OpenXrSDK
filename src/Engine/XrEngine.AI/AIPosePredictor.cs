﻿using XrEngine.OpenXr;
using XrMath;

namespace XrEngine.AI
{
    public class AIPosePredictor : IPosePredictor
    {
        readonly AIPosePredictorModel _core;
        readonly List<PoseTrainData> _data = [];

        public AIPosePredictor(string modelPath)
        {
            _core = new AIPosePredictorModel(7, modelPath);
        }

        public Pose3 Predict(float dt)
        {
            if (_data.Count < 8)
                throw new InvalidOperationException("Not enough data to predict");
            var pred = _core.Predict(_data);
            return pred.Pose;
        }

        public void Track(Pose3 pose, float time)
        {
            if (_data.Count > 8)
                _data.RemoveAt(0);

            _data.Add(new PoseTrainData
            {
                Pose = pose,
                Time = time
            });
        }
    }
}
