public interface ICornerCutterDrawData : IBuildingCustomDrawData
{
    IBeltLaneRendererDefinition InputLaneRenderingDefinition { get; }
    IBeltLaneRendererDefinition OutputLaneRenderingDefinition { get; }
}
