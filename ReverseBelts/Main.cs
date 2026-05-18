using Core.Dependency;
using Cysharp.Threading.Tasks;
using Game.Orchestration;
using MonoMod.RuntimeDetour;
using ShapezShifter.SharpDetour;
using UnityEngine;

namespace ReverseBelts
{
    public class Main : IMod
    {
        public static Core.Logging.ILogger logger;
        public static GlobalsData Globals;
        public static DependencyContainer GameDependencyContainer;
        public static DependencyContainer SessionDependencyContainer;

        private Hook GameInitHook;
        private Hook SessionInitHook;
        private Hook CameraHook;
        private Hook BuildingsHook;
        private GameObject inputHandlerObject;

        public Main(Core.Logging.ILogger logger)
        {
            Main.logger = logger;
            Main.logger.Info.Log("Hello from Reverse Belts!");

            InputHandler.SetLogger(logger);
            inputHandlerObject = new GameObject("ModInputHandler");
            inputHandlerObject.AddComponent<InputHandler>();
            GameObject.DontDestroyOnLoad(inputHandlerObject);

            CreateHooks();
        }

        public void CreateHooks()
        {
            logger.Info.Log("##### Making Hooks");

            GameInitHook = DetourHelper.CreatePostfixHook<GameOrchestrator, UniTask>(
                original: orchestrator => orchestrator.InitializeMainMenu(),
                postfix: GetGameData);
            SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameStartOptions, GlobalsData, IGameData>(
                original: (orchestrator, gameStartOptions, globals, gameData) => orchestrator.Init(gameStartOptions, globals, gameData),
                postfix: GetSessionData);
            CameraHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameData, CameraGameSettings, Keybindings>(
                original: (orchestrator, gameData, cameraSettings, keybindings) => orchestrator.Init_6_PlayerInteraction(gameData, cameraSettings, keybindings),
                postfix: BindCamera);
            BuildingsHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, BuildingsModulesLookup>(
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
            if (inputHandlerObject != null)
            {
                GameObject.Destroy(inputHandlerObject);
            }
        }
    }
}
