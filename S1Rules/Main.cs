using Core.Logging;
using Unity.Mathematics;
using MonoMod.RuntimeDetour;
using ShapezShifter.SharpDetour;
using System;

namespace S1Rules
{
    public class Main : IMod
    {
        private Hook simHook;

        public void MakeHooks()
        {
            simHook = DetourHelper.StaticReplace<ShapePartReference, ShapePartReference, int, bool>(
                original: (a, b, partCount) => ShapeLogic.Logic_IsConnected(a, b, partCount),
                replacement: Logic_IsConnected);
        }

        public Main(ILogger logger)
        {
            MakeHooks();
        }

        public void Dispose()
        {
            simHook.Dispose();
        }

        public static bool Logic_IsConnected(ShapePartReference a, ShapePartReference b, int partCount)
        {
            if (a.Part.Shape.Code == 'P' || b.Part.Shape.Code == 'P')
            {
                return false;
            }
            if (a.LayerIndex == b.LayerIndex)
            {
                return true;
            }
            if (math.abs(a.LayerIndex - b.LayerIndex) == 1 && math.abs(a.PartIndex - b.PartIndex) < 2)
            {
                return true;
            }

            return false;
        }

    }
}
