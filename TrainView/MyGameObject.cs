using Game.Core.Trains;
using ShapezShifter.Kit;
using System;
using Unity.Collections;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

namespace TrainView
{
    public class MyGameObject : MonoBehaviour
    {
        private static ILogger _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        void Update()
        {
            TrainId currentTrainId = Main.currentTrainId;

            // Check if train was destroyed and unbind if so.
            if (currentTrainId != TrainId.Invalid)
            {
                try
                {
                    var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
                    var TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
                    TrainSim.GetTrainData(currentTrainId);
                }
                catch (Exception ex)
                {
                    _logger.Info.Log($"Train destroyed: {currentTrainId}");
                    currentTrainId = TrainId.Invalid;
                }
            }

            // Check which key was pressed
            // TODO: Use the game's keybinding system instead of hardcoding keys here.
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.commaKey.wasPressedThisFrame)
            {
                currentTrainId = PickTrain(-1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.periodKey.wasPressedThisFrame)
            {
                currentTrainId = PickTrain(+1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.slashKey.wasPressedThisFrame)
            {
                _logger.Info.Log($"Unbind train: {currentTrainId}");
                currentTrainId = TrainId.Invalid;
            }
            Main.currentTrainId = currentTrainId;
        }

        private TrainId PickTrain(int dir)
        {
            var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
            var TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
            var TrainIds = TrainSim.GetAllTrains(Allocator.Temp);
            TrainId currentTrainId = Main.currentTrainId;
            //_logger.Info.Log($"TrainIds length {TrainIds.Length}");
            if (TrainIds.Length == 0)
            {
                return TrainId.Invalid;
            }

            int index;
            if (currentTrainId == TrainId.Invalid)
            {
                index = dir == 1 ? 0 : TrainIds.Length - 1;
            }
            else
            {
                index = TrainIds.IndexOf(currentTrainId);
                index = (index + TrainIds.Length + dir) % TrainIds.Length;
            }
            var trainId = TrainIds[index];
            _logger.Info.Log($"New TrainId: {trainId}");
            return trainId;
        }

        void OnDestroy()
        {
        }
    }
}
