using Game.Core.Coordinates;
using ShapezShifter.Kit;
using System.Text;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

namespace ReverseBelts
{
    public class InputHandler : MonoBehaviour
    {
        private static ILogger _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                ReverseBelts();
            }
        }

        private void ReverseBelts()
        {
            var buildingSelection = GameHelper.Core.LocalPlayer.InteractionState.BuildingSelection;
            foreach (var building in buildingSelection)
            {
                var map = building.Map;
                var config = building.Configuration;
                getNewDefinition(building, out var definition);
                getNewTransform(building, out var transform);

                string oldBldg = building.ToString();
                map.DeleteBuilding(building.Id);
                if (definition != null)
                {
                    var newbuilding = map.CreateBuilding(definition, transform, config);
                    _logger?.Info.Log($"Old {oldBldg}");
                    _logger?.Info.Log($"New {newbuilding}");
                }
                else
                {
                    _logger?.Info.Log($"DELETED {oldBldg}");
                }
            }
            buildingSelection.Clear();
        }
        private void getNewDefinition(BuildingModel building, out IBuildingDefinition definition)
        {
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
                    buildings.TryGetValue(new BuildingDefinitionId("Merger2To1LInternalVariantMirrored"), out definition);
                    break;
                case "Merger2To1LInternalVariantMirrored":
                    buildings.TryGetValue(new BuildingDefinitionId("Splitter1To2LInternalVariant"), out definition);
                    break;
                case "Splitter1To2LInternalVariantMirrored":
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
                default:
                    definition = null;
                    //_logger?.Info.Log($"Unsupported building type: {definition.Name}");
                    break;
            }
        }

        private void getNewTransform(BuildingModel building, out GlobalTileTransform transform)
        {
            switch (building.Definition.Id.Name)
            {
                case "BeltDefaultLeftInternalVariantMirrored":
                case "Lift1DownLeftInternalVariantMirrored":
                case "Lift1UpLeftInternalVariantMirrored":
                case "Lift2DownLeftInternalVariantMirrored":
                case "Lift2UpLeftInternalVariantMirrored":
                    transform = building.Transform.Rotate(GridRotation.RotateCCW);
                    break;
                case "BeltDefaultLeftInternalVariant":
                case "Lift1UpLeftInternalVariant":
                case "Lift1DownLeftInternalVariant":
                case "Lift2UpLeftInternalVariant":
                case "Lift2DownLeftInternalVariant":
                    transform = building.Transform.Rotate(GridRotation.RotateCW);
                    break;
                case "Lift1UpBackwardInternalVariant":
                case "Lift1DownBackwardInternalVariant":
                case "Lift2UpBackwardInternalVariant":
                case "Lift2DownBackwardInternalVariant":
                    transform = building.Transform;
                    break;
                default:
                    transform = building.Transform.Rotate(GridRotation.Rotate180);
                    break;
            }
        }

    }
}
