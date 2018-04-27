
using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// TODO: Have a move list in the inspector instead of order value

[AddComponentMenu("Modifiers/Modify Object")]
[ExecuteInEditMode]
public class MegaModifyObject : MegaModifiers
{
	[HideInInspector]
	public Mesh cachedMesh;
	public bool InvisibleUpdate	= false;
	bool		visible			= true;
	int			restorekeep		= 0;

	private static int CompareOrder(MegaModifier m1, MegaModifier m2)
	{
		return m1.Order - m2.Order;
	}

	[ContextMenu("Resort")]
	public virtual void Resort()
	{
		BuildList();
	}

	[ContextMenu("Help")]
	public virtual void Help()
	{
		Application.OpenURL("http://www.west-racing.com/mf/?page_id=444");
	}

	[ContextMenu("Remove Modify Object (Keep deformed mesh)")]
	public virtual void RemoveKeep()
	{
		MegaModifier[] mods = GetComponents<MegaModifier>();

		for ( int i = 0; i < mods.Length; i++ )
		{
			if ( Application.isEditor )
				DestroyImmediate(mods[i]);
			else
				Destroy(mods[i]);
		}

		restorekeep = 1;
		if ( Application.isEditor )
			DestroyImmediate(this);
		else
			Destroy(this);
	}

	[ContextMenu("Remove Modify Object (Restore Mesh)")]
	public virtual void RemoveRestore()
	{
		MegaModifier[] mods = GetComponents<MegaModifier>();

		for ( int i = 0; i < mods.Length; i++ )
		{
			if ( Application.isEditor )
				DestroyImmediate(mods[i]);
			else
				Destroy(mods[i]);
		}

		restorekeep = 2;
		if ( Application.isEditor )
			DestroyImmediate(this);
		else
			Destroy(this);
	}

	void OnDestroy()
	{
		if ( mesh != cachedMesh )
		{
			if ( restorekeep == 0 || restorekeep == 2 )
			{
				if ( !SetMeshNew(gameObject, cachedMesh) )
				{
					if ( Application.isEditor )
						DestroyImmediate(mesh);
					else
						Destroy(mesh);
				}
			}
		}
	}

	void Start()
	{
		if ( dynamicMesh )
			cachedMesh = null;
	}

	void OnRenderObject()
	{
		if ( UpdateMode == MegaUpdateMode.OnRender )
		{
			ModifyObjectMT();
		}
	}

	public void GetMesh(bool force)
	{
		if ( mesh == null || cachedMesh == null || sverts.Length == 0 || mesh.vertexCount != sverts.Length || force )
		{
			if ( dynamicMesh )
			{
				cachedMesh = FindMesh(gameObject, out sourceObj);
				if ( verts != null && verts.Length > 0 )
					cachedMesh.vertices = verts;
				mesh = cachedMesh;
				if ( mesh.vertexCount != 0 )
					SetMeshData();
			}
			else
			{
				cachedMesh = FindMesh(gameObject, out sourceObj);
				if ( verts != null && verts.Length > 0 )
					cachedMesh.vertices = verts;
				mesh = MegaCopyObject.DupMesh(cachedMesh, "");

				SetMesh(gameObject, mesh);
				if ( mesh.vertexCount != 0 )
					SetMeshData();
			}
		}
	}

	void SetMeshData()
	{
		bbox = cachedMesh.bounds;
		sverts = new Vector3[cachedMesh.vertexCount];
		verts = cachedMesh.vertices;

		uvs = cachedMesh.uv;
		suvs = new Vector2[cachedMesh.uv.Length];
		cols = cachedMesh.colors;

		//BuildNormalMapping(cachedMesh, false);
		mods = GetComponents<MegaModifier>();

		Array.Sort(mods, CompareOrder);

		for ( int i = 0; i < mods.Length; i++ )
		{
			if ( mods[i] != null )
			{
				mods[i].SetModMesh(mesh);
				mods[i].ModStart(this);	// Some mods like push error if we dont do this, put in error check and disable 
			}
		}
		mapping = null;
		UpdateMesh = -1;
	}

	public void ModReset(MegaModifier m)
	{
		if ( m != null )
		{
			m.SetModMesh(cachedMesh);
			BuildList();
		}
	}

	// Check, do we need these?
	void Update()
	{
		GetMesh(false);

		if ( visible || InvisibleUpdate )
		{
			if ( UpdateMode == MegaUpdateMode.Update )
				ModifyObjectMT();
		}
	}

	void LateUpdate()
	{
		if ( visible || InvisibleUpdate )
		{
			if ( UpdateMode == MegaUpdateMode.LateUpdate )
				ModifyObjectMT();
		}
	}

	void OnBecameVisible()
	{
		visible = true;
	}

	void OnBecameInvisible()
	{
		visible = false;
	}

	[ContextMenu("Reset")]
	public void Reset()
	{
		ResetMeshInfo();
	}

	// Mesh related stuff
	[ContextMenu("Reset Mesh Info")]
	public void ResetMeshInfo()
	{
		if ( mods != null )
		{
			if ( mods.Length > 0 )
			{
				mesh.vertices = mods[0].verts;	//_verts;	// mesh.vertices = GetVerts(true);
			}
			mesh.uv = uvs;	//GetUVs(true);

			if ( recalcnorms )
				RecalcNormals();

			if ( recalcbounds )
				mesh.RecalculateBounds();
		}
#if false
		if ( cachedMesh == null )
			cachedMesh = (Mesh)Mesh.Instantiate(FindMesh(gameObject, out sourceObj));

		GetMeshData(false);

		mesh.vertices = verts;	//_verts;	// mesh.vertices = GetVerts(true);
		mesh.uv = uvs;	//GetUVs(true);

		if ( recalcnorms )
			RecalcNormals();

		if ( recalcbounds )
			mesh.RecalculateBounds();
#endif
	}

#if false
	void Reset()
	{
		if ( cachedMesh == null )
			cachedMesh = (Mesh)Mesh.Instantiate(FindMesh(gameObject, out sourceObj));

		BuildList();
		ReStart1(true);
	}
#endif

	// Called by my scripts when the mesh has changed
	public void MeshUpdated()
	{
		GetMesh(true);
		//cachedMesh = (Mesh)Mesh.Instantiate(FindMesh(gameObject, out sourceObj));

		//GetMeshData(true);

		foreach ( MegaModifier mod in mods )	// Added back in?
			mod.SetModMesh(cachedMesh);
	}

	// Replace mesh data with data from newmesh, called from scripts not used internally
	public void MeshChanged(Mesh newmesh)
	{
		if ( mesh )
		{
			mesh.vertices = newmesh.vertices;
			mesh.normals = newmesh.normals;
			mesh.uv = newmesh.uv;
#if UNITY_5_0 || UNITY_5_1 || UNITY_5 || UNITY_2017 || UNITY_2018
			mesh.uv2 = newmesh.uv2;
			mesh.uv3 = newmesh.uv3;
			mesh.uv4 = newmesh.uv4;
#else
			mesh.uv1 = newmesh.uv1;
			mesh.uv2 = newmesh.uv2;
#endif
			mesh.colors = newmesh.colors;
			mesh.tangents = newmesh.tangents;

			mesh.subMeshCount = newmesh.subMeshCount;
			for ( int i = 0; i < newmesh.subMeshCount; i++ )
				mesh.SetTriangles(newmesh.GetTriangles(i), i);

			bbox = newmesh.bounds;
			sverts = new Vector3[mesh.vertexCount];
			verts = mesh.vertices;

			uvs = mesh.uv;
			suvs = new Vector2[mesh.uv.Length];
			cols = mesh.colors;

			//BuildNormalMapping(cachedMesh, false);
			mods = GetComponents<MegaModifier>();

			Array.Sort(mods, CompareOrder);

			foreach ( MegaModifier mod in mods )
			{
				if ( mod != null )
				{
					mod.SetModMesh(newmesh);
					mod.ModStart(this);	// Some mods like push error if we dont do this, put in error check and disable 
				}
			}

			mapping = null;
			UpdateMesh = -1;
		}
	}

#if false
	public void GetMeshData(bool force)
	{
		if ( force || mesh == null )
			mesh = FindMesh1(gameObject, out sourceObj);	//Utils.GetMesh(gameObject);

		// Do we use mesh anymore
		if ( mesh != null )	// was mesh
		{
			bbox = cachedMesh.bounds;
			sverts = new Vector3[cachedMesh.vertexCount];
			verts = cachedMesh.vertices;

			uvs = cachedMesh.uv;
			suvs = new Vector2[cachedMesh.uv.Length];
			cols = cachedMesh.colors;

			//BuildNormalMapping(cachedMesh, false);
			mods = GetComponents<MegaModifier>();

			Array.Sort(mods, CompareOrder);

			for ( int i = 0; i < mods.Length; i++ )
			{
				if ( mods[i] != null )
					mods[i].ModStart(this);	// Some mods like push error if we dont do this, put in error check and disable 
			}
		}

		UpdateMesh = -1;
	}
#endif

	public void SetMesh(GameObject go, Mesh mesh)
	{
		if ( go )
		{
			Transform[] trans = (Transform[])go.GetComponentsInChildren<Transform>(true);

			for ( int i = 0; i < trans.Length; i++ )
			{
				MeshFilter mf = (MeshFilter)trans[i].GetComponent<MeshFilter>();

				if ( mf )
				{
					mf.sharedMesh = mesh;
					return;
				}

				SkinnedMeshRenderer skin = (SkinnedMeshRenderer)trans[i].GetComponent<SkinnedMeshRenderer>();
				if ( skin )
				{
					skin.sharedMesh = mesh;
					return;
				}
			}
		}
	}

	static public Mesh FindMesh(GameObject go, out GameObject obj)
	{
		if ( go )
		{
			Transform[] trans = (Transform[])go.GetComponentsInChildren<Transform>(true);

			for ( int i = 0; i < trans.Length; i++ )
			{
				MeshFilter mf = (MeshFilter)trans[i].GetComponent<MeshFilter>();

				if ( mf )
				{
					if ( mf.gameObject != go )
						obj = mf.gameObject;
					else
						obj = null;

					return mf.sharedMesh;
				}

				SkinnedMeshRenderer skin = (SkinnedMeshRenderer)trans[i].GetComponent<SkinnedMeshRenderer>();
				if ( skin )
				{
					if ( skin.gameObject != go )
						obj = skin.gameObject;
					else
						obj = null;

					return skin.sharedMesh;
				}
			}
		}

		obj = null;
		return null;
	}

	static public Mesh FindMesh(GameObject go)
	{
		if ( go )
		{
			Transform[] trans = (Transform[])go.GetComponentsInChildren<Transform>(true);

			for ( int i = 0; i < trans.Length; i++ )
			{
				MeshFilter mf = (MeshFilter)trans[i].GetComponent<MeshFilter>();

				if ( mf )
				{
					return mf.sharedMesh;
				}

				SkinnedMeshRenderer skin = (SkinnedMeshRenderer)trans[i].GetComponent<SkinnedMeshRenderer>();
				if ( skin )
				{
					return skin.sharedMesh;
				}
			}
		}

		return null;
	}

	static public bool SetMeshNew(GameObject go, Mesh m)
	{
		if ( go )
		{
			Transform[] trans = (Transform[])go.GetComponentsInChildren<Transform>(true);

			for ( int i = 0; i < trans.Length; i++ )
			{
				MeshFilter mf = (MeshFilter)trans[i].GetComponent<MeshFilter>();

				if ( mf )
				{
					mf.sharedMesh = m;
					return true;
				}

				SkinnedMeshRenderer skin = (SkinnedMeshRenderer)trans[i].GetComponent<SkinnedMeshRenderer>();
				if ( skin )
				{
					skin.sharedMesh = m;
					return true;
				}
			}
		}

		return false;
	}


#if false
	public Mesh FindMesh1(GameObject go, out GameObject obj)
	{
		if ( go )
		{
			MeshFilter[] filters = (MeshFilter[])go.GetComponentsInChildren<MeshFilter>(true);

			if ( filters.Length > 0 )
			{
				if ( filters[0].gameObject != go )
					obj = filters[0].gameObject;
				else
					obj = null;
				return filters[0].mesh;
			}

			SkinnedMeshRenderer[] skins = (SkinnedMeshRenderer[])go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			if ( skins.Length > 0 )
			{
				if ( skins[0].gameObject != go )
					obj = skins[0].gameObject;
				else
					obj = null;
				return skins[0].sharedMesh;
			}
		}

		obj = null;
		return null;
	}
#endif

#if false
	void RestoreMesh(GameObject go, Mesh mesh)
	{
		if ( go )
		{
			MeshFilter[] filters = (MeshFilter[])go.GetComponentsInChildren<MeshFilter>(true);

			if ( filters.Length > 0 )
			{
				filters[0].sharedMesh = (Mesh)Instantiate(mesh);
				return;
			}
			SkinnedMeshRenderer[] skins = (SkinnedMeshRenderer[])go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			if ( skins.Length > 0 )
			{
				skins[0].sharedMesh = (Mesh)Instantiate(mesh);
				return;
			}
		}
	}
#endif
}
