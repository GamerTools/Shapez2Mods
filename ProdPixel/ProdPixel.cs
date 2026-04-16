using Game.Core.Coordinates;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Unity.Mathematics;

using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class ProdPixel : IMod
{
    public static ILogger logger;
    private Hook SIPRHook;

    public ProdPixel(ILogger logger)
    {
        ProdPixel.logger = logger;
        ProdPixel.logger.Info.Log("Hello from Prod Pixel!");
        CreateHooks();
    }

    public void CreateHooks()
    {
        BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        ProdPixel.logger.Info.Log("##### Making Hooks");

        SIPRHook = new Hook(
            typeof(SandboxItemProducerSimulationRenderer).GetMethod("OnDrawDynamic", FLAGS),
            typeof(ProdPixel).GetMethod("OnDrawDynamic", FLAGS));

        //CreateSIPRHook = Replace
        //    <SandboxItemProducerSimulationRenderer, StatelessBuildingSimulationRenderer<ItemProducerSimulation, SandboxItemProducerMetaBuildingDefinition.DrawData>.Entity, FrameDrawOptions>(
        //        (renderer, entity, options) => renderer.OnDrawDynamic(entity, options),
        //        OnDrawDynamic;
    }

    public static void OnDrawDynamic(
        SandboxItemProducerSimulationRenderer self,
        in StatelessBuildingSimulationRenderer<ItemProducerSimulation, SandboxItemProducerMetaBuildingDefinition.DrawData>.Entity entity,
        FrameDrawOptions options)
    {
        self.DrawBeltItem(in entity.Transform, options, entity.Simulation.OutputLane, entity.DrawData.OutputLaneRenderingDefinition);
        ItemProducerConfiguration itemProducerConfiguration = (ItemProducerConfiguration)entity.Building.Configuration;
        if (itemProducerConfiguration.ResourceItem != null)
        {
            options.Renderers.Shapes.Add(
                options.Renderers.BeltItems.GetDrawData(itemProducerConfiguration.ResourceItem, options.LOD.ShapeLOD),  // 2
                FastMatrix.TranslateScale(new LocalVector(0f, 0f, 0.66f) * entity.Transform, new float3(3.6f, 3.6f, 3.6f)));
        }
    }

    public void Dispose()
    {
        SIPRHook.Dispose();
    }
}

