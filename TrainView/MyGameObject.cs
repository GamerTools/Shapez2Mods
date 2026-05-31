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

        private TrainId CurrentTrainId = TrainId.Invalid;
        private NativeArray<TrainId> TrainIds;
        private TrainsSimulation TrainSim;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        void Update()
        {
            // Check if train was destroyed and unbind if so.
            try
            {
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
                PickTrain(-1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.periodKey.wasPressedThisFrame)
            {
                PickTrain(+1);
            }
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.slashKey.wasPressedThisFrame)
            {
                CurrentTrainId = TrainId.Invalid;
                return;
            }
            if (CurrentTrainId == TrainId.Invalid)
            {
                return;
            }

            var trainData = TrainSim.GetTrainData(CurrentTrainId);
            var camera = Main.SessionDependencyContainer.Resolve<CameraController>();
            var (pos, rot) = TrainSimulationHelper.CalculateTrainPosition(trainData);
            ConvertToCamera(ref pos, ref rot);
            camera.CurrentPosition = pos;

            // Rotate the camera only if the camera is zoomed in.
            var zoom = GameHelper.Core.Viewport.Zoom;
            if (zoom < 75f)
            {
                camera.TargetRotationDegrees = rot;
            }
        }

        public static void ConvertToCamera(ref double2 pos, ref float rot)
        {
            pos.y = -pos.y;
        }

        private void PickTrain(int dir)
        {
            if (TrainIds.IsCreated)
            {
                TrainIds.Dispose();
            }

            var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
            TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
            TrainIds = TrainSim.GetAllTrains(Allocator.Persistent);
            //_logger.Info.Log($"TrainIds length {TrainIds.Length}");
            if (TrainIds.Length == 0)
            {
                CurrentTrainId = TrainId.Invalid;
                return;
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
            CurrentTrainId = TrainIds[index];
            //Console.WriteLine($"CurrentTrainId: {CurrentTrainId}");
        }

        void OnDestroy()
        {
            if (TrainIds.IsCreated)
            {
                TrainIds.Dispose();
            }
        }
    }
}
