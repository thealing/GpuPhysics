namespace Simulator.Backend.Common;

using Simulator.Engine;
using Simulator.Engine.Geometry;

public struct Sphere : ISphere
{
	public readonly Vector Center => _worldCenter;
	public readonly float Radius => _radius;

	public Sphere(Vector center, float radius)
	{
		_localCenter = center;
		_worldCenter = center;
		_radius = radius;
	}

	public void SetTransform(Transform transform)
	{
		_worldCenter = transform * _localCenter;
	}

	private readonly Vector _localCenter;
	private readonly float _radius;
	private Vector _worldCenter;
}
