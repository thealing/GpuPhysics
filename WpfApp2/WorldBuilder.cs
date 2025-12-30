namespace WpfApp2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Simulator.Backend.Cpu;
using Simulator.Engine;
using Simulator.Backend.Common;
using Simulator.Engine.Geometry;

public class WorldBuilder
{
	public WorldStorage storage;
	public Example example;
	public ShapeType shapeType;
	public int objectCount;
	public int shapeCountPerBody;
	public float minSize;
	public float maxSize;

	public Vector cameraPosition;
	public Vector cameraDirection;

	public WorldBuilder()
	{
		// defaults
		R = null!;
		objectCount = 1000;
		shapeCountPerBody = 1;
		minSize = 5;
		maxSize = 15;

		storage.Config.Gravity.Linear.Y = -10;

		storage.Config.DeltaTime = 1f / 60f;

		storage.Config.IterationCount = 10;

		storage.Config.UseWarmStarting = 0;

		storage.Config.CorrectionVelocityFactor = 0.3f;
		storage.Config.CorrectionVelocityLimit = 15f;
	}

	private Random R;

	public void Build()
	{
		// deterministion wprld
		R = new Random(1);

		float sizeLength = maxSize * 7.5f;

		switch (example)
		{
			case Example.Pit:
			{
				float size = Math.Max(1, maxSize / 10);

				var cs = new Vector(sizeLength, maxSize * 2 + objectCount * size, sizeLength);
				CreateChamber(cs);

				for (int i = 0; i < objectCount; i++)
				{
					float size2 = Random(minSize, maxSize);

					float x = Random(-sizeLength, sizeLength);
					float z = Random(-sizeLength, sizeLength);
					Vector position = new Vector(x, size2 + i * size, z);

					AddObject(position, size2, 0.0f, 0.75f);
				}

				cameraPosition = new Vector(0, sizeLength * 1.0f + cs.Y / 20, sizeLength * 4 + cs.Y / 10);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			case Example.Piramid:
			{
				float size = maxSize;

				float r = size;   // same radius you used
				float diameter = 2 * r;

				int baseCount = 0; // approximate
				{
					int total = 0;

					while (true)
					{
						int next = total + (baseCount + 1) * (baseCount + 1);
						if (next > objectCount)
							break;

						baseCount++;
						total = next;
					}
				}

				float layerHeight = RealMath.Sqrt(8.0f / 3.0f) * r;

				int placed = 0;

				float maxY = 0;

				for (int layer = 0; placed < objectCount; layer++)
				{
					int count = baseCount - layer;
					if (count <= 0)
						break;

					float y = r + layer * layerHeight;

					maxY = y;

					// center pyramid
					float offset = (count - 1) * diameter * 0.5f;

					for (int i = 0; i < count && placed < objectCount; i++)
					{
						for (int j = 0; j < count && placed < objectCount; j++)
						{
							float x = (i * diameter) - offset;
							float z = (j * diameter) - offset;

							AddObject(new Vector(x, y, z), r, 0, 1, layer == 0 ? 0 : 1);

							placed++;
						}
					}
				}

				var cs = new Vector(baseCount * maxSize * 2, maxY * 2, baseCount * maxSize * 2);
				CreateChamber(cs);

				cameraPosition = new Vector(cs.X * 0.5f, cs.Y * 0.5f, cs.Z * 0.9f);
				cameraDirection = new Vector(0, cs.Y * 0.2f, 0) - cameraPosition;

				break;
			}
			case Example.Stack:
			{
				var cs = new Vector(sizeLength, maxSize * objectCount, sizeLength);
				CreateChamber(cs);

				for (int i = 0; i < objectCount; i++)
				{
					float size = Random(minSize, maxSize);
					Vector position = new Vector(1, size + maxSize * 2 * i, 1);

					AddObject(position, size, 0.0f, 0.75f);
				}

				cameraPosition = new Vector(0, sizeLength * 1.0f + cs.Y / 200, sizeLength * 4 + cs.Y / 100);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			case Example.Dominoes:
			{
				CreateChamber(new Vector(maxSize * objectCount, maxSize * 20, maxSize * objectCount));

				float width = maxSize * 0.4f;
				float height = maxSize * 2.0f;
				float depth = maxSize * 0.6f;

				float spacing = depth * 0.9f;   // overlap for guaranteed contact
				float spiralStep = width * 0.6f;

				float theta = 0.0f;
				float radius = maxSize * 3.0f;

				for (int i = 0; i < objectCount; i++)
				{
					float x = RealMath.Cos(theta) * radius;
					float z = RealMath.Sin(theta) * radius;

					float y = height * 0.5f; // on the ground

					Vector position = new Vector(x, y, z);

					// Tangent direction of the spiral
					Vector tangent = new Vector(-RealMath.Sin(theta), 0, RealMath.Cos(theta));
					float yaw = MathF.Atan2(tangent.X, tangent.Z);

					Quaternion rotation = FromAxisAngle(new Vector(0, 1, 0), yaw);

					Vector sizeMultiplier = new Vector(0.7f, 0.5f, 0.3f);

					if (i == 0) // first one
					{
						float tiltAngle = 0.25f; // ~14 degrees, realistic;

						Quaternion tilt = FromAxisAngle(new Vector(1, 0, 0), tiltAngle);

						AddStableQube(position, new Vector(depth, height, width) * sizeMultiplier, tilt * rotation, 1, 1);
					}
					else
					{
						AddStableQube(position, new Vector(depth, height, width) * sizeMultiplier, rotation, 1, 1);
					}

					float arcStep = spacing / radius;
					theta += arcStep;
					radius += spiralStep * arcStep * 2f;
				}

				cameraPosition = new Vector(0, radius * 1.4f, radius * 1.4f);
				cameraDirection = -cameraPosition;

				break;
			}
			case Example.Table:
			{
				sizeLength = RealMath.Sqrt(objectCount) * 4 * maxSize * maxSize;

				CreateChamber(new Vector(sizeLength, maxSize * 20, sizeLength));

				int legCount = Math.Max(1, objectCount / 2);
				int topCount = Math.Max(1, objectCount - legCount);

				float tableRadius = sizeLength * 0.3f;
				float legHeight = maxSize * 3.0f;
				float legRadius = tableRadius;

				float legWidth = maxSize * 0.4f;
				float legDepth = maxSize * 0.4f;

				float topThickness = maxSize * 0.3f;
				float topHeight = legHeight + topThickness;

				float TwoPi = MathF.Tau;

				// ---- Legs ----
				for (int i = 0; i < topCount; i++)
				{
					float angle = (TwoPi * i) / legCount;

					float x = RealMath.Cos(angle) * legRadius;
					float z = RealMath.Sin(angle) * legRadius;
					float y = legHeight * 0.5f;

					AddQube(new Vector(x, y, z), new Vector(legDepth, legHeight / 2, legWidth), Quaternion.Identity, 1);
				}

				// ---- Table top (cylinder made of cubes) ----
				float arcLength = (TwoPi * tableRadius) / Math.Max(2, topCount);
				float cubeDepth = arcLength * 0.9f;

				int tableBody = storage.AddBody(new());

				for (int i = 0; i < topCount; i++)
				{
					float angle = (TwoPi * i) / legCount;

					float x = RealMath.Cos(angle) * tableRadius;
					float z = RealMath.Sin(angle) * tableRadius;
					float y = topHeight;

					// Tangential orientation
					float yaw = angle + MathF.PI / 2;
					Quaternion rotation = FromAxisAngle(new Vector(0, 1, 0), MathF.PI - yaw);

					PolyhedronDefinition pd = PolyhedronCreator.CreateCubeDefinition();

					pd.Scale(new Vector(cubeDepth, topThickness, maxSize * 1.2f) * RealMath.Sqrt(3));

					pd.Transform(new Transform(new Vector(x, y, z), rotation));

					ShapeDefinition shape = new();

					shape.BodyIndex = tableBody;
					shape.Density = 1;

					shape.Material.Restitution = 0;
					shape.Material.DynamicFriction = 1;
					shape.Material.StaticFriction = 1;

					storage.AddPolyhedronShape(shape, pd);
				}

				cameraPosition = new Vector(0, tableRadius * 0.2f, tableRadius * 0.2f);
				cameraDirection = new Vector(tableRadius * 0.4f, -tableRadius * 0.2f, -tableRadius * 0.4f);

				break;
			}
			case Example.Thumbler:
			{
				// Thumbler size
				sizeLength = (MathF.Cbrt(objectCount) + 2) * maxSize * 4;

				CreateChamber(new Vector(sizeLength, sizeLength, sizeLength));

				float boxSize = sizeLength / 3;
				float wallThickness = maxSize;
				float half = boxSize;

				Vector wallScaleX = new Vector(wallThickness, boxSize, boxSize);
				Vector wallScaleY = new Vector(boxSize, wallThickness, boxSize);
				Vector wallScaleZ = new Vector(boxSize, boxSize, wallThickness);

				Vector center = new Vector(0, sizeLength / 2, 0);

				{
					BodyDefinition bodyDef = new();

					bodyDef.Transform.Position = center;
					bodyDef.Transform.Rotation = Quaternion.Identity;

					// Spinning velocity here, must increate the correction impulse limit up to handle it
					bodyDef.Velocity.Angular.Z = 1;

					int body = storage.AddBody(bodyDef);

					// ---- Bottom ----
					AddQube(body, new Vector(0, -half+wallThickness * 0.5f, 0), wallScaleY, 0);

					// ---- Top ----
					AddQube(body, new Vector(0, half- wallThickness * 0.5f, 0), wallScaleY, 0);

					// ---- +X / -X ----
					AddQube(body, new Vector(half - wallThickness * 0.5f, 0, 0), wallScaleX, 0);
					AddQube(body, new Vector(-half + wallThickness * 0.5f, 0, 0), wallScaleX, 0);

					// ---- +Z / -Z ----
					AddQube(body, new Vector(0, 0, half - wallThickness * 0.5f), wallScaleZ, 0);
					AddQube(body, new Vector(0, 0, -half + wallThickness * 0.5f), wallScaleZ, 0);
				}

				// ---- Spam objects inside ----
				int spamCount = objectCount;
				float spamSize = maxSize * 0.6f;

				Random rng = R;

				for (int i = 0; i < spamCount; i++)
				{
					float x = (float)(rng.NextDouble() * (boxSize - wallThickness * 2) - (half - wallThickness));
					float y = (float)(rng.NextDouble() * (boxSize - wallThickness * 2) + wallThickness);
					float z = (float)(rng.NextDouble() * (boxSize - wallThickness * 2) - (half - wallThickness));

					// NO RESTUTION, NO FRICTION

					AddObject(RandomPosition(center - half * 0.8f, center + half * 0.8f), Random(minSize, maxSize), 0, 0);
				}

				cameraPosition = new Vector(0, center.Y, sizeLength);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			case Example.Downhill_Slope:
			{
				int spawnObjectCount = 100;

				float wallHeight = maxSize * 30;
				float wallThickness = maxSize;
				float bottomThickness = maxSize;

				float bowlSize = Math.Max(wallHeight, (maxSize + 3) * spawnObjectCount / 3 * 4);

				float tiltAngle = 1.15f; // ~20 degrees, good sliding angle

				float xm = wallHeight * 3;

				// ---- Bottom ----
				AddQube(new Vector(0, bottomThickness * 0.5f, 0), new Vector(xm, bottomThickness, bowlSize));

				// ---- Walls ----
				float wallY = wallHeight * MathF.Cos(tiltAngle);
				float wallDistance = wallY * 2 + wallThickness;

				float wallAdd = wallThickness;

				// +X wall
				AddQube(new Vector(xm + wallDistance, wallY, 0), new Vector(wallThickness, wallHeight, bowlSize + wallAdd), FromAxisAngle(new Vector(0, 0, 1), -tiltAngle), 0);

				// -X wall
				AddQube(new Vector(-xm - 0, wallY, 0), new Vector(wallThickness, wallHeight, bowlSize), FromAxisAngle(new Vector(0, 0, 1), 0), 0);

				// +Z wall
				AddQube(new Vector(0, wallY, bowlSize + 0), new Vector(xm, wallHeight, wallThickness), FromAxisAngle(new Vector(1, 0, 0), 0), 0);

				// -Z wall
				AddQube(new Vector(0, wallY, -bowlSize - 0), new Vector(xm, wallHeight, wallThickness), FromAxisAngle(new Vector(1, 0, 0), -0), 0);

				// ---- Objects around the bowl (same Y level) ----
				int remaining = spawnObjectCount - 5;
				float spawnY = wallY * 2f + maxSize * (1 + MathF.Cos(tiltAngle)) + wallThickness  * MathF.Sin(tiltAngle);
				float ringRadius = bowlSize * 0.55f;

				// ---- Spawn objects on wall tops ----
				int perWall = spawnObjectCount;
				float span = bowlSize * 0.45f;

				for (int i = 0; i < perWall; i++)
				{
					float r = xm + wallDistance + wallHeight * MathF.Sin(tiltAngle) - maxSize * 2;

					float t = -bowlSize * 0.9f + bowlSize * 0.9f * 2 * (i + 0.5f) / perWall + maxSize;

					float friction = 0.25f * i / (perWall - 1) * 4;

					AddStableQube(new Vector(r, spawnY, t), new Vector(maxSize), FromAxisAngle(new Vector(0, 0, 1), -tiltAngle), 1, 0.00f + friction);
				}

				cameraPosition = new Vector(-bowlSize * 1.5f, bowlSize * 1, bowlSize * 0.4f);
				cameraDirection = new Vector(bowlSize * 2, -bowlSize * 1, -bowlSize * 0.6f);

				break;
			}
			case Example.Mass_Ratio:
			{
				// Use the square root of objectCount to build a roughly square grid of columns
				int c2 = (int)MathF.Sqrt(objectCount);

				CreateChamber(new Vector(maxSize * c2 * 2, maxSize * c2 * 2, maxSize * c2 * 3));

				// Temporarily override the shape type to isolate mass effects
				var shapeType1 = shapeType;
				shapeType = ShapeType.Sphere;

				for (int c = 0; c < c2; c++)
				{
					// Position each column along the X axis
					float x = maxSize * 3 * (c - c2 / 2);

					float y = maxSize * 0;

					// Stack c2 objects vertically per column
					for (int i = 0; i < c2; i++)
					{
						// The top object in each column is large (heavy),
						// all others are small (light)
						float s = i == c2 - 1 ? maxSize : minSize;

						y += s;

						// Add a sphere at the computed position
						// Mass index increases only for columns that have a large top object

						AddObject(new Vector(x, y, 0), s, 0, 0, 1 + c * (s == maxSize ? 1 : 0));

						y += s;

						if (i == c2 - 2)
						{
							y += maxSize * 10;
						}
					}
				}

				// Restore the previous shape type
				shapeType = shapeType1;

				cameraPosition = new Vector(0, maxSize * c2, maxSize * c2 * 3);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			case Example.Restitution_1:
			{
				// Use the square root of objectCount to build a square grid in XZ
				int c2 = (int)MathF.Sqrt(objectCount);

				// Create a chamber large enough to contain the full grid and bounce height
				CreateChamber(new Vector(maxSize * c2 * 2, maxSize * 20, maxSize * c2 * 5));

				// Temporarily switch to spheres to remove rotational effects
				var shapeType1 = shapeType;
				shapeType = ShapeType.Sphere;

				int placed = 0;
				bool wasOverOne = false;

				// Iterate over a square grid in XZ
				for (int xIndex = 0; xIndex < c2; xIndex++)
				{
					for (int zIndex = 0; zIndex < c2 && placed < objectCount; zIndex++)
					{
						// Compute linear column index
						int columnIndex = xIndex * c2 + zIndex;

						// Stop if we exceed the requested object count
						if (columnIndex >= objectCount)
							break;

						// Distribute restitution linearly from 0 to 1 across columns
						float restitution = 0.5f + 0.5f * (float)placed / (objectCount - 1) * 2;

						if (!wasOverOne && restitution >= 1)
						{
							wasOverOne = true;
							restitution = 1;
						}

						placed++;

						// Position objects in a centered square grid on the XZ plane
						float z = -maxSize * 3 * (xIndex - c2 / 2);
						float x = maxSize * 3 * (zIndex - c2 / 2);

						// Start each sphere high enough to observe bounce height clearly
						float y = maxSize * 10;

						// Add one sphere per grid cell
						// Mass is constant, only restitution changes (assuming it's the third parameter)
						AddObject(new Vector(x, y, z), maxSize, restitution, 0, 1);
					}
				}

				// Restore original shape type
				shapeType = shapeType1;

				cameraPosition = new Vector(0, maxSize * c2, maxSize * c2 * 4);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			case Example.Restitution_2:
			{
				// Use the square root of objectCount to build a square grid in XY
				int c2 = (int)MathF.Sqrt(objectCount);

				// Create a chamber large enough to contain the full grid and bounce depth
				CreateChamber(new Vector(maxSize * c2 * 2, maxSize * c2 * 2, maxSize * objectCount));

				// Temporarily switch to spheres to remove rotational effects
				var shapeType1 = shapeType;
				shapeType = ShapeType.Sphere;

				bool wasOverOne = false;

				// Iterate over a square grid in XY
				for (int xIndex = 0; xIndex < c2; xIndex++)
				{
					for (int yIndex = 0; yIndex < c2; yIndex++)
					{
						// Compute linear column index
						int columnIndex = xIndex * c2 + yIndex;

						// Stop if we exceed the requested object count
						if (columnIndex >= objectCount)
							break;

						// Distribute restitution linearly from 0 to 1 across columns

						float d = 1f / Math.Max(1, c2);

						float restitution = 1 - d + d * (float)xIndex / (c2 - 1) * 2;

						if (!wasOverOne && restitution >= 1)
						{
							wasOverOne = true;
							restitution = 1;
						}

						// Position objects in a centered square grid on the XY plane
						float x = maxSize * 3 * (xIndex - c2 / 2);
						float y = maxSize * 3 * (yIndex + 1);

						// Place spheres offset along Z so they can fall and bounce
						float z = maxSize * 0;

						// Add one sphere per grid cell
						// Mass is constant, only restitution changes
						AddObject(new Vector(x, y, z), maxSize, restitution, 0, 1);
					}
				}

				// Restore original shape type
				shapeType = shapeType1;

				cameraPosition = new Vector(0, maxSize * c2 * 1.7f, maxSize * c2 * 5);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			case Example.Restitution_3:
			{
				float size = Math.Max(1, maxSize / 10);

				for (int v = -1; v <= 1; v++)
				{
					var offset = new Vector(v * sizeLength * 5, 0, 0);

					CreateChamber(offset, new Vector(sizeLength, maxSize * 2 + objectCount * size / 4.5f, sizeLength), true, maxSize);

					for (int i = 0; i < objectCount / 3; i++)
					{
						float size2 = Random(minSize, maxSize);

						float x = Random(-sizeLength, sizeLength);
						float z = Random(-sizeLength, sizeLength);
						Vector position = new Vector(x, size2 + i * size, z);

						AddObject(offset + position, size2, 1f + 0.1f * v, 0.0f);
					}
				}

				cameraPosition = new Vector(0, maxSize * 2 + objectCount * size / 4.3f, sizeLength * 11 + objectCount * size / 2);
				cameraDirection = new Vector(0, 0, -1);

				break;
			}
			default:
			{
				throw new NotImplementedException();
			}
		}
	}

	private float Random(float min, float max)
	{
		return (float)R.NextDouble() * (max - min) + min;
	}

	private Vector RandomPosition(Vector min, Vector max)
	{
		float x = (float)(R.NextDouble() * (max.X - min.X) + min.X);
		float y = (float)(R.NextDouble() * (max.Y - min.Y) + min.Y);
		float z = (float)(R.NextDouble() * (max.Z - min.Z) + min.Z);
		return new Vector(x, y, z);
	}

	private void CreateChamber(Vector size)
	{
		CreateChamber(Vector.Zero, size, false, maxSize);
	}

	private void CreateChamber(Vector position, Vector size, bool top, float thickness)
	{
		if (top)
		{
			AddQube(position + new Vector(0, size.Y * 2 + thickness, 0), new Vector(size.X, thickness, size.Z));
		}
		AddQube(position + new Vector(0, -thickness, 0), new Vector(size.X, thickness, size.Z));
		AddQube(position + new Vector(-size.X - thickness, size.Y, 0), new Vector(thickness, size.Y, size.Z));
		AddQube(position + new Vector(size.X + thickness, size.Y, 0), new Vector(thickness, size.Y, size.Z));
		AddQube(position + new Vector(0, size.Y, -thickness - size.Z), new Vector(size.X, size.Y, thickness));
		AddQube(position + new Vector(0, size.Y, thickness + size.Z), new Vector(size.X, size.Y, thickness));
	}

	private void AddQube(Vector position, Vector scale)
	{
		AddQube(position, scale, Quaternion.Identity, 0);
	}

	private void AddQube(Vector position, Vector scale, Quaternion rotation, float density)
	{
		AddQube(position, scale, rotation, density, 1);
	}

	private void AddQube(Vector position, Vector scale, Quaternion rotation, float density, float friction)
	{
		BodyDefinition body = new();

		body.Transform.Position = position;
		body.Transform.Rotation = rotation;

		int bodyIndex = storage.AddBody(body);

		ShapeDefinition shape = new();

		shape.BodyIndex = bodyIndex;
		shape.Density = density;

		shape.Material.Restitution = 0;
		shape.Material.DynamicFriction = friction * friction;
		shape.Material.StaticFriction = friction * friction;

		PolyhedronDefinition pd = PolyhedronCreator.CreateCubeDefinition();

		pd.Scale(scale * RealMath.Sqrt(3));

		storage.AddPolyhedronShape(shape, pd);
	}

	private void AddStableQube(Vector position, Vector scale, Quaternion rotation, float density, float friction)
	{
		// 8 cubes as the octants, so 4 contact points will be generated when they collide with the gorund

		BodyDefinition body = new();

		body.Transform.Position = position;
		body.Transform.Rotation = rotation;

		int bodyIndex = storage.AddBody(body);

		ShapeDefinition shape = new();

		shape.BodyIndex = bodyIndex;
		shape.Density = density;

		friction /= 2; // results in expected behaviour
		scale /= 2;

		shape.Material.Restitution = 0;
		shape.Material.DynamicFriction = friction * friction;
		shape.Material.StaticFriction = friction * friction;

		for (int x = -1; x <= 1; x += 2)
		{
			for (int y = -1; y <= 1; y += 2)
			{
				for (int z = -1; z <= 1; z += 2)
				{
					PolyhedronDefinition pd = PolyhedronCreator.CreateCubeDefinition();

					pd.Scale(scale * RealMath.Sqrt(3));

					pd.Transform(new Transform(new Vector(x, y, z) * scale, Quaternion.Identity));

					storage.AddPolyhedronShape(shape, pd);
				}
			}
		}
	}

	private void AddQube(int body, Vector position, Vector scale, float density)
	{
		ShapeDefinition shape = new();

		shape.BodyIndex = body;
		shape.Density = density;

		shape.Material.Restitution = 0;
		shape.Material.DynamicFriction = 1;
		shape.Material.StaticFriction = 1;

		PolyhedronDefinition pd = PolyhedronCreator.CreateCubeDefinition();

		Stabilize(ref pd, density);

		pd.Scale(scale * RealMath.Sqrt(3));

		pd.Transform(new Transform(position, Quaternion.Identity));

		storage.AddPolyhedronShape(shape, pd);
	}

	private void AddObject(Vector position, float size, float restitution, float friction, float density = 1)
	{
		BodyDefinition body = new();

		int bodyIndex = storage.AddBody(body);

		if (shapeCountPerBody != 1)
		{
			size /= (shapeCountPerBody * 2.5f) / MathF.Tau + 1f;
		}

		float shapeRadius = size;
		float spacing = shapeRadius * 2.5f;
		float circleRadius = (shapeCountPerBody * size * 2.5f) / MathF.Tau;

		for (int i = 0; i < shapeCountPerBody; i++)
		{
			float angle = (MathF.Tau * i) / shapeCountPerBody;

			Vector localOffset = new Vector(RealMath.Cos(angle) * circleRadius, 0, RealMath.Sin(angle) * circleRadius);

			if (shapeCountPerBody == 1)
			{
				localOffset = Vector.Zero;
			}

			Vector shapePosition = position + localOffset;

			ShapeDefinition shape = new();

			shape.BodyIndex = bodyIndex;
			shape.Density = density;

			shape.Material.Restitution = restitution;

			//Ground friction is one, combination formula is SQRT(friction1 * friction2)
			shape.Material.DynamicFriction = RealMath.Square(friction);
			shape.Material.StaticFriction = RealMath.Square(friction);

			PolyhedronDefinition? pd = null;

			switch (shapeType)
			{
				case ShapeType.Sphere:
				{
					storage.AddSphereShape(shape, shapePosition, size);
					continue;
				}
				case ShapeType.Tetrahedron:
				{
					pd = PolyhedronCreator.CreateTetrahedronDefinition();
					break;
				}
				case ShapeType.Cube:
				{
					pd = PolyhedronCreator.CreateCubeDefinition();
					break;
				}
				case ShapeType.Octahedron:
				{
					pd = PolyhedronCreator.CreateOctahedronDefinition();
					break;
				}
				case ShapeType.Icosahedron:
				{
					pd = PolyhedronCreator.CreateIcosahedronDefinition();
					break;
				}
				case ShapeType.Dodecahedron:
				{
					pd = PolyhedronCreator.CreateDodecahedronDefinition();
					break;
				}
				case ShapeType.Random:
				{
					if (R.Next(6) == 1)
					{
						storage.AddSphereShape(shape, shapePosition, size);
						continue;
					}
					pd = Program.CreateRandomPlatonicSolidDefinition(R);
					break;
				}
				default:
				{
					throw new NotImplementedException();
				}
			}

			if (pd != null)
			{
				Stabilize(ref pd, density);
				pd.Scale(new Vector(size));
				pd.Transform(new Transform(shapePosition, Quaternion.Identity));
				storage.AddPolyhedronShape(shape, pd);
			}
		}
	}

	private static Quaternion FromAxisAngle(Vector axis, float angle)
	{
		axis = axis.Normalize();

		float half = angle * 0.5f;
		float s = RealMath.Sin(half);

		return new Quaternion(axis.X * s, axis.Y * s, axis.Z * s, RealMath.Cos(half));
	}

	private void Stabilize(ref PolyhedronDefinition pd, float density)
	{
	}
}
