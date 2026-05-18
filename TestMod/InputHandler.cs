using Game.Core.Coordinates;
using Game.Placement.Connectors;
using ShapezShifter.Flow;
using ShapezShifter.Kit;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

namespace ShapezShifter.Utilities
{
    /// <summary>
    /// MonoBehaviour component that handles keyboard input for debugging
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private static ILogger _logger;

        /// <summary>
        /// Sets the logger to use for output
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        void Update()
        {
            // Check if R key is pressed
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                LogSelectedBuildings();
                ReverseBelts();
            }
        }

        private void LogSelectedBuildings()
        {
            try
            {
                var buildingSelection = GameHelper.Core.LocalPlayer.InteractionState.BuildingSelection;

                if (buildingSelection == null)
                {
                    _logger?.Info.Log("BuildingSelection is null");
                    return;
                }

                int count = buildingSelection.Count;
                _logger?.Info.Log("=== Selected Buildings ===");
                _logger?.Info.Log($"Total selected buildings: {count}");

                if (count == 0)
                {
                    _logger?.Info.Log("No buildings selected");
                    _logger?.Info.Log("=== End of Selected Buildings ===");
                    return;
                }

                var sb = new StringBuilder();
                int index = 0;

                foreach (var building in buildingSelection)
                {
                    index++;

                    // Get basic building info
                    var buildingType = building.GetType();
                    var definition = building.Definition;

                    _logger?.Info.Log($"Building #{index} {building}");
                }

                _logger?.Info.Log("=== End of Selected Buildings ===");
            }

            catch (System.Exception ex)
            {
                _logger?.Info.Log($"Error logging selected buildings: {ex.Message}");
                _logger?.Info.Log($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ReverseBelts()
        {
            var buildingSelection = GameHelper.Core.LocalPlayer.InteractionState.BuildingSelection;
            //if (buildingSelection == null || buildingSelection.Count == 0)
            //{
            //    return;
            //}
            foreach (var building in buildingSelection)
            {
                var map = building.Map;
                //var definition = building.Definition;
                //var transform = build1ing.Transform;
                getNewDefinition(building, out var definition);
                getNewTransform(building, out var transform);
                //var config = def.CreateConfiguration();

                // Delete the old one
                map.DeleteBuilding(building.Id);

                // Create the new one
                if (definition != null)
                {
                    map.CreateBuilding(definition, transform, building.Configuration);
                }
            }
            buildingSelection.Clear();
        }

        private void getNewDefinition(BuildingModel building, out IBuildingDefinition definition)
        {
            var buildings = MyMod.SessionDependencyContainer.Resolve<BuildingsModulesLookup>().BuildingSimulationData;
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
                    transform = building.Transform.Rotate(GridRotation.RotateCCW);
                    break;
                case "BeltDefaultLeftInternalVariant":
                    transform = building.Transform.Rotate(GridRotation.RotateCW);
                    break;
                default:
                    transform = building.Transform.Rotate(GridRotation.Rotate180);
                    break;
            }
        }

    }
}
