using System;
using System.Collections.Generic;
using System.Linq;
using Core.Collections.Scoped;
using Core.Localization;
using Game.Core.Coordinates;
using Game.Core.Research;
using JetBrains.Annotations;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Kit;
using ShapezShifter.Textures;
using Unity.Mathematics;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class MyMod : IMod
{
    static readonly ChunkDirection[][] FoundationTypes =
    {
        Array.Empty<ChunkDirection>(),
        new[] { ChunkDirection.North },
        new[] { ChunkDirection.North, ChunkDirection.West },
        new[] { ChunkDirection.North, ChunkDirection.South },
        new[] { ChunkDirection.North, ChunkDirection.West, ChunkDirection.South }
    };

    public MyMod(ILogger logger)
    {
        logger.Info.Log("Hello from MyMod!");

        foreach (var (notchDirections, index) in FoundationTypes.Select((nd, i) => (nd, i)))
        {
            AddFoundation(index + 1, notchDirections);
        }
    }

    public void Dispose() { }

    private void AddFoundation(int index, ChunkDirection[] notchDirections)
    {
        string suffix = $"var_{index}";
        IslandDefinitionGroupId groupId = new($"FoundationGroup_{suffix}");
        IslandDefinitionId definitionId = new($"Foundation_{suffix}");

        string titleId = $"Foundation{suffix}.title";
        string descriptionId = "island-layout.Layout_GenericPlatform.description";

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<MyMod>().SubLocator("Resources");

        string icon = modResourcesLocator.SubPath($"Foundation_4x4.png");

        IIslandGroupBuilder islandGroupBuilder = IslandGroup.Create(groupId)
           .WithTitle(titleId.T())
           .WithDescription(descriptionId.T())
           .WithIcon(FileTextureLoader.LoadTextureAsSprite(icon, out _))
           .AsNonTransportableIsland()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.Area);

        ChunkLayoutLookup<ChunkVector, IslandChunkData> layout = FoundationLayout(notchDirections);

        IIslandBuilder islandBuilder = Island.Create(definitionId)
           .WithLayout(layout)
           .WithConnectorData(FoundationConnectors(layout))
           .WithInteraction(flippable: false, canHoldBuildings: true)
           .WithDefaultChunkCost()
           .WithRenderingOptions(ChunkDrawingOptions(), drawPlayingField: true);

        IToolbarElementLocator platformsGroup = ToolbarElementLocator.Root().ChildAt(5);
        IToolbarElementLocator lineFoundations = platformsGroup.ChildAt(4);

        IToolbarEntryInsertLocation toolbarEntryLocation = lineFoundations.ChildAt(^1).InsertAfter();

        AtomicIslands.Extend()
           .AllScenarios()
           .WithIsland(islandBuilder, islandGroupBuilder)
           .UnlockedAtMilestone(new ByIdPerScenarioMilestoneSelector(MilestoneSelectorBasedOnMode))
           .WithDefaultPlacement()
           .InToolbar(toolbarEntryLocation)
           .WithoutSimulation()
           .WithoutModules()
           .Build();
        return;

        ResearchUpgradeId MilestoneSelectorBasedOnMode(string scenario)
        {
            string milestoneId = scenario.ToLower().Contains("converter-scenario")
                ? "RNTier1_Onboarding"
                : "RNInitial";
            return new ResearchUpgradeId(milestoneId);
        }
    }

    private IChunkDrawingContextProvider ChunkDrawingOptions()
    {
        return new HomogeneousChunkDrawing(ChunkPlatformDrawingContext.DrawAll());
    }

    // TODO: Create fluent API for this
    private ChunkLayoutLookup<ChunkVector, IslandChunkData> FoundationLayout(ChunkDirection[] notchDirections)
    {
        return new ChunkLayoutLookup<ChunkVector, IslandChunkData>(Chunks(notchDirections));
    }

    private IEnumerable<KeyValuePair<ChunkVector, IslandChunkData>> Chunks(ChunkDirection[] notchDirections)
    {
        ChunkVector thisChunk = ChunkVector.Zero;
        ScopedList<ChunkVector> otherChunks = ScopedList<ChunkVector>.Get();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                bool found = false;
                foreach (ChunkDirection dir in notchDirections)
                {
                    ChunkVector neighbor = thisChunk + dir;
                    if (neighbor.x == x && neighbor.y == y) {
                        found = true;
                        break;
                    }
                }
                if (found)
                    continue;
                otherChunks.Add(new ChunkVector(x, y, 0));
            }
        }

        yield return new KeyValuePair<ChunkVector, IslandChunkData>(
            thisChunk,
            IslandLayoutFactory.CreateIslandChunkData(
                thisChunk,
                notchDirections,
                otherChunks,
                true,
                false,
                out _));
    }

    // TODO: Create fluent API for this
    private IIslandConnectorData FoundationConnectors(ChunkLayoutLookup<ChunkVector, IslandChunkData> chunkLayout)
    {
        return new IslandConnectorData(
            Array.Empty<EntityIO<LocalChunkPivot, IIslandConnector>>(),
            chunkLayout.ChunkPositions);
    }

}
