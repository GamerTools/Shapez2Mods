using Game.Core.Trains;
using Game.Core.Coordinates;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using ShapezShifter.Flow;
using ShapezShifter.Hijack;
using ShapezShifter.SharpDetour;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Mathematics;
using static ShapezShifter.Flow.ModConsoleCommandsCreator;

using ILogger = Core.Logging.ILogger;
using Game.Content.Features.Trains.Predictions;
using ShapezShifter.Kit;

[UsedImplicitly]
public class MyMod : IMod
{
    public static ILogger logger;
    private Hook SIPRHook;
    private ModConsoleCommandsCreator.ModConsoleRewirer consoleRewirer;
    private GameMode Mode;

    public MyMod(ILogger logger)
    {
        MyMod.logger = logger;

        MyMod.logger.Info.Log("Hello from Test Mod!");

        consoleRewirer = this.AddModCommands();
        AddConsoleCommands();
        //Mode = GameHelper.Core.Mode;

        //CreateHooks();
    }

    public void AddConsoleCommands()
    {
        consoleRewirer.AddCommand(console =>
        {
            console.Register("hello", context =>
            {
                context.Output("Hello World!");
            });
            console.Register("test", context =>
            {
                context.Output($"Layers: {GameHelper.Core.Mode.Scenario.ResearchConfig.MaxShapeLayers}");
                context.Output($" Parts: {GameHelper.Core.Mode.ShapesConfiguration.PartCount}");
            });
        });
    }

    /*
    public void AddTrainCommands() {
        ITrainRegistry trainRegistry;
        consoleRewirer.AddCommand(console =>
        {
            console.Register("listtrains", context =>
            {
                if (trainRegistry == null)
                {
                    context.Output("Train registry is not available.");
                    return;
                }

                var trains = trainRegistry.GetAllTrains();
                if (trains == null || !trains.Any())
                {
                    context.Output("No trains found in the game.");
                    return;
                }

                context.Output($"Found {trains.Count()} train(s):");
                int index = 1;
                foreach (var train in trains)
                {
                    context.Output($"  {index}. Train ID: {train.Id}, Cars: {train.CarCount}");
                    index++;
                }
            });
        });
    }
    */

    public void CreateHooks()
    {
        BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        MyMod.logger.Info.Log("##### Making Hooks");

        SIPRHook = new Hook(
            typeof(SandboxItemProducerSimulationRenderer).GetMethod("OnDrawDynamic", FLAGS),
            typeof(MyMod).GetMethod("OnDrawDynamic", FLAGS));

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

    public static Hook Replace<TObject, TArg0, TArg1>(Expression<Action<TObject, TArg0, TArg1>> original, Action<TObject, TArg0, TArg1> replacement)
    {
        MethodInfo runtimeMethod = DetourHelper.GetRuntimeMethod<TObject>(original);
        return new Hook(runtimeMethod, replacement);
    }

    public void Dispose()
    {
        SIPRHook?.Dispose();
        consoleRewirer?.Dispose();
    }
}

class Stuff
{
    Stuff(ILogger logger) {
#if DEBUG_OFF
        logger.Info.Log("Waiting for debugger to attach...");
        while (!System.Diagnostics.Debugger.IsAttached)
        {
            System.Threading.Thread.Sleep(100);
        }
        logger.Info.Log("Debugger attached!");
        System.Diagnostics.Debugger.Break(); // Optional: Break immediately
#endif
    Type classType = typeof(SandboxItemProducerSimulationRenderer);

    // Methods
    MethodInfo[] methods = classType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (methods.Length > 0)
        {
            logger.Info.Log($"\n--- Methods ({methods.Length}) ---");
            foreach (MethodInfo method in methods)
            {
                if (method.IsSpecialName) continue; // Skip property getters/setters

                string accessibility = method.IsPublic ? "public" : method.IsPrivate ? "private" : "protected";
    string modifier = method.IsStatic ? "static " : method.IsVirtual ? "virtual " : "";
    ParameterInfo[] methodParams = method.GetParameters();
    string paramStr = string.Join(", ", methodParams.Select(p =>
    {
        string modifier = p.IsIn ? "in " : p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "";
        string typeName = p.ParameterType.IsByRef ? p.ParameterType.GetElementType().Name : p.ParameterType.Name;
        return $"{modifier}{typeName} {p.Name}";
    }));
    logger.Info.Log($"  {accessibility} {modifier}{method.ReturnType.Name} {method.Name}({paramStr})");
            }
        }

        MethodInfo method1 = classType.GetMethod("OnDrawDynamic");
if (method1 == null)
{
    logger.Error.Log("Failed to find method OnDrawDynamic in SandboxItemProducerSimulationRenderer.");
    return;
}

        //ParameterInfo[] parameters = method.GetParameters();

        //logger.Info.Log($"Method: {method.Name}");
        //logger.Info.Log($"Return Type: {method.ReturnType.FullName}");
        //logger.Info.Log($"Parameter Count: {parameters.Length}");

        //for (int i = 0; i < parameters.Length; i++)
        //{
        //    ParameterInfo param = parameters[i];
        //    logger.Info.Log($"Parameter {i}:");
        //    logger.Info.Log($"  Name: {param.Name}");
        //    logger.Info.Log($"  Type: {param.ParameterType.FullName}");
        //    logger.Info.Log($"  IsIn: {param.IsIn}");
        //    logger.Info.Log($"  IsOut: {param.IsOut}");
        //    logger.Info.Log($"  IsByRef: {param.ParameterType.IsByRef}");
        //    logger.Info.Log($"  Position: {param.Position}");
        //}
        //new SIPRInterceptor();
    }

}
