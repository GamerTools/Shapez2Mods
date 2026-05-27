using Game.Core.Coordinates;
using ShapezShifter.Kit;
using ILogger = Core.Logging.ILogger;

namespace ReverseBelts
{
    class BeltTools
    {
        private static ILogger _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public static void ReverseBelts()
        {
            var buildingSelection = GameHelper.Core.LocalPlayer.InteractionState.BuildingSelection;
            foreach (var building in buildingSelection)
            {
                var map = building.Map;
                var config = building.Configuration;
                getNewDefinition(building, out var definition);
                getNewTransform(building, out var transform);

                string oldBldg = building.ToString();
                if (definition != null)
                {
                    map.DeleteBuilding(building.Id);
                    var newBldg = map.CreateBuilding(definition, transform, config);
                    _logger?.Info.Log($"Old {oldBldg}");
                    _logger?.Info.Log($"New {newBldg}");
                }
                else
                {
                    _logger?.Info.Log($"Unk {oldBldg}");
                }
            }
            buildingSelection.Clear();
        }

        // TODO: BuildingPlacersCreator has a Buildings field that might be useful instead of making new Ids.
        // TODO: Maybe GameHelper.Core.Mode.Buildings can be used?
        private static void getNewDefinition(BuildingModel building, out IBuildingDefinition definition)
        {
            var map = GameHelper.Core.LocalPlayer.CurrentMap.Buildings;
            var buildings = Main.SessionDependencyContainer.Resolve<BuildingsModulesLookup>().BuildingSimulationData;
            switch (building.Definition.Id.Name)
            {
                case "BeltDefaultForwardInternalVariant":
                    definition = building.Definition;
                    break;
                case "BeltDefaultLeftInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("BeltDefaultLeftInternalVariant"), out definition);
                    break;
                case "BeltDefaultLeftInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("BeltDefaultLeftInternalVariantMirrored"), out definition);
                    break;
                case "Splitter1To2LInternalVariant":
                case "SplitterOverflowLInternalVariant":
                case "BeltFilterDefaultInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Merger2To1LInternalVariantMirrored"), out definition);
                    break;
                case "Merger2To1LInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Splitter1To2LInternalVariant"), out definition);
                    break;
                case "Splitter1To2LInternalVariantMirrored":
                case "SplitterOverflowLInternalVariantMirrored":
                case "BeltFilterDefaultInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Merger2To1LInternalVariant"), out definition);
                    break;
                case "Merger2To1LInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Splitter1To2LInternalVariantMirrored"), out definition);
                    break;
                case "SplitterTShapeInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("MergerTShapeInternalVariant"), out definition);
                    break;
                case "MergerTShapeInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("SplitterTShapeInternalVariant"), out definition);
                    break;
                case "Splitter1To3InternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Merger3To1InternalVariant"), out definition);
                    break;
                case "Merger3To1InternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Splitter1To3InternalVariant"), out definition);
                    break;
                case "Lift1UpForwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1DownForwardInternalVariant"), out definition);
                    break;
                case "Lift1DownForwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1UpForwardInternalVariant"), out definition);
                    break;
                case "Lift2UpForwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2DownForwardInternalVariant"), out definition);
                    break;
                case "Lift2DownForwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2UpForwardInternalVariant"), out definition);
                    break;
                case "Lift1UpLeftInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1DownLeftInternalVariantMirrored"), out definition);
                    break;
                case "Lift1DownLeftInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1UpLeftInternalVariant"), out definition);
                    break;
                case "Lift1UpLeftInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1DownLeftInternalVariant"), out definition);
                    break;
                case "Lift1DownLeftInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1UpLeftInternalVariantMirrored"), out definition);
                    break;
                case "Lift2UpLeftInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2DownLeftInternalVariantMirrored"), out definition);
                    break;
                case "Lift2DownLeftInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2UpLeftInternalVariant"), out definition);
                    break;
                case "Lift2UpLeftInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2DownLeftInternalVariant"), out definition);
                    break;
                case "Lift2DownLeftInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2UpLeftInternalVariantMirrored"), out definition);
                    break;
                case "Lift1UpBackwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1DownBackwardInternalVariant"), out definition);
                    break;
                case "Lift1DownBackwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift1UpBackwardInternalVariant"), out definition);
                    break;
                case "Lift2UpBackwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2DownBackwardInternalVariant"), out definition);
                    break;
                case "Lift2DownBackwardInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("Lift2UpBackwardInternalVariant"), out definition);
                    break;
                case "BeltPortSenderInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("BeltPortReceiverInternalVariant"), out definition);
                    break;
                case "BeltPortReceiverInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("BeltPortSenderInternalVariant"), out definition);
                    break;
                case "BeltReaderDefaultInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("BeltReaderDefaultInternalVariantMirrored"), out definition);
                    break;
                case "BeltReaderDefaultInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("BeltReaderDefaultInternalVariant"), out definition);
                    break;
                case "RotatorOneQuadInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("RotatorOneQuadCCWInternalVariant"), out definition);
                    break;
                case "RotatorOneQuadCCWInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("RotatorOneQuadInternalVariant"), out definition);
                    break;
                case "CutterDefaultInternalVariant":
                case "CutterDefaultInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("HalvesSwapperDefaultInternalVariant"), out definition);
                    break;
                case "PainterDefaultInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("PainterDefaultInternalVariantMirrored"), out definition);
                    break;
                case "PainterDefaultInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("PainterDefaultInternalVariant"), out definition);
                    break;
                case "CrystalGeneratorDefaultInternalVariant":
                    buildings.TryGetValue(new BuildingDefinitionId("CrystalGeneratorDefaultInternalVariantMirrored"), out definition);
                    break;
                case "CrystalGeneratorDefaultInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("CrystalGeneratorDefaultInternalVariant"), out definition);
                    break;
                case "RotatorHalfInternalVariant":
                case "CutterHalfInternalVariant":
                case "HalvesSwapperDefaultInternalVariant":
                    definition = building.Definition;
                    break;
                default:
                    definition = null;
                    break;
            }
        }

        private static void getNewTransform(BuildingModel building, out GlobalTileTransform transform)
        {
            // set the position - only needed for belt lifts and to adjust cutters
            TileVector offset;
            switch (building.Definition.Id.Name)
            {
                case "Lift1UpForwardInternalVariant":
                case "Lift1UpLeftInternalVariant":
                case "Lift1UpLeftInternalVariantMirrored":
                case "Lift1UpBackwardInternalVariant":
                    offset = TileVector.Up;
                    break;
                case "Lift1DownForwardInternalVariant":
                case "Lift1DownLeftInternalVariant":
                case "Lift1DownLeftInternalVariantMirrored":
                case "Lift1DownBackwardInternalVariant":
                    offset = TileVector.Down;
                    break;
                case "Lift2UpForwardInternalVariant":
                case "Lift2UpLeftInternalVariant":
                case "Lift2UpLeftInternalVariantMirrored":
                case "Lift2UpBackwardInternalVariant":
                    offset = new TileVector(0, 0, 2);
                    break;
                case "Lift2DownForwardInternalVariant":
                case "Lift2DownLeftInternalVariantMirrored":
                case "Lift2DownLeftInternalVariant":
                case "Lift2DownBackwardInternalVariant":
                    offset = new TileVector(0, 0, -2);
                    break;
                case "CutterDefaultInternalVariant":
                case "HalvesSwapperDefaultInternalVariant":
                    int x = 0;
                    int y = 0;
                    switch (building.Transform.Rotation.Value)
                    {
                        case GridRotation.Serializable.NoRotate:
                            y = -1;
                            break;
                        case GridRotation.Serializable.RotateCW:
                            x = 1;
                            break;
                        case GridRotation.Serializable.Rotate180:
                            y = 1;
                            break;
                        case GridRotation.Serializable.RotateCCW:
                            x = -1;
                            break;
                        default:
                            break;
                    }
                    offset = new TileVector(x, y, 0);
                    break;
                default:
                    offset = TileVector.Zero;
                    break;
            }
            transform = building.Transform + offset;

            // Set the rotation
            switch (building.Definition.Id.Name)
            {
                case "BeltDefaultLeftInternalVariantMirrored":
                case "Lift1DownLeftInternalVariantMirrored":
                case "Lift1UpLeftInternalVariantMirrored":
                case "Lift2DownLeftInternalVariantMirrored":
                case "Lift2UpLeftInternalVariantMirrored":
                    transform = transform.Rotate(GridRotation.RotateCCW);
                    break;
                case "BeltDefaultLeftInternalVariant":
                case "Lift1UpLeftInternalVariant":
                case "Lift1DownLeftInternalVariant":
                case "Lift2UpLeftInternalVariant":
                case "Lift2DownLeftInternalVariant":
                    transform = transform.Rotate(GridRotation.RotateCW);
                    break;
                case "Lift1UpBackwardInternalVariant":
                case "Lift1DownBackwardInternalVariant":
                case "Lift2UpBackwardInternalVariant":
                case "Lift2DownBackwardInternalVariant":
                    // No rotation needed
                    break;
                default:
                    transform = transform.Rotate(GridRotation.Rotate180);
                    break;
            }
        }

    }
}
