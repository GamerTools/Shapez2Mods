using Game.Core.Trains;
using ShapezShifter.Kit;
using System;
using System.ComponentModel.Design.Serialization;
using Unity.Collections;
using Unity.Mathematics;
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
            try
            {
                var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
                var TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
                TrainSim.GetTrainData(CurrentTrainId);
            }
            catch (Exception ex)
            {
                CurrentTrainId = TrainId.Invalid;
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
                CurrentTrainId = TrainId.Invalid;
            }
            //_logger.Info.Log($"CurrentTrainId: {CurrentTrainId}");
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
            return TrainIds[index];
        }

        void OnDestroy()
        {
        }
    }
}
