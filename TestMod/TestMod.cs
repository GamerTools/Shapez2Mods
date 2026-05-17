using Core.Dependency;
using Cysharp.Threading.Tasks;
using Game.Content.Features.Trains.Predictions;
using Game.Core.Coordinates;
using Game.Core.Trains;
using Game.Orchestration;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using ShapezShifter.Flow;
using ShapezShifter.Hijack;
using ShapezShifter.Kit;
using ShapezShifter.SharpDetour;
using ShapezShifter.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using static ShapezShifter.Flow.ModConsoleCommandsCreator;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class MyMod : IMod
{
    public static ILogger logger;
    private ModConsoleCommandsCreator.ModConsoleRewirer consoleRewirer;

    public MyMod(ILogger logger)
    {
        MyMod.logger = logger;

        MyMod.logger.Info.Log("Hello from Test Mod!");

        // Initialize ClassInspector with logger
        ClassInspector.SetLogger(logger);

        consoleRewirer = this.AddModCommands();
        AddConsoleCommands();
        CreateHooks();
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
                //var savegameManager = GameDependencyContainer.Resolve<ISavegameManager>();
                //var gameSessionOrchestrator = dependencyContainer.Resolve<GameSessionOrchestrator>();
            });

            console.Register("getview", context =>
            {
                var camera = SessionDependencyContainer.Resolve<CameraController>();
                var pos = camera.CurrentPosition;
                var view = GameHelper.Core.Viewport;
                //context.Output($"viewport: {v1.Position.x},{v1.Position.y} {v1.Angle} {v1.RotationDegrees}");
                context.Output($"camera x,y: {pos.x},{pos.y}");
                context.Output($"camera rotation,angle: {camera.TargetRotationDegrees},{camera.TargetAngle}");
                context.Output($"viewport zoom: {view.TargetZoom}");
            });

            var optX = new DebugConsole.FloatOption("x", -100.0f, 100.0f);
            var optY = new DebugConsole.FloatOption("y", -100.0f, 100.0f);
            console.Register("setview", optX, optY, context =>
            {
                var camera = SessionDependencyContainer.Resolve<CameraController>();
                var pos = camera.CurrentPosition;
                context.Output($"before: {pos.x},{pos.y} {camera.TargetRotationDegrees}");
                camera.CurrentPosition = new double2(optX.Value, optY.Value);
                pos = camera.CurrentPosition;
                context.Output($"after: {pos.x},{pos.y} {camera.TargetRotationDegrees}");
            });

            console.Register("inspect", context =>
            {
                context.Output("Inspecting GameHelper...");
                ClassInspector.DisplayClassMembers(typeof(GameHelper), null, maxDepth: 3);
                context.Output("Inspection complete. Check logs for details.");
            });

            console.Register("allglobal", context =>
            {
                context.Output("Inspecting globals...");
                ClassInspector.DisplayClassMembers(typeof(GlobalsData), globals, maxDepth: 3);
                context.Output("Inspection complete. Check logs for details.");
            });

            console.Register("inspectmod", context =>
            {
                context.Output("Inspecting MyMod instance...");
                ClassInspector.DisplayClassMembers(typeof(MyMod), this, maxDepth: 1);
                context.Output("Inspection complete. Check logs for details.");
            });

            console.Register("listdeps", context =>
            {
                context.Output("Listing DependencyContainer classes...");
                ClassInspector.LogDependencyContainerClasses(GameDependencyContainer);
                ClassInspector.LogDependencyContainerClasses(SessionDependencyContainer);
                context.Output("Listing complete. Check logs for details.");
            });
        });
    }

    private Hook GameInitHook;
    private Hook SessionInitHook;
    private Hook PIHook;
    private GlobalsData globals;
    private DependencyContainer GameDependencyContainer;
    private DependencyContainer SessionDependencyContainer;

    public void CreateHooks()
    {
        MyMod.logger.Info.Log("##### Making Hooks");

        GameInitHook = DetourHelper.CreatePostfixHook<GameOrchestrator, UniTask>(
            original: orchestrator => orchestrator.InitializeMainMenu(),
            postfix: GetGameData);
        SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameStartOptions, GlobalsData, IGameData>(
            original: (orchestrator, gameStartOptions, globals, gameData) => orchestrator.Init(gameStartOptions, globals, gameData),
            postfix: GetSessionData);
        PIHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameData, CameraGameSettings, Keybindings>(
            original: (orchestrator, gameData, cameraSettings, keybindings) => orchestrator.Init_6_PlayerInteraction(gameData, cameraSettings, keybindings),
            postfix: GetPIData);
    }

    private UniTask GetGameData(GameOrchestrator orchestrator, UniTask result)
    {
        this.GameDependencyContainer = orchestrator.InitializationDependencyContainer;
        return result;
    }

    private void GetSessionData(GameSessionOrchestrator orchestrator, IGameStartOptions gameStartOptions, GlobalsData globals, IGameData gameData)
    {
        this.globals = globals;
        this.SessionDependencyContainer = orchestrator.DependencyContainer;
    }

    private void GetPIData(GameSessionOrchestrator orchestrator, IGameData gameData, CameraGameSettings cameraSettings, Keybindings keybindings)
    {
        orchestrator.DependencyContainer.Bind<CameraController>().To(orchestrator.PlayerInteractionOrchestrator.CameraController);
    }

    public void Dispose()
    {
        GameInitHook?.Dispose();
        SessionInitHook?.Dispose();
        PIHook?.Dispose();
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
