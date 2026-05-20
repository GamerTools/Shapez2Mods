using Assimp;
using Game.Core.Coordinates;
using Game.Core.Trains;
using MonoMod.Cil;
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
            ToggleTrain();
        }
        if (CurrentTrainId == TrainId.Invalid)
        {
            return;
        }
        var trainData = TrainSim.GetTrainData(CurrentTrainId);
        var dir = trainData.Head.Outgoing;
        var pos = dir.Position.ToOrigin_G();
        var camera = MyMod.SessionDependencyContainer.Resolve<CameraController>();
        camera.CurrentPosition = new double2(pos.x + 10, -pos.y - 10);
        switch (dir.Direction.Value)
        {
            case ChunkDirection.Serializable.North:
                camera.TargetRotationDegrees = 0f;
                break;
            case ChunkDirection.Serializable.East:
                camera.TargetRotationDegrees = 90f;
                break;
            case ChunkDirection.Serializable.South:
                camera.TargetRotationDegrees = 180f;
                break;
            case ChunkDirection.Serializable.West:
                camera.TargetRotationDegrees = 270f;
                break;
        }
    }

    private void ToggleTrain()
    {
        if (TrainIds.IsCreated)
        {
            TrainIds.Dispose();
        }
        var sim = GameHelper.Core.LocalPlayer.CurrentMap.Simulator;
        TrainSim = sim.GetSystem<TrainSystem>().TrainsSimulation;
        TrainIds = TrainSim.GetAllTrains(Allocator.Persistent);
        _logger.Info.Log($"TrainIds {TrainIds}");
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
