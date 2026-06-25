using Core.Dependency;
using Cysharp.Threading.Tasks;
using Game.Content.Rendering.Trains;
using Game.Core.Content;
using Game.Core.Content.Meta;
using Game.Core.Rendering.Culling;
using Game.Core.Trains;
using Game.Orchestration;
using Game.Platforms;
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

        public static GlobalsData Globals;
        public static DependencyContainer GameDependencyContainer;
        public static DependencyContainer SessionDependencyContainer;

        public static TrainsSimulation trainSim;
        public static TrainId currentTrainId = TrainId.Invalid;
        private Matrix4x4 wagonTrs;
        private bool wasDrawn = false;

        private ModConsoleCommandsCreator.ModConsoleRewirer consoleRewirer;
        private GameObject myGameObject;
        private Hook GameInitHook;
        private Hook SessionInitHook;
        private Hook CameraHook;
        private Hook DrawHook;
        private Hook TrainHook;
        private Hook OVTrainHook;

        private delegate bool DrawTrainWagonDelegate(TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, out Matrix4x4 wagonTrs);
        private delegate bool DrawTrainWagonOverviewDelegate(TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, UnityMeshReference mesh, MaterialReference material, out Matrix4x4 wagonTrs);

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
            // UniTask InitializeMainMenu()
            GameInitHook = DetourHelper.CreatePostfixHook<GameOrchestrator, UniTask>(
                original: orchestrator => orchestrator.InitializeMainMenu(),
                postfix: GetGameData);
            // public async UniTask Prepare(IGameStartOptions gameStartOptions, GlobalsData globals, IGameData gameData, IPlatform platform, IContent content, IContentMetadataProviderCollection contentMetadata)
            SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameStartOptions, GlobalsData, IGameData, IPlatform, IContent, IContentMetadataProviderCollection, UniTask>(
                original: (orchestrator, gameStartOptions, globals, gameData, platform, content, contentMetadata) => orchestrator.Prepare(gameStartOptions, globals, gameData, platform, content, contentMetadata),
                postfix: GetSessionData);
            // void Init_6_PlayerInteraction(IGameData gameData, CameraGameSettings cameraSettings, Keybindings keybindings)
            CameraHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameData, CameraGameSettings, Keybindings>(
                original: (orchestrator, gameData, cameraSettings, keybindings) => orchestrator.Init_6_PlayerInteraction(gameData, cameraSettings, keybindings),
                postfix: BindCamera);
            // void DoDraw(FrameDrawOptionsNoLOD options, MapCullResult cullResult)
            DrawHook = DetourHelper.CreatePostfixHook<TrainsDrawer, FrameDrawOptionsNoLOD, MapCullResult>(
                original: (drawer, options, cullResult) => drawer.DoDraw(options, cullResult),
                postfix: Update);

            // Use low level hook call because ShapezShifter's detour doesn't support out parameters.
            BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            TrainHook = new Hook(
                typeof(TrainsDrawer).GetMethod("DrawTrainWagon", FLAGS),
                typeof(Main).GetMethod("GetTrs", FLAGS), this);
            OVTrainHook = new Hook(
                typeof(TrainsDrawer).GetMethod("DrawTrainWagonOverview", FLAGS),
                typeof(Main).GetMethod("GetTrsOverview", FLAGS), this);
        }

        private UniTask GetGameData(GameOrchestrator self, UniTask result)
        {
            GameDependencyContainer = self.InitializationDependencyContainer;
            return result;
        }
        private UniTask GetSessionData(GameSessionOrchestrator self, IGameStartOptions gameStartOptions, GlobalsData globals, IGameData gameData, IPlatform platform, IContent content, IContentMetadataProviderCollection contentMetadata, UniTask result)
        {
            Globals = globals;
            SessionDependencyContainer = self.DependencyContainer;
            trainSim = self.Simulator.GetSystem<TrainSystem>().TrainsSimulation;
            return result;
        }
        private void BindCamera(GameSessionOrchestrator self, IGameData gameData, CameraGameSettings cameraSettings, Keybindings keybindings)
        {
            self.DependencyContainer.Bind<CameraController>().To(self.PlayerInteractionOrchestrator.CameraController);
        }

        private void Update(TrainsDrawer self, FrameDrawOptionsNoLOD options, MapCullResult cullResult)
        {
            if (currentTrainId == TrainId.Invalid)
            {
                return;
            }
            double2 position;
            float rotation;
            float height = 0f;
            if (wasDrawn)
            {
                // Get exact location using wagonTrs.
                Vector3 pos = wagonTrs.GetPosition();
                //_logger.Info.Log($"Using TRS position: {pos.x} {pos.y} {pos.z}");
                position = new double2(pos[0], pos[2]);
                var wagonData = trainSim.GetTrainData(currentTrainId).Wagons[0];
                rotation = wagonTrs.rotation.eulerAngles.y + (wagonData.UpsideDown ? -90f : 90f);
                height = pos[1];
                wasDrawn = false;
            }
            else
            {
                // No TRS data.  Get approx location using trainData.
                try
                {
                    (position, rotation) = TrainSimulationHelper.ApproxTrainPosition(currentTrainId);
                }
                catch
                {
                    _logger.Info.Log($"Train not found: {currentTrainId}");
                    return;
                }

                // convert to camera coordinates
                position.y = -position.y;
            }
            var camera = Main.SessionDependencyContainer.Resolve<CameraController>();
            camera.CurrentPosition = position;
            var parent = GameHelper.Core.Viewport.MainCamera.transform.parent;
            parent.position = new float3(parent.position.x, height, parent.position.z);

            // Rotate the camera only if the camera is zoomed in.
            float zoom = GameHelper.Core.Viewport.Zoom;
            if (zoom < 75f)
            {
                camera.TargetRotationDegrees = rotation;
                //var roll = wagonTrs.rotation.eulerAngles.x;
                //var yaw = wagonTrs.rotation.eulerAngles.y;
                //var pitch = wagonTrs.rotation.eulerAngles.z;
                //Vector3 offset = new Vector3(0, 10f, 0);
                //parent.position = wagonTrs.GetPosition() + offset;
                //parent.rotation = Quaternion.Euler(0, yaw + 90f, 0);
            }
        }

        private bool GetTrs(DrawTrainWagonDelegate orig, TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, out Matrix4x4 wagonTrs)
        {
            wagonTrs = Matrix4x4.identity;
            bool result = orig(self, draw, trainId, train, wagonIdx, wagonData, out wagonTrs);
            if (result && wagonIdx == 0 && trainId == currentTrainId)
            {
                wasDrawn = true;
                this.wagonTrs = wagonTrs;
            }

            return result;
        }

        private bool GetTrsOverview(DrawTrainWagonOverviewDelegate orig, TrainsDrawer self, FrameDrawOptionsNoLOD draw, TrainId trainId, TrainData train, int wagonIdx, WagonNavigationData wagonData, UnityMeshReference mesh, MaterialReference material, out Matrix4x4 wagonTrs)
        {
            wagonTrs = Matrix4x4.identity;
            bool result = orig(self, draw, trainId, train, wagonIdx, wagonData, mesh, material, out wagonTrs);
            if (result && wagonIdx == 0 && trainId == currentTrainId)
            {
                wasDrawn = true;
                this.wagonTrs = wagonTrs;
            }

            return result;
        }

        public void Dispose()
        {
            GameInitHook?.Dispose();
            SessionInitHook?.Dispose();
            CameraHook?.Dispose();
            DrawHook?.Dispose();
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
