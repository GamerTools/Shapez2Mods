using Core.Factory;

public class CornerCutterSimulationFactory : IFactory<CornerCutterSimulationState, CornerCutterSimulation>
{
    private readonly ICornerCutterConfiguration Configuration;
    private readonly ShapeOperationCornerCut CornerCut;
    private readonly IShapeRegistry ShapeRegistry;

    public CornerCutterSimulationFactory(
        ICornerCutterConfiguration configuration,
        IShapeRegistry shapeRegistry,
        ShapeOperationCornerCut cornerCut)
    {
        Configuration = configuration;
        ShapeRegistry = shapeRegistry;
        CornerCut = cornerCut;
    }

    public CornerCutterSimulation Produce(CornerCutterSimulationState simulationState)
    {
        return new CornerCutterSimulation(simulationState, Configuration, ShapeRegistry, CornerCut);
    }
}

public interface ICornerCutterConfiguration
{
    public BeltSpeed BeltSpeed { get; }
    public BeltDelay ProcessingDelay { get; }
}
