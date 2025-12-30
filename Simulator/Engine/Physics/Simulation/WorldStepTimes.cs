namespace Simulator.Engine.Physics.Simulation;

public struct WorldStepTimes
{
	public double PreparationTime;
	public double ShapeUpdateTime;
	public double BodyUpdateTime;
	public double CollisionDetectionTime;
	public double ContactPreparationTime;
	public double GravityApplicationTime;
	public double ContactCacheLoadingTime;
	public double ContactWarmStartingTime;
	public double ContactResolutionTime;
	public double ContactCacheSavingTime;
	public double BodyFinalizationTime;
}
