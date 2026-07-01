using Game.Core.Trains;
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
                    Main.trainSim.GetTrainData(currentTrainId);
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
                currentTrainId = PickTrain(currentTrainId, -1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.periodKey.wasPressedThisFrame)
            {
                currentTrainId = PickTrain(currentTrainId, +1);
            }
            if (currentTrainId != TrainId.Invalid &&
                UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _logger.Info.Log($"Unbind train: {currentTrainId}");
                currentTrainId = TrainId.Invalid;
            }
            Main.currentTrainId = currentTrainId;
        }

        private TrainId PickTrain(TrainId currentTrainId, int dir)
        {
            var TrainIds = Main.trainSim.GetAllTrains(Allocator.Temp);
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
            TrainId trainId = TrainIds[index];
            _logger.Info.Log($"New TrainId: {trainId}");
            return trainId;
        }

        void OnDestroy()
        {
        }
    }
}
