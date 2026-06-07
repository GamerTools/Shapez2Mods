using Game.Core.Coordinates;
using Game.Core.Trains;
using ShapezShifter.Kit;
using Unity.Mathematics;
using ILogger = Core.Logging.ILogger;

namespace TrainView
{
    public static class TrainSimulationHelper
    {
        private static ILogger _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public static (double2 position, float rotation) ApproxTrainPosition(TrainId trainId)
        {
            TrainData trainData = Main.trainSim.GetTrainData(trainId);

            // Get the incoming and outgoing track positions
            //var incoming = trainData.Head.Incoming;
            var outgoing = trainData.Head.Outgoing;

            // Convert to world coordinates
            //var inPos = incoming.Position.ToOrigin_G();
            var outPos = outgoing.Position.ToOrigin_G();

            // Set position to center of grid
            double2 position = new double2(outPos.x + 10, outPos.y + 10);
            float rotation = GetRotationFromDirection(outgoing.Direction);

            return (position, rotation);
        }

        private static float GetRotationFromDirection(ChunkDirection direction)
        {
            switch (direction.Value)
            {
                case ChunkDirection.Serializable.North:
                    return 0f;
                case ChunkDirection.Serializable.East:
                    return 90f;
                case ChunkDirection.Serializable.South:
                    return 180f;
                case ChunkDirection.Serializable.West:
                    return 270f;
                default:
                    return 0f;
            }
        }
    }
}
