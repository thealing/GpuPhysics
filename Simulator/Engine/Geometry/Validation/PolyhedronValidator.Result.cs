namespace Simulator.Engine.Geometry.Validation;

public static partial class PolyhedronValidator
{
	public enum Result
	{
		Valid,
		NotFinite,
		SidePointNotFound,
		FirstSidePointStartIndexIsNotZero,
		SidePointIndicesAreNotContiguous,
		SideHasTooFewPoints,
		SideNormalLengthIsNotCorrect,
		SideIsNotPlanar,
		SideWindingIsNotPositive,
		VolumeIsNotPositive,
		EdgeIsDuplicated,
		EdgeLeftSideNotFound,
		EdgeLeftSideIsNotCorrect,
		EdgeRightSideNotFound,
		EdgeRightSideIsNotCorrect,
		Count
	}
}
