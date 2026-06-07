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

        public static TrainId CurrentTrainId = TrainId.Invalid;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        void Update()
        {
            // Check if train was destroyed and unbind if so.
            if (CurrentTrainId != TrainId.Invalid)
            {
                try
                {
                    var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
                    var TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
                    TrainSim.GetTrainData(CurrentTrainId);
                }
                catch (Exception ex)
                {
                    _logger.Info.Log($"Train destroyed: {CurrentTrainId}");
                    CurrentTrainId = TrainId.Invalid;
                }
            }

            // Check which key was pressed
            // TODO: Use the game's keybinding system instead of hardcoding keys here.
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.commaKey.wasPressedThisFrame)
            {
                CurrentTrainId = PickTrain(-1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.periodKey.wasPressedThisFrame)
            {
                CurrentTrainId = PickTrain(+1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.slashKey.wasPressedThisFrame)
            {
                _logger.Info.Log($"Unbind train: {CurrentTrainId}");
                CurrentTrainId = TrainId.Invalid;
            }
        }

        private TrainId PickTrain(int dir)
        {
            var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
            var TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
            var TrainIds = TrainSim.GetAllTrains(Allocator.Temp);
            //_logger.Info.Log($"TrainIds length {TrainIds.Length}");
            if (TrainIds.Length == 0)
            {
                return TrainId.Invalid;
            }

            int index;
            if (CurrentTrainId == TrainId.Invalid)
            {
                index = dir == 1 ? 0 : TrainIds.Length - 1;
            }
            else
            {
                index = TrainIds.IndexOf(CurrentTrainId);
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
