public readonly struct ShapeCornerCutResult
{
    public readonly ShapeCollapseResult LeftSide;
    public readonly ShapeCollapseResult RightSide;

    public ShapeCornerCutResult(ShapeCollapseResult leftSide, ShapeCollapseResult rightSide)
    {
        LeftSide = leftSide;
        RightSide = rightSide;
    }
}