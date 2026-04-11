using Core.Factory;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Hijack;

internal class CornerCutterFactoryBuilder
    : IBuildingSimulationFactoryBuilder<CornerCutterSimulation, CornerCutterSimulationState,
        CornerCutterConfiguration>
{
    public IFactory<CornerCutterSimulationState, CornerCutterSimulation> BuildFactory(
        SimulationSystemsDependencies dependencies,
        out CornerCutterConfiguration config)
    {
        config = new CornerCutterConfiguration(
            BuffableBeltSpeed.DiscreteSpeed.OneSecondPerTile,
            BuffableBeltDelay.DiscreteDuration.OnePointFiveSeconds,
            new ResearchSpeedId("CutterSpeed"));

        var cornerCut = new ShapeOperationCornerCut(
            dependencies.Mode.MaxShapeLayers,
            dependencies.ShapeRegistry,
            dependencies.ShapeIdManager);

        return new CornerCutterSimulationFactory(config, dependencies.ShapeRegistry, cornerCut);
    }
}
