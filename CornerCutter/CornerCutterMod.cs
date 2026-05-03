using System;
using Core.Collections;
using Core.Localization;
using Game.Core.Research;
using JetBrains.Annotations;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Kit;
using ShapezShifter.Textures;
using UnityEngine;
using ILogger = Core.Logging.ILogger;
using Renderer = CornerCutterSimulationRenderer;
using Simulation = CornerCutterSimulation;
using RendererData = ICornerCutterDrawData;

[UsedImplicitly]
public class CornerCuttersMod : IMod
{
    public CornerCuttersMod(ILogger logger)
    {
        BuildingDefinitionGroupId groupId = new("CornerCutterGroup");
        BuildingDefinitionId definitionId = new("CornerCutter");

        string titleId = "building-variant.cutter-corner.title";
        string titleDescription = "building-variant.cutter-corner.description";

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<CornerCuttersMod>().SubLocator("Resources");

        using var assetBundleHelper =
            AssetBundleHelper.CreateForAssetBundleEmbeddedWithMod<CornerCuttersMod>("Resources/CornerCutter");

        string iconPath = modResourcesLocator.SubPath("CornerCutter_Icon.png");

        IBuildingGroupBuilder cornerCutterGroup = BuildingGroup.Create(groupId)
           .WithTitle(titleId.T())
           .WithDescription(titleDescription.T())
           .WithIcon(FileTextureLoader.LoadTextureAsSprite(iconPath, out _))
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

    public void Dispose() { }

    private SideUpgradePresentationData CreateSideUpgradePresentationData(string titleId, string titleDescription)
    {
        return new SideUpgradePresentationData(
            new ResearchUpgradeId("CBCornerCutter"),
            GameImageId.Empty,
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
