
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MegaTriangle
{
	public int t;
	public Vector3 a, b, c;
	public Bounds bounds;

	public MegaTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 dir, int t)
	{
		this.t = t;
		this.a = a;
		this.b = b;
		this.c = c;

		//Vector3 cross = Vector3.Cross(b - a, c - a);

		Vector3 min = Vector3.Min(Vector3.Min(a, b), c);
		Vector3 max = Vector3.Max(Vector3.Max(a, b), c);
		bounds.SetMinMax(min, max);
	}

	public void Barycentric(Vector3 p, out float u, out float v, out float w)
	{
		Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
		float d00 = Vector3.Dot(v0, v0);
		float d01 = Vector3.Dot(v0, v1);
		float d11 = Vector3.Dot(v1, v1);
		float d20 = Vector3.Dot(v2, v0);
		float d21 = Vector3.Dot(v2, v1);
		float denom = 1f / (d00 * d11 - d01 * d01);
		v = (d11 * d20 - d01 * d21) * denom;
		w = (d00 * d21 - d01 * d20) * denom;
		u = 1.0f - v - w;
	}
}

public class MegaVoxel
{
	public class Voxel_t
	{
		public Vector3 position;
		public List<MegaTriangle> tris;

		public Voxel_t()
		{
			position = Vector3.zero;
			tris = new List<MegaTriangle>();
		}
	}

	public static void GetGridIndex(Vector3 p, out int x, out int y, out int z, float unit)
	{
		x = (int)((p.x - start.x) / unit);
		y = (int)((p.y - start.y) / unit);
		z = (int)((p.z - start.z) / unit);
	}

	public static Voxel_t[, ,] volume;
	public static int width;
	public static int height;
	public static int depth;
	static Vector3 start;

	public static void Voxelize(Vector3[] vertices, int[] indices, Bounds bounds, int resolution, out float unit)
	{
		float maxLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
		unit = maxLength / resolution;
		float hunit = unit * 0.5f;

		start = bounds.min - new Vector3(hunit, hunit, hunit);
		Vector3 end = bounds.max + new Vector3(hunit, hunit, hunit);
		Vector3 size = end - start;

		width = Mathf.CeilToInt(size.x / unit);
		height = Mathf.CeilToInt(size.y / unit);
		depth = Mathf.CeilToInt(size.z / unit);

		volume = new Voxel_t[width, height, depth];
		Bounds[, ,] boxes = new Bounds[width, height, depth];
		Vector3 voxelSize = Vector3.one * unit;

		for ( int x = 0; x < width; x++ )
		{
			for ( int y = 0; y < height; y++ )
			{
				for ( int z = 0; z < depth; z++ )
				{
					Vector3 p = new Vector3(x, y, z) * unit + start;
					Bounds aabb = new Bounds(p, voxelSize);
					boxes[x, y, z] = aabb;
					volume[x, y, z] = new Voxel_t();
				}
			}
		}

		Vector3 direction = Vector3.forward;

		for ( int i = 0, n = indices.Length; i < n; i += 3 )
		{
			MegaTriangle tri = new MegaTriangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]], direction, i);

			Vector3 min = tri.bounds.min - start;
			Vector3 max = tri.bounds.max - start;
			int iminX = (int)(min.x / unit), iminY = (int)(min.y / unit), iminZ = (int)(min.z / unit);
			int imaxX = (int)(max.x / unit), imaxY = (int)(max.y / unit), imaxZ = (int)(max.z / unit);

			iminX = Mathf.Clamp(iminX, 0, width - 1);
			iminY = Mathf.Clamp(iminY, 0, height - 1);
			iminZ = Mathf.Clamp(iminZ, 0, depth - 1);
			imaxX = Mathf.Clamp(imaxX, 0, width - 1);
			imaxY = Mathf.Clamp(imaxY, 0, height - 1);
			imaxZ = Mathf.Clamp(imaxZ, 0, depth - 1);

			for ( int x = iminX; x <= imaxX; x++ )
			{
				for ( int y = iminY; y <= imaxY; y++ )
				{
					for ( int z = iminZ; z <= imaxZ; z++ )
					{
						if ( Intersects(tri, boxes[x, y, z]) )
						{
							Voxel_t voxel = volume[x, y, z];
							voxel.position = boxes[x, y, z].center;
							voxel.tris.Add(tri);
							volume[x, y, z] = voxel;
						}
					}
				}
			}
		}
	}

	public static bool Intersects(MegaTriangle tri, Bounds aabb)
	{
		float p0, p1, p2, r;

		Vector3 center = aabb.center, extents = aabb.max - center;

		Vector3 v0 = tri.a - center,
			v1 = tri.b - center,
			v2 = tri.c - center;

		Vector3 f0 = v1 - v0,
			f1 = v2 - v1,
			f2 = v0 - v2;

		Vector3 a00 = new Vector3(0, -f0.z, f0.y),
			a01 = new Vector3(0, -f1.z, f1.y),
			a02 = new Vector3(0, -f2.z, f2.y),
			a10 = new Vector3(f0.z, 0, -f0.x),
			a11 = new Vector3(f1.z, 0, -f1.x),
			a12 = new Vector3(f2.z, 0, -f2.x),
			a20 = new Vector3(-f0.y, f0.x, 0),
			a21 = new Vector3(-f1.y, f1.x, 0),
			a22 = new Vector3(-f2.y, f2.x, 0);

		// Test axis a00
		p0 = Vector3.Dot(v0, a00);
		p1 = Vector3.Dot(v1, a00);
		p2 = Vector3.Dot(v2, a00);
		r = extents.y * Mathf.Abs(f0.z) + extents.z * Mathf.Abs(f0.y);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a01
		p0 = Vector3.Dot(v0, a01);
		p1 = Vector3.Dot(v1, a01);
		p2 = Vector3.Dot(v2, a01);
		r = extents.y * Mathf.Abs(f1.z) + extents.z * Mathf.Abs(f1.y);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a02
		p0 = Vector3.Dot(v0, a02);
		p1 = Vector3.Dot(v1, a02);
		p2 = Vector3.Dot(v2, a02);
		r = extents.y * Mathf.Abs(f2.z) + extents.z * Mathf.Abs(f2.y);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a10
		p0 = Vector3.Dot(v0, a10);
		p1 = Vector3.Dot(v1, a10);
		p2 = Vector3.Dot(v2, a10);
		r = extents.x * Mathf.Abs(f0.z) + extents.z * Mathf.Abs(f0.x);
		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a11
		p0 = Vector3.Dot(v0, a11);
		p1 = Vector3.Dot(v1, a11);
		p2 = Vector3.Dot(v2, a11);
		r = extents.x * Mathf.Abs(f1.z) + extents.z * Mathf.Abs(f1.x);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a12
		p0 = Vector3.Dot(v0, a12);
		p1 = Vector3.Dot(v1, a12);
		p2 = Vector3.Dot(v2, a12);
		r = extents.x * Mathf.Abs(f2.z) + extents.z * Mathf.Abs(f2.x);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a20
		p0 = Vector3.Dot(v0, a20);
		p1 = Vector3.Dot(v1, a20);
		p2 = Vector3.Dot(v2, a20);
		r = extents.x * Mathf.Abs(f0.y) + extents.y * Mathf.Abs(f0.x);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a21
		p0 = Vector3.Dot(v0, a21);
		p1 = Vector3.Dot(v1, a21);
		p2 = Vector3.Dot(v2, a21);
		r = extents.x * Mathf.Abs(f1.y) + extents.y * Mathf.Abs(f1.x);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		// Test axis a22
		p0 = Vector3.Dot(v0, a22);
		p1 = Vector3.Dot(v1, a22);
		p2 = Vector3.Dot(v2, a22);
		r = extents.x * Mathf.Abs(f2.y) + extents.y * Mathf.Abs(f2.x);

		if ( Mathf.Max(-Mathf.Max(p0, p1, p2), Mathf.Min(p0, p1, p2)) > r )
			return false;

		if ( Mathf.Max(v0.x, v1.x, v2.x) < -extents.x || Mathf.Min(v0.x, v1.x, v2.x) > extents.x )
			return false;

		if ( Mathf.Max(v0.y, v1.y, v2.y) < -extents.y || Mathf.Min(v0.y, v1.y, v2.y) > extents.y )
			return false;

		if ( Mathf.Max(v0.z, v1.z, v2.z) < -extents.z || Mathf.Min(v0.z, v1.z, v2.z) > extents.z )
			return false;

		Vector3 normal = Vector3.Cross(f1, f0).normalized;
		Plane pl = new Plane(normal, Vector3.Dot(normal, tri.a));
		return Intersects(pl, aabb);
	}

	public static bool Intersects(Plane pl, Bounds aabb)
	{
		Vector3 center = aabb.center;
		Vector3 extents = aabb.max - center;

		float r = extents.x * Mathf.Abs(pl.normal.x) + extents.y * Mathf.Abs(pl.normal.y) + extents.z * Mathf.Abs(pl.normal.z);
		float s = Vector3.Dot(pl.normal, center) - pl.distance;

		return Mathf.Abs(s) <= r;
	}
}

[CanEditMultipleObjects, CustomEditor(typeof(MegaWrap))]
public class MegaWrapEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MegaWrap mod = (MegaWrap)target;

#if !UNITY_5 && !UNITY_2017 && !UNITY_2018
		EditorGUIUtility.LookLikeControls();
#endif
		mod.WrapEnabled = EditorGUILayout.Toggle("Enabled", mod.WrapEnabled);
		mod.target = (MegaModifyObject)EditorGUILayout.ObjectField("Target", mod.target, typeof(MegaModifyObject), true);

		float max = 1.0f;
		if ( mod.target )
			max = mod.target.bbox.size.magnitude;

		mod.maxdist = EditorGUILayout.Slider("Max Dist", mod.maxdist, 0.0f, max);	//2.0f);	//mod.maxdist);
		if ( mod.maxdist < 0.0f )
			mod.maxdist = 0.0f;

		mod.maxpoints = EditorGUILayout.IntField("Max Points", mod.maxpoints);	//mod.maxdist);
		if ( mod.maxpoints < 1 )
			mod.maxpoints = 1;

		Color col = GUI.backgroundColor;
		EditorGUILayout.BeginHorizontal();
		if ( mod.bindverts == null )
		{
			GUI.backgroundColor = Color.red;
			if ( GUILayout.Button("Map") )
				Attach(mod.target);
		}
		else
		{
			GUI.backgroundColor = Color.green;
			if ( GUILayout.Button("ReMap") )
				Attach(mod.target);
		}

		GUI.backgroundColor = col;
		if ( GUILayout.Button("Reset") )
			mod.ResetMesh();

		EditorGUILayout.EndHorizontal();

		if ( GUI.changed )
			EditorUtility.SetDirty(mod);

		mod.gap = EditorGUILayout.FloatField("Gap", mod.gap);
		mod.shrink = EditorGUILayout.Slider("Shrink", mod.shrink, 0.0f, 1.0f);
		mod.size = EditorGUILayout.Slider("Size", mod.size, 0.001f, 0.04f);
		if ( mod.bindverts != null )
			mod.vertindex = EditorGUILayout.IntSlider("Vert Index", mod.vertindex, 0, mod.bindverts.Length - 1);
		mod.offset = EditorGUILayout.Vector3Field("Offset", mod.offset);

		mod.NormalMethod = (MegaNormalMethod)EditorGUILayout.EnumPopup("Normal Method", mod.NormalMethod);
#if UNITY_5 || UNITY_2017 || UNITY_2018
		mod.UseBakedMesh = EditorGUILayout.Toggle("Use Baked Mesh", mod.UseBakedMesh);
#endif

		if ( mod.bindverts == null || mod.target == null )
			EditorGUILayout.LabelField("Object not wrapped");
		else
			EditorGUILayout.LabelField("UnMapped", mod.nomapcount.ToString());

		if ( GUI.changed )
			EditorUtility.SetDirty(mod);
	}

	public void OnSceneGUI()
	{
		DisplayDebug();
	}

	void DisplayDebug()
	{
		MegaWrap mod = (MegaWrap)target;
		if ( mod.target )
		{
			if ( mod.bindverts != null && mod.bindverts.Length > 0 )
			{
				if ( mod.targetIsSkin && !mod.sourceIsSkin )
				{
					Color col = Color.black;
					Handles.matrix = Matrix4x4.identity;

					MegaBindVert bv = mod.bindverts[mod.vertindex];

					for ( int i = 0; i < bv.verts.Count; i++ )
					{
						MegaBindInf bi = bv.verts[i];
						float w = bv.verts[i].weight / bv.weight;

						if ( w > 0.5f )
							col = Color.Lerp(Color.green, Color.red, (w - 0.5f) * 2.0f);
						else
							col = Color.Lerp(Color.blue, Color.green, w * 2.0f);
						Handles.color = col;

						Vector3 p = (mod.skinnedVerts[bv.verts[i].i0] + mod.skinnedVerts[bv.verts[i].i1] + mod.skinnedVerts[bv.verts[i].i2]) / 3.0f;	//tm.MultiplyPoint(mod.vr[i].cpos);
						MegaHandles.DotCap(i, p, Quaternion.identity, mod.size);	//0.01f);

						Vector3 p0 = mod.skinnedVerts[bi.i0];
						Vector3 p1 = mod.skinnedVerts[bi.i1];
						Vector3 p2 = mod.skinnedVerts[bi.i2];

						Vector3 cp = mod.GetCoordMine(p0, p1, p2, bi.bary);
						Handles.color = Color.gray;
						Handles.DrawLine(p, cp);

						Vector3 norm = mod.FaceNormal(p0, p1, p2);
						Vector3 cp1 = cp + (((bi.dist * mod.shrink) + mod.gap) * norm.normalized);
						Handles.color = Color.green;
						Handles.DrawLine(cp, cp1);
					}
				}
				else
				{
					Color col = Color.black;
					Matrix4x4 tm = mod.target.transform.localToWorldMatrix;
					Handles.matrix = tm;	//Matrix4x4.identity;

					MegaBindVert bv = mod.bindverts[mod.vertindex];

					for ( int i = 0; i < bv.verts.Count; i++ )
					{
						MegaBindInf bi = bv.verts[i];
						float w = bv.verts[i].weight / bv.weight;

						if ( w > 0.5f )
							col = Color.Lerp(Color.green, Color.red, (w - 0.5f) * 2.0f);
						else
							col = Color.Lerp(Color.blue, Color.green, w * 2.0f);
						Handles.color = col;

						Vector3 p = (mod.target.sverts[bv.verts[i].i0] + mod.target.sverts[bv.verts[i].i1] + mod.target.sverts[bv.verts[i].i2]) / 3.0f;	//tm.MultiplyPoint(mod.vr[i].cpos);
						MegaHandles.DotCap(i, p, Quaternion.identity, mod.size);	//0.01f);

						Vector3 p0 = mod.target.sverts[bi.i0];
						Vector3 p1 = mod.target.sverts[bi.i1];
						Vector3 p2 = mod.target.sverts[bi.i2];

						Vector3 cp = mod.GetCoordMine(p0, p1, p2, bi.bary);
						Handles.color = Color.gray;
						Handles.DrawLine(p, cp);

						Vector3 norm = mod.FaceNormal(p0, p1, p2);
						Vector3 cp1 = cp + (((bi.dist * mod.shrink) + mod.gap) * norm.normalized);
						Handles.color = Color.green;
						Handles.DrawLine(cp, cp1);
					}
				}

				// Show unmapped verts
				Handles.color = Color.yellow;
				for ( int i = 0; i < mod.bindverts.Length; i++ )
				{
					if ( mod.bindverts[i].weight == 0.0f )
					{
						Vector3 pv1 = mod.freeverts[i];
						MegaHandles.DotCap(0, pv1, Quaternion.identity, mod.size);	//0.01f);
					}
				}
			}

			if ( mod.verts != null && mod.verts.Length > mod.vertindex )
			{
				Handles.color = Color.red;
				Handles.matrix = mod.transform.localToWorldMatrix;
				Vector3 pv = mod.verts[mod.vertindex];
				MegaHandles.DotCap(0, pv, Quaternion.identity, mod.size);	//0.01f);
			}
		}
	}

	void Attach(MegaModifyObject modobj)
	{
		MegaWrap mod = (MegaWrap)target;

		mod.targetIsSkin = false;
		mod.sourceIsSkin = false;

		if ( mod.mesh && mod.startverts != null )
			mod.mesh.vertices = mod.startverts;

		if ( modobj == null )
		{
			mod.bindverts = null;
			return;
		}

		mod.nomapcount = 0;

		if ( mod.mesh )
			mod.mesh.vertices = mod.startverts;

		MeshFilter mf = mod.GetComponent<MeshFilter>();
		Mesh srcmesh = null;

		if ( mf != null )
		{
			//skinned = false;
			srcmesh = mf.sharedMesh;
		}
		else
		{
			SkinnedMeshRenderer smesh = (SkinnedMeshRenderer)mod.GetComponent(typeof(SkinnedMeshRenderer));

			if ( smesh != null )
			{
				//skinned = true;
				srcmesh = smesh.sharedMesh;
				mod.sourceIsSkin = true;
			}
		}

		if ( srcmesh == null )
		{
			Debug.LogWarning("No Mesh found on the target object, make sure target has a mesh and MegaFiers modifier attached!");
			return;
		}

		if ( mod.mesh == null )
			mod.mesh = mod.CloneMesh(srcmesh);	//mf.mesh);

		if ( mf )
			mf.mesh = mod.mesh;
		else
		{
			SkinnedMeshRenderer smesh = (SkinnedMeshRenderer)mod.GetComponent(typeof(SkinnedMeshRenderer));
			smesh.sharedMesh = mod.mesh;
		}

		if ( mod.sourceIsSkin == false )
		{
			SkinnedMeshRenderer tmesh = (SkinnedMeshRenderer)modobj.GetComponent(typeof(SkinnedMeshRenderer));
			if ( tmesh != null )
			{
				mod.targetIsSkin = true;

				if ( !mod.sourceIsSkin )
				{
					Mesh sm = tmesh.sharedMesh;
					mod.bindposes = sm.bindposes;
					mod.boneweights = sm.boneWeights;
					mod.bones = tmesh.bones;
					mod.skinnedVerts = sm.vertices;	//new Vector3[sm.vertexCount];
				}
			}
		}

		if ( mod.targetIsSkin )
		{
			if ( mod.boneweights == null || mod.boneweights.Length == 0 )
				mod.targetIsSkin = false;
		}

		mod.neededVerts.Clear();

		mod.verts = mod.mesh.vertices;
		mod.startverts = mod.mesh.vertices;
		mod.freeverts = new Vector3[mod.startverts.Length];
		Vector3[] baseverts = modobj.verts;	//basemesh.vertices;
		int[] basefaces = modobj.tris;	//basemesh.triangles;

		mod.bindverts = new MegaBindVert[mod.verts.Length];

		// matrix to get vertex into local space of target
		Matrix4x4 tm = mod.transform.localToWorldMatrix * modobj.transform.worldToLocalMatrix;

		List<MegaCloseFace> closefaces = new List<MegaCloseFace>();

		Vector3 p0 = Vector3.zero;
		Vector3 p1 = Vector3.zero;
		Vector3 p2 = Vector3.zero;

		Vector3[] tverts = new Vector3[mod.target.sverts.Length];

		for ( int i = 0; i < tverts.Length; i++ )
		{
			if ( mod.targetIsSkin && !mod.sourceIsSkin )
				tverts[i] = modobj.transform.InverseTransformPoint(mod.GetSkinPos(i));
			else
				tverts[i] = baseverts[i];
		}

		EditorUtility.ClearProgressBar();

		float unit = 0.0f;
		mod.target.mesh.RecalculateBounds();
		MegaVoxel.Voxelize(tverts, basefaces, mod.target.mesh.bounds, 16, out unit);

		//Vector3 min = mod.target.mesh.bounds.min;

		for ( int i = 0; i < mod.verts.Length; i++ )
		{
			MegaBindVert bv = new MegaBindVert();
			mod.bindverts[i] = bv;

			Vector3 p = tm.MultiplyPoint(mod.verts[i]);

			p = mod.transform.TransformPoint(mod.verts[i]);
			p = modobj.transform.InverseTransformPoint(p);
			mod.freeverts[i] = p;

			closefaces.Clear();

			int gx = 0;
			int gy = 0;
			int gz = 0;

			MegaVoxel.GetGridIndex(p, out gx, out gy, out gz, unit);

			for ( int x = gx - 1; x <= gx + 1; x++ )
			{
				if ( x >= 0 && x < MegaVoxel.width )
				{
					for ( int y = gy - 1; y <= gy + 1; y++ )
					{
						if ( y >= 0 && y < MegaVoxel.height )
						{
							for ( int z = gz - 1; z <= gz + 1; z++ )
							{
								if ( z >= 0 && z < MegaVoxel.depth )
								{
									List<MegaTriangle> tris = MegaVoxel.volume[x, y, z].tris;

									for ( int t = 0; t < tris.Count; t++ )
									{
										float dist = mod.GetDistance(p, tris[t].a, tris[t].b, tris[t].c);

										if ( Mathf.Abs(dist) < mod.maxdist )
										{
											MegaCloseFace cf = new MegaCloseFace();
											cf.dist = Mathf.Abs(dist);
											cf.face = tris[t].t;

											bool inserted = false;
											for ( int k = 0; k < closefaces.Count; k++ )
											{
												if ( cf.dist < closefaces[k].dist )
												{
													closefaces.Insert(k, cf);
													inserted = true;
													break;
												}
											}

											if ( !inserted )
												closefaces.Add(cf);
										}
									}
								}
							}
						}
					}
				}
			}

			float tweight = 0.0f;
			int maxp = mod.maxpoints;
			if ( maxp == 0 )
				maxp = closefaces.Count;
			for ( int j = 0; j < maxp; j++ )
			{
				if ( j < closefaces.Count )
				{
					int t = closefaces[j].face;

					p0 = tverts[basefaces[t]];
					p1 = tverts[basefaces[t + 1]];
					p2 = tverts[basefaces[t + 2]];

					Vector3 normal = mod.FaceNormal(p0, p1, p2);

					float dist = closefaces[j].dist;	//GetDistance(p, p0, p1, p2);

					MegaBindInf bi = new MegaBindInf();
					bi.dist = mod.GetPlaneDistance(p, p0, p1, p2);	//dist;
					bi.face = t;
					bi.i0 = basefaces[t];
					bi.i1 = basefaces[t + 1];
					bi.i2 = basefaces[t + 2];
					bi.bary = mod.CalcBary(p, p0, p1, p2);
					bi.weight = 1.0f / (1.0f + dist);
					bi.area = normal.magnitude * 0.5f;	//CalcArea(baseverts[basefaces[t]], baseverts[basefaces[t + 1]], baseverts[basefaces[t + 2]]);	// Could calc once at start
					tweight += bi.weight;
					bv.verts.Add(bi);
				}
			}

			if ( mod.maxpoints > 0 && mod.maxpoints < bv.verts.Count )
				bv.verts.RemoveRange(mod.maxpoints, bv.verts.Count - mod.maxpoints);

			// Only want to calculate skin vertices we use
			if ( !mod.sourceIsSkin && mod.targetIsSkin )
			{
				for ( int fi = 0; fi < bv.verts.Count; fi++ )
				{
					if ( !mod.neededVerts.Contains(bv.verts[fi].i0) )
						mod.neededVerts.Add(bv.verts[fi].i0);

					if ( !mod.neededVerts.Contains(bv.verts[fi].i1) )
						mod.neededVerts.Add(bv.verts[fi].i1);

					if ( !mod.neededVerts.Contains(bv.verts[fi].i2) )
						mod.neededVerts.Add(bv.verts[fi].i2);
				}
			}

			if ( tweight == 0.0f )
			{
				mod.nomapcount++;
				break;
			}

			bv.weight = tweight;
		}
	}
}