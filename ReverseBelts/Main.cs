using Core.Dependency;
using Cysharp.Threading.Tasks;
using Game.Orchestration;
using Game.Platforms;
using MonoMod.RuntimeDetour;
using ShapezShifter.SharpDetour;
using System;
using System.Runtime.InteropServices;
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
        private Hook BuildingsHook;
        private readonly GameObject inputHandlerObject;

        public Main(Core.Logging.ILogger logger)
        {
            Main.logger = logger;
            Main.logger.Info.Log("Hello from Reverse Belts!");

            BeltTools.SetLogger(logger);
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
            // public void Init_1_GameOptions(GlobalsData globals, IGameStartOptions gameStartOptions, IGameData gameData, IPlatform platform)
            SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, GlobalsData, IGameStartOptions, IGameData, IPlatform>(
                original: (orchestrator, globals, gameStartOptions, gameData, platform) => orchestrator.Init_1_GameOptions(globals, gameStartOptions, gameData, platform),
                postfix: GetSessionData);
            BuildingsHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, BuildingsModulesLookup>(
                original: (orchestrator, modules) => orchestrator.InjectBuildingsModuleProviders(modules),
                postfix: BindBuildings);
        }

        private UniTask GetGameData(GameOrchestrator self, UniTask result)
        {
            GameDependencyContainer = self.InitializationDependencyContainer;
            return result;
        }
        private void GetSessionData(GameSessionOrchestrator self, GlobalsData globals, IGameStartOptions gameStartOptions, IGameData gameData, IPlatform platform)
        {
            Globals = globals;
            SessionDependencyContainer = self.DependencyContainer;
        }
        private void BindBuildings(GameSessionOrchestrator self, BuildingsModulesLookup buildingsModulesLookup)
        {
            self.DependencyContainer.Bind<BuildingsModulesLookup>().To(buildingsModulesLookup);
        }

        public void Dispose()
        {
            GameInitHook?.Dispose();
            SessionInitHook?.Dispose();
            BuildingsHook?.Dispose();
            if (inputHandlerObject != null)
            {
                GameObject.Destroy(inputHandlerObject);
            }
        }
    }
}
