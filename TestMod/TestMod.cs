using Game.Core.Trains;
using Game.Core.Coordinates;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using ShapezShifter.Flow;
using ShapezShifter.Hijack;
using ShapezShifter.SharpDetour;
using ShapezShifter.Utilities;
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

    public MyMod(ILogger logger)
    {
        MyMod.logger = logger;

        MyMod.logger.Info.Log("Hello from Test Mod!");

        // Initialize ClassInspector with logger
        ClassInspector.SetLogger(logger);

        consoleRewirer = this.AddModCommands();
        AddConsoleCommands();

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
                context.Output($"Parts: {GameHelper.Core.Mode.ShapesConfiguration.PartCount}");
            });

            // Example: Display class members recursively
            console.Register("inspect", context =>
            {
                //var core = GameHelper.Core;
                //context.Output("Inspecting SandboxItemProducerSimulationRenderer...");
                context.Output("Inspecting GameHelper...");
                ClassInspector.DisplayClassMembers(typeof(GameHelper), null, maxDepth: 3);
                context.Output("Inspection complete. Check logs for details.");
            });

            // Example: Inspect an instance with values
            console.Register("inspectmod", context =>
            {
                context.Output("Inspecting MyMod instance...");
                ClassInspector.DisplayClassMembers(typeof(MyMod), this, maxDepth: 1);
                context.Output("Inspection complete. Check logs for details.");
            });
        });
    }

    public void CreateHooks()
    {
        BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        MyMod.logger.Info.Log("##### Making Hooks");

        SIPRHook = new Hook(
            typeof(SandboxItemProducerSimulationRenderer).GetMethod("OnDrawDynamic", FLAGS),
            typeof(MyMod).GetMethod("OnDrawDynamic", FLAGS));
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

    }

}
