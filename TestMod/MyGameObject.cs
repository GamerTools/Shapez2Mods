using Game.Core.Trains;
using ShapezShifter.Kit;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

public class MyGameObject : MonoBehaviour
{
    private static ILogger _logger;
    private TrainId CurrentTrainId = TrainId.Invalid;
    private NativeArray<TrainId> TrainIds;
    private TrainsSimulation TrainSim;
    private GameObject CurrentTrainGameObject;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
        GameObjectHelper.SetLogger(logger);
    }

    void Update()
    {
        // Check if period key is pressed
        // TODO: add keys for next, previous, and unbind.
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.periodKey.wasPressedThisFrame)
        {
            ToggleTrain();
        }
        if (CurrentTrainId == TrainId.Invalid)
        {
            return;
        }

        var trainData = TrainSim.GetTrainData(CurrentTrainId);
        var camera = MyMod.SessionDependencyContainer.Resolve<CameraController>();
        var (pos, rot) = TrainSimulationHelper.CalculateTrainPosition(trainData);
        ConvertToCamera(ref pos, ref rot);
        camera.CurrentPosition = pos;
        camera.TargetRotationDegrees = rot;
    }

    // TODO: Should depend on the current mode.
    public static void ConvertToCamera(ref double2 pos, ref float rot)
    {
        pos.y = -pos.y;
    }

    private void ToggleTrain()
    {
        if (TrainIds.IsCreated)
        {
            TrainIds.Dispose();
        }

        // Clear the current train GameObject reference
        CurrentTrainGameObject = null;

        var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
        TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
        TrainIds = TrainSim.GetAllTrains(Allocator.Persistent);
        //_logger.Info.Log($"TrainIds {TrainIds}");
        if (TrainIds.Length == 0)
        {
            return;
        }
        if (CurrentTrainId == TrainId.Invalid)
        {
            // Pick the first train id
            CurrentTrainId = TrainIds[0];
            return;
        }
        // Pick the next train id, or unset it if it is at the end of the list.
        int i = TrainIds.IndexOf(CurrentTrainId);
        if (i < TrainIds.Length - 1)
        {
            CurrentTrainId = TrainIds[i + 1];
        }
        else
        {
            CurrentTrainId = TrainId.Invalid;
        }
    }

    void OnDestroy()
    {
        if (TrainIds.IsCreated)
        {
            TrainIds.Dispose();
        }
    }
}
