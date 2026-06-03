using Core.Collections;
using Core.Localization;
using Cysharp.Threading.Tasks;
using Game.Core.Research;
using Game.Orchestration;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Kit;
using ShapezShifter.SharpDetour;
using ShapezShifter.Textures;
using System;
using UnityEngine;
using ILogger = Core.Logging.ILogger;
using Renderer = CornerCutterSimulationRenderer;
using RendererData = ICornerCutterDrawData;
using Simulation = CornerCutterSimulation;

[UsedImplicitly]
public class CornerCuttersMod : IMod
{
    public static ILogger logger;

    private Hook UBHook;
    private Hook GameInitHook;

    GameImageId shopImageId;
    Sprite shopImage;

    public CornerCuttersMod(ILogger logger)
    {
        CornerCuttersMod.logger = logger;

        //var d = delegate (Func<GameData, GameImageId, Sprite> orig, GameData self, GameImageId id) {
        //    if (id.IsEmpty) { logger.Info.Log("GetImage id is null"); return null; } else { return orig(self, id); }
        //};
        //Func<Func<GameData, GameImageId, Sprite>, GameData, GameImageId, Sprite> ImageWrapper = d;
        //ImageHook = new Hook(
        //    typeof(GameData).GetMethod(nameof(GameData.GetImage), new[] { typeof(GameImageId) })!,
        //    ImageWrapper);

        GameInitHook = DetourHelper.CreatePostfixHook<GameOrchestrator, UniTask>(
            original: orchestrator => orchestrator.InitializeMainMenu(),
            postfix: (self, result) => {
                GameData gameData = self.InitializationDependencyContainer.Resolve<IGameData>() as GameData;
                gameData._Images.Add(shopImageId, shopImage);
                return result;
            });
        UBHook = DetourHelper.CreatePostfixHook<ShapezShifter.Flow.Atomic.SideUpgradeBuilder, ScenarioId, ResearchProgression, ResearchSideUpgrade>(
            original: (builder, scenarioId, progression) => builder.Build(scenarioId, progression),
            postfix: (_, _, progression, upgrade) => {
                if (!progression._ShopItems.Contains(upgrade))
                {
                    progression._ShopItems.Add(upgrade);
                }
                if (!progression._SideUpgradeCategories.Contains(upgrade.Category))
                {
                    progression._SideUpgradeCategories.Add(upgrade.Category);
                }
                return upgrade;
            });

        //SessionInitHook = DetourHelper.CreatePostfixHook<GameSessionOrchestrator, IGameStartOptions, GlobalsData, IGameData>(
        //    original: (orchestrator, gameStartOptions, globals, gameData) => orchestrator.Init(gameStartOptions, globals, gameData),
        //    postfix: GetSessionData);

        BuildingDefinitionGroupId groupId = new("CornerCutterGroup");
        BuildingDefinitionId definitionId = new("CornerCutter");
        shopImageId = new GameImageId("CornerCutterShopImage");

        string titleId = "building-variant.cutter-corner.title";
        string titleDescription = "building-variant.cutter-corner.description";

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<CornerCuttersMod>().SubLocator("Resources");

        using var assetBundleHelper =
            AssetBundleHelper.CreateForAssetBundleEmbeddedWithMod<CornerCuttersMod>("Resources/CornerCutter");

        string iconPath = modResourcesLocator.SubPath("CornerCutter_Icon.png");
        Sprite icon = FileTextureLoader.LoadTextureAsSprite(iconPath, out _);
        string shopImagePath = modResourcesLocator.SubPath("Preview.png");
        shopImage = FileTextureLoader.LoadTextureAsSprite(shopImagePath, out _);

        IBuildingGroupBuilder cornerCutterGroup = BuildingGroup.Create(groupId)
           .WithTitle(titleId.T())
           .WithDescription(titleDescription.T())
           .WithIcon(icon)
           .AsNonTransportableBuilding()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.LinePerpendicular)
           .WithDefaultStructureOverview();

        IBuildingConnectorData connectorData = BuildingConnectors.SingleTile()
           .AddShapeInput(ShapeConnectorConfig.DefaultInput())
           .AddShapeOutput(ShapeConnectorConfig.DefaultOutput())
           .Build();

        IBuildingBuilder cornerCutterBuilder = Building.Create(definitionId)
           .WithConnectorData(connectorData)
           .DynamicallyRendering<Renderer, Simulation, RendererData>(new CornerCutterDrawData())
           .WithStaticDrawData(CreateDrawData(modResourcesLocator))
           .WithoutSound()
           .WithoutSimulationConfiguration()
           .WithEfficiencyData(new BuildingEfficiencyData(2.0f, 1));

        IPresentableUnlockableSideUpgradeBuilder sideUpgradeBuilder = SideUpgrade.New()
           .WithPresentationData(CreateSideUpgradePresentationData(titleId, titleDescription))
           .WithCost(new ResearchCostPoints(new ResearchPointCurrency(50)).AsEnumerable())
           .WithCustomRequirements(Array.Empty<ResearchMechanicId>(), Array.Empty<ResearchUpgradeId>());
        AtomicBuildings.Extend()
           .AllScenarios()
           .WithBuilding(cornerCutterBuilder, cornerCutterGroup)
           .UnlockedWithNewSideUpgrade(sideUpgradeBuilder)
           .WithDefaultPlacement()
           .InToolbar(ToolbarElementLocator.Root().ChildAt(0).ChildAt(2).ChildAt(^1).InsertAfter())
           .WithSimulation(new CornerCutterFactoryBuilder(), logger)
           .WithAtomicShapeProcessingModules(BuiltinResearchSpeed.CutterSpeed, 2.0f)
           .WithPrediction(new CornerCutterPredictionFactoryBuilder(), logger)
           .Build();
    }

    public void Dispose()
    {
        UBHook?.Dispose();
        GameInitHook?.Dispose();
    }

    private SideUpgradePresentationData CreateSideUpgradePresentationData(string titleId, string titleDescription)
    {
        return new SideUpgradePresentationData(
            new ResearchUpgradeId("CBCornerCutter"),
            shopImageId,
            GameVideoId.Empty,
            titleId.T(),
            titleDescription.T(),
            false,
            "Buildings");
    }

    private static BuildingDrawData CreateDrawData(ModFolderLocator modResourcesLocator)
    {
        string baseMeshPath = modResourcesLocator.SubPath("CornerCutter.fbx");
        Mesh baseMesh = FileMeshLoader.LoadSingleMeshFromFile(baseMeshPath);

        LOD6Mesh baseModLod = MeshLod.Create().AddLod0Mesh(baseMesh).BuildLod6Mesh();

        return new BuildingDrawData(
            renderVoidBelow: false,
            new ILODMesh[] { baseModLod, baseModLod, baseModLod },
            baseModLod,
            baseModLod,
            baseModLod.LODClose,
            new LODEmptyMesh(),
            BoundingBoxHelper.CreateBasicCollider(baseMesh),
            new CornerCutterDrawData(),
            false,
            null,
            false);
    }
}
