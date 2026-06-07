using Core.Dependency;
using Cysharp.Threading.Tasks;
using Game.Core.Trains;
using Game.Orchestration;
using MonoMod.RuntimeDetour;
using ShapezShifter.Flow;
using ShapezShifter.Kit;
using ShapezShifter.SharpDetour;
using System.Reflection;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static ShapezShifter.Flow.ModConsoleCommandsCreator;
using ILogger = Core.Logging.ILogger;

namespace TrainView
{

    public class Main : IMod
    {
        private static ILogger _logger;

        private ModConsoleCommandsCreator.ModConsoleRewirer consoleRewirer;
        private Hook GameInitHook;
        private Hook SessionInitHook;
        private Hook CameraHook;
        private Hook TrainHook;
        private Hook OVTrainHook;
        private GameObject myGameObject;

        private delegate bool DrawTrainWagonDelegate(TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, out Matrix4x4 wagonTrs);
        private delegate bool DrawTrainWagonOverviewDelegate(TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, UnityMeshReference mesh, MaterialReference material, out Matrix4x4 wagonTrs);

        public static GlobalsData Globals;
        public static DependencyContainer GameDependencyContainer;
        public static DependencyContainer SessionDependencyContainer;

        public Main(ILogger logger)
        {
            _logger = logger;

            _logger.Info.Log("Hello from Train View!");

            MyGameObject.SetLogger(logger);
            TrainSimulationHelper.SetLogger(logger);

            myGameObject = new GameObject("TrainView.MyGameObject");
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
                console.Register("debug", context =>
                {
                    var debugmode = SessionDependencyContainer.Resolve<DebugModeManager>();
                    debugmode.ShowView("trains");
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
                console.Register("listtrains", context =>
                {
                    context.Output("Listing trains...");
                    var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
                    var trainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
                    var trains = trainSim.GetAllTrains(Allocator.Temp);
                    foreach (var trainId in trains)
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
            BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

            GameInitHook = DetourHelper.CreatePostfixHook<GameOrchestrator, UniTask>(
                original: orchestrator => orchestrator.InitializeMainMenu(),
                postfix: GetGameData);
            SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameStartOptions, GlobalsData, IGameData>(
                original: (orchestrator, gameStartOptions, globals, gameData) => orchestrator.Init(gameStartOptions, globals, gameData),
                postfix: GetSessionData);
            CameraHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameData, CameraGameSettings, Keybindings>(
                original: (orchestrator, gameData, cameraSettings, keybindings) => orchestrator.Init_6_PlayerInteraction(gameData, cameraSettings, keybindings),
                postfix: BindCamera);
            // Use low level hook call because ShapezShifter's detour doesn't support out parameters.
            TrainHook = new Hook(
                typeof(TrainsDrawer).GetMethod("DrawTrainWagon", FLAGS),
                typeof(Main).GetMethod("UpdateTrainCamera", FLAGS), this);
            OVTrainHook = new Hook(
                typeof(TrainsDrawer).GetMethod("DrawTrainWagonOverview", FLAGS),
                typeof(Main).GetMethod("UpdateTrainCameraOverview", FLAGS), this);
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

        private bool UpdateTrainCameraOverview(DrawTrainWagonOverviewDelegate orig, TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, UnityMeshReference mesh, MaterialReference material, out Matrix4x4 wagonTrs)
        {
            //_logger.Info.Log("##### Train is in overview #####");
            wagonTrs = default;
            bool result = orig(self, draw, trainId, train, wagonIdx, wagonData, mesh, material, out wagonTrs);
            if (wagonIdx != 0 || trainId != MyGameObject.CurrentTrainId)
            {
                return result;
            }
            var (pos, rot) = TrainSimulationHelper.ApproxTrainPosition(wagonData);

            // convert to camera coordinates
            var camera = Main.SessionDependencyContainer.Resolve<CameraController>();
            pos.y = -pos.y;
            camera.CurrentPosition = pos;

            return result;
        }

        private bool UpdateTrainCamera(DrawTrainWagonDelegate orig, TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, out Matrix4x4 wagonTrs)
        {
            wagonTrs = default;
            bool result = orig(self, draw, trainId, train, wagonIdx, wagonData, out wagonTrs);
            if (wagonIdx != 0 || trainId != MyGameObject.CurrentTrainId)
            {
                return result;
            }

            var camera = Main.SessionDependencyContainer.Resolve<CameraController>();
            var zoom = GameHelper.Core.Viewport.Zoom;
            if (result == false)
            {
                // Train is off camera.  Get approx location using trainData.
                //_logger.Info.Log("Train is off camera");
                var (pos, rot) = TrainSimulationHelper.ApproxTrainPosition(wagonData);

                // convert to camera coordinates
                pos.y = -pos.y;
                camera.CurrentPosition = pos;

                // Rotate the camera only if the camera is zoomed in.
                if (zoom < 75f)
                {
                    camera.TargetRotationDegrees = rot;
                }
            }
            else
            {
                //_logger.Info.Log("Train is on camera");
                // Train is on camera.  Get exact location using wagonTrs.
                Vector3 pos = wagonTrs.GetPosition();
                //_logger.Info.Log($"Train TRS position: {pos.x} {pos.y} {pos.z}");
                camera.CurrentPosition = new double2(pos[0], pos[2]);
                if (zoom < 75f)
                {
                    camera.TargetRotationDegrees = wagonTrs.rotation.eulerAngles.y + 90f;
                }
            }

            return result;
        }

        public void Dispose()
        {
            GameInitHook?.Dispose();
            SessionInitHook?.Dispose();
            CameraHook?.Dispose();
            TrainHook?.Dispose();
            OVTrainHook?.Dispose();
            consoleRewirer?.Dispose();

            if (myGameObject != null)
            {
                GameObject.Destroy(myGameObject);
            }
        }
    }
}
