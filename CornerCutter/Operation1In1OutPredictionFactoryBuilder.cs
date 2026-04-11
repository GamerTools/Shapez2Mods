using Core.Factory;
using Game.Content.Features.Predictions.Processing;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Hijack.Predictions;

internal class CornerCutterPredictionFactoryBuilder
    : IBuildingPredictionFactoryBuilder<Processing1In1OutPredictionSimulation>
{
    public IFactory<Processing1In1OutPredictionSimulation> BuildFactory(PredictionSystemsDependencies dependencies)
    {
        var op = new ShapeOperationCornerCut(
            dependencies.Mode.MaxShapeLayers,
            dependencies.ShapeRegistry,
            dependencies.ShapeIdManager);
        return new Processing1In1OutPredictionSimulationFactory(op);
    }
}
