using System.Numerics;
using Tensorflow;
using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;
using XrMath;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;

namespace XrEngine.AI
{
    public class PoseTrainData
    {
        public float Time { get; set; }

        public Pose3 Pose { get; set; }
    }

    public class AIPosePredictorCore 
    {
        const int _featureSize = 8;

        private IModel? _model;
        
        private string _modelPath = "d:\\pose_prediction_model";

        private int _sequenceLength;


        public AIPosePredictorCore(int sequenceLength)
        {
            _sequenceLength = sequenceLength;
        }

        public void Train(List<PoseTrainData> poses)
        {
            // Prepare training and validation datasets
            var (xTrain, yTrain) = PrepareDataset(poses);
            var (xVal, yVal) = PrepareDataset(poses.GetRange(poses.Count - 60, 60));

            // Create or load the model
            if (File.Exists($"{_modelPath}.pb"))
            {
                Console.WriteLine("Loading saved model...");
                _model = keras.models.load_model(_modelPath);
            }
            else
            {
                Console.WriteLine("Creating a new model...");
                _model = CreateModel();
            }

            // Train the model
            Console.WriteLine("Training the model...");
            _model.fit(xTrain, yTrain, batch_size: 32, epochs: 20, validation_data: (xVal, yVal), verbose: 2);

            // Save the model after training
            _model.save(_modelPath);
            _model.save_weights(_modelPath + ".h5");

            Console.WriteLine("Model training complete and saved.");
        }

        public PoseTrainData Predict(IList<PoseTrainData> poseSequence)
        {
            if (poseSequence.Count < _sequenceLength)
                throw new ArgumentException($"At least {_sequenceLength} poses are required for prediction.");

            if (_model == null)
            {
                if (!File.Exists($"{_modelPath}/saved_model.pb"))
                    throw new InvalidOperationException("Model not found. Train the model before making predictions.");
                _model = keras.models.load_model(_modelPath);
                _model.load_weights(_modelPath + ".h5");
            }

            // Prepare input for prediction
            var xInput = PrepareInputForPrediction(poseSequence);
            var lastPose = poseSequence[^1];

            // Predict the next pose
            var prediction = _model.predict(xInput).First();
            var outputArray = prediction.numpy();

            // Construct the next pose
            var deltaPos = new Vector3((float)outputArray[0, 0], (float)outputArray[0, 1], (float)outputArray[0, 2]);
            var deltaOrient = new Quaternion((float)outputArray[0, 3], (float)outputArray[0, 4], (float)outputArray[0, 5], (float)outputArray[0, 6]);
            var deltaTime = (float)outputArray[0, 7];

            return new PoseTrainData
            {
                Pose = new Pose3
                {
                    Position = lastPose.Pose.Position + deltaPos,
                    Orientation = lastPose.Pose.Orientation * deltaOrient,
                },
                Time = lastPose.Time + deltaTime
            };
        }

        private Sequential CreateModel()
        {
            var model = keras.Sequential(
            [
                keras.layers.InputLayer(input_shape: new Shape(_sequenceLength, _featureSize)),
                keras.layers.LSTM(128, return_sequences: false),
                keras.layers.Dense(64, activation: keras.activations.Relu),
                keras.layers.Dense(_featureSize)
            ]);

            model!.compile(
                    optimizer: keras.optimizers.Adam(learning_rate: 0.001f),
                    loss: keras.losses.MeanAbsoluteError(),
                    metrics: ["mean_absolute_error"]
            );


            return model;
        }

        public static float[,] ConvertListTo2DArray(List<float[]> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("The input list is null or empty.");

            int rows = list.Count;
            int cols = list[0].Length;

            float[,] array = new float[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                if (list[i].Length != cols)
                    throw new ArgumentException("All rows in the input list must have the same number of columns.");

                for (int j = 0; j < cols; j++)
                {
                    array[i, j] = list[i][j];
                }
            }

            return array;
        }


        private (NDArray, NDArray) PrepareDataset(List<PoseTrainData> poses)
        {
            int batchSize = poses.Count - _sequenceLength - 1;
            var xData = new float[batchSize, _sequenceLength, _featureSize];
            var yData = new float[batchSize, _featureSize];

            for (int i = 0; i < batchSize; i++)
            {
                int poseIdx;

                for (int j = 0; j < _sequenceLength; j++)
                {
                    poseIdx = i + j;

                    var deltaPosition = poses[poseIdx + 1].Pose.Position - poses[poseIdx].Pose.Position;
                    var deltaTime = poses[poseIdx + 1].Time - poses[poseIdx].Time;
                    var deltaOrientation = Quaternion.Inverse(poses[poseIdx].Pose.Orientation) * poses[poseIdx + 1].Pose.Orientation;

                    xData[i, j, 0] = deltaPosition.X;
                    xData[i, j, 1] = deltaPosition.Y;
                    xData[i, j, 2] = deltaPosition.Z;
                    xData[i, j, 3] = deltaOrientation.X;
                    xData[i, j, 4] = deltaOrientation.Y;
                    xData[i, j, 5] = deltaOrientation.Z;
                    xData[i, j, 6] = deltaOrientation.W;
                    xData[i, j, 7] = deltaTime;
                }

                poseIdx = i + _sequenceLength;

                var targetDeltaPos = poses[poseIdx + 1].Pose.Position - poses[poseIdx].Pose.Position;
                var targetDeltaTime = poses[poseIdx + 1].Time - poses[poseIdx].Time;
                var targetDeltaOrientation = Quaternion.Inverse(poses[poseIdx].Pose.Orientation) * poses[poseIdx + 1].Pose.Orientation;

                yData[i, 0] = targetDeltaPos.X;
                yData[i, 1] = targetDeltaPos.Y;
                yData[i, 2] = targetDeltaPos.Z;
                yData[i, 3] = targetDeltaOrientation.X;
                yData[i, 4] = targetDeltaOrientation.Y;
                yData[i, 5] = targetDeltaOrientation.Z;
                yData[i, 6] = targetDeltaOrientation.W;
                yData[i, 7] = targetDeltaTime;
            }

            return (np.array(xData), np.array(yData));
        }

        private NDArray PrepareInputForPrediction(IList<PoseTrainData> poseSequence)
        {
            var xInput = new float[1, _sequenceLength, _featureSize];

            for (int j = 0; j < _sequenceLength; j++)
            {
                var deltaPosition = poseSequence[j + 1].Pose.Position - poseSequence[j].Pose.Position;
                var deltaTime = poseSequence[j + 1].Time - poseSequence[j].Time;
                var deltaOrientation = Quaternion.Inverse(poseSequence[j].Pose.Orientation) * poseSequence[j + 1].Pose.Orientation;

                xInput[0, j, 0] = deltaPosition.X;
                xInput[0, j, 1] = deltaPosition.Y;
                xInput[0, j, 2] = deltaPosition.Z;
                xInput[0, j, 3] = deltaOrientation.X;
                xInput[0, j, 4] = deltaOrientation.Y;
                xInput[0, j, 5] = deltaOrientation.Z;
                xInput[0, j, 6] = deltaOrientation.W;
                xInput[0, j, 7] = deltaTime;
            }

            return np.array(xInput);
        }
    }
}
