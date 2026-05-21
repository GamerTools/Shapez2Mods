using System;
using System.Linq;
using Core.Dependency;
using Cysharp.Threading.Tasks;
using Game.Core.Trains;
using Game.Orchestration;
using MonoMod.RuntimeDetour;
using ShapezShifter.Flow;
using ShapezShifter.Kit;
using ShapezShifter.SharpDetour;
using ShapezShifter.Utilities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static ShapezShifter.Flow.ModConsoleCommandsCreator;
using ILogger = Core.Logging.ILogger;

public class MyMod : IMod
{
    public static ILogger logger;
    
    private ModConsoleCommandsCreator.ModConsoleRewirer consoleRewirer;
    private Hook GameInitHook;
    private Hook SessionInitHook;
    private Hook CameraHook;
    private Hook BuildingsHook;
    private GameObject myGameObject;

    public static GlobalsData Globals;
    public static DependencyContainer GameDependencyContainer;
    public static DependencyContainer SessionDependencyContainer;

    public MyMod(ILogger logger)
    {
        MyMod.logger = logger;

        MyMod.logger.Info.Log("Hello from Test Mod!");

        // Initialize ClassInspector with logger
        ClassInspector.SetLogger(logger);

        MyGameObject.SetLogger(logger);
        myGameObject = new GameObject("MyGameObject");
        myGameObject.AddComponent<MyGameObject>();
        GameObject.DontDestroyOnLoad(myGameObject);

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
                var mode = GameHelper.Core.Mode;
                context.Output($"Layers: {mode.Scenario.ResearchConfig.MaxShapeLayers}");
                context.Output($"Parts: {mode.ShapesConfiguration.PartCount}");

                var bs = GameHelper.Core.LocalPlayer.InteractionState.BuildingSelection;
                context.Output($"Number of selected machines: {bs.Count}");

                var buildings = SessionDependencyContainer.Resolve<BuildingsModulesLookup>().BuildingSimulationData;
                MyMod.logger.Info.Log("List of buildings:");
                foreach (var building in buildings)
                {
                    MyMod.logger.Info.Log($"{building.Key}: {building.Value}");
                }

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
                ClassInspector.DisplayClassMembers(typeof(GlobalsData), Globals, maxDepth: 3);
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

            console.Register("listasm", new DebugConsole.StringOption("name"), context =>
            {
                string assemblyName = context.GetString(0);
                context.Output($"Listing types in assembly '{assemblyName}'...");
                ClassInspector.LogAssemblyTypesByName(assemblyName);
                context.Output("Listing complete. Check logs for details.");
            });

            console.Register("listasmall", context =>
            {
                context.Output("Listing all loaded assemblies...");
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .OrderBy(a => a.GetName().Name)
                    .ToList();

                context.Output($"Total loaded assemblies: {assemblies.Count}");
                foreach (var asm in assemblies)
                {
                    context.Output($"  {asm.GetName().Name}");
                }
            });

            console.Register("listasmwiki", context =>
            {
                context.Output("Generating MediaWiki documentation for all game assemblies...");
                context.Output("This may take a moment. Check logs for output.");
                ClassInspector.LogAllAssembliesAsMediaWiki();
                context.Output("MediaWiki documentation generated. Check logs for output.");
            });

            console.Register("listtrains", context =>
            {
                context.Output("Listing trains...");
                var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
                var trainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
                var trains = trainSim.GetAllTrains(Allocator.Temp);
                foreach ( var trainId in trains )
                {
                    var trainData = trainSim.GetTrainData(trainId);
                    var pos = trainData.Head.Incoming;
                    context.Output($"Train {trainId}: {pos}");
                }
                trains.Dispose();
            });
        });
    }

    public void CreateHooks()
    {
        MyMod.logger.Info.Log("##### Making Hooks");

        GameInitHook = DetourHelper.CreatePostfixHook<GameOrchestrator, UniTask>(
            original: orchestrator => orchestrator.InitializeMainMenu(),
            postfix: GetGameData);
        SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameStartOptions, GlobalsData, IGameData>(
            original: (orchestrator, gameStartOptions, globals, gameData) => orchestrator.Init(gameStartOptions, globals, gameData),
            postfix: GetSessionData);
        CameraHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameData, CameraGameSettings, Keybindings>(
            original: (orchestrator, gameData, cameraSettings, keybindings) => orchestrator.Init_6_PlayerInteraction(gameData, cameraSettings, keybindings),
            postfix: BindCamera);
        BuildingsHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, BuildingsModulesLookup> (
            original: (orchestrator, modules) => orchestrator.InjectBuildingsModuleProviders(modules),
            postfix: BindBuildings);
    }

    private UniTask GetGameData(GameOrchestrator self, UniTask result)
    {
        GameDependencyContainer = self.InitializationDependencyContainer;
        return result;
    }
    private void GetSessionData(GameSessionOrchestrator self, IGameStartOptions gameStartOptions, GlobalsData globals, IGameData gameData)
    {
        Globals = globals;
        SessionDependencyContainer = self.DependencyContainer;
    }
    private void BindCamera(GameSessionOrchestrator self, IGameData gameData, CameraGameSettings cameraSettings, Keybindings keybindings)
    {
        self.DependencyContainer.Bind<CameraController>().To(self.PlayerInteractionOrchestrator.CameraController);
    }
    private void BindBuildings(GameSessionOrchestrator self, BuildingsModulesLookup buildingsModulesLookup)
    {
        self.DependencyContainer.Bind<BuildingsModulesLookup>().To(buildingsModulesLookup);
    }

    public void Dispose()
    {
        GameInitHook?.Dispose();
        SessionInitHook?.Dispose();
        CameraHook?.Dispose();
        BuildingsHook?.Dispose();
        consoleRewirer?.Dispose();

        if (myGameObject != null)
        {
            GameObject.Destroy(myGameObject);
        }
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
