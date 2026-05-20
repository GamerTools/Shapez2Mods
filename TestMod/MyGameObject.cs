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
        var inDir = trainData.Head.Incoming;
        var outDir = trainData.Head.Outgoing;
        var pos = outDir.Position.ToOrigin_G();
        var camera = MyMod.SessionDependencyContainer.Resolve<CameraController>();
        camera.CurrentPosition = new double2(pos.x + 10, -pos.y - 10);
        // TODO: Get the train model's positiona and rotation.
        // TODO: Set the camera position (not the target).

        // This should make the camera change rotation only if the train is actually turning, and not when it is just moving straight.
        if (inDir.Direction.Value == outDir.Direction.Value) { return; }
        // TODO: Compute the difference in rotation between the incoming and outgoing direction, and add it to the current camera rotation, instead of just snapping to the outgoing direction.
        switch (outDir.Direction.Value)
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
