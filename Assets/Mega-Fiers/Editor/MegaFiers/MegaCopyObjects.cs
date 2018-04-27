
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;

#if !UNITY_FLASH && !UNITY_METRO && !UNITY_WP8
public class MegaCopyObjects : MonoBehaviour
{
	[MenuItem("GameObject/Mega Instance Object")]
	static void InstanceModifiedMesh()
	{
		GameObject from = Selection.activeGameObject;
		MegaCopyObject.InstanceObject(from);
	}

	//[MenuItem("GameObject/Mega Copy Object")]
	static void DoCopyObjects()
	{
		GameObject from = Selection.activeGameObject;
		MegaCopyObject.DoCopyObjects(from);
	}

	//[MenuItem("GameObject/Mega Copy Hier")]
	static void DoCopyObjectsHier()
	{
		GameObject from = Selection.activeGameObject;
		MegaCopyObject.DoCopyObjectsChildren(from);
	}

	static Mesh CloneMesh(Mesh mesh)
	{
		Mesh clonemesh = new Mesh();
		clonemesh.vertices = mesh.vertices;
#if UNITY_5_0 || UNITY_5_1 || UNITY_5 || UNITY_2017 || UNITY_2018
		clonemesh.uv2 = mesh.uv2;
		clonemesh.uv3 = mesh.uv3;
		clonemesh.uv4 = mesh.uv4;
#else
		clonemesh.uv1 = mesh.uv1;
		clonemesh.uv2 = mesh.uv2;
#endif
		clonemesh.uv = mesh.uv;
		clonemesh.normals = mesh.normals;
		clonemesh.tangents = mesh.tangents;
		clonemesh.colors = mesh.colors;

		clonemesh.subMeshCount = mesh.subMeshCount;

		for ( int s = 0; s < mesh.subMeshCount; s++ )
			clonemesh.SetTriangles(mesh.GetTriangles(s), s);

		clonemesh.boneWeights = mesh.boneWeights;
		clonemesh.bindposes = mesh.bindposes;
		clonemesh.name = mesh.name + "_copy";
		clonemesh.RecalculateBounds();

		return clonemesh;
	}

	[MenuItem("GameObject/Old Create Mega Prefab (Deprec)")]
	static void AllNewCreateSimplePrefab()
	{
		if ( Selection.activeGameObject != null )
		{
			if ( !Directory.Exists("Assets/MegaPrefabs") )
				AssetDatabase.CreateFolder("Assets", "MegaPrefabs");

			GameObject obj = Selection.activeGameObject;

			// Make a copy?
			GameObject newobj = MegaCopyObject.DoCopyObjects(obj);
			newobj.name = obj.name;

			// Get all modifyObjects in children
			MegaModifyObject[] mods = newobj.GetComponentsInChildren<MegaModifyObject>();

			for ( int i = 0; i < mods.Length; i++ )
			{
				// Need method to get the base mesh
				GameObject pobj = mods[i].gameObject;

				// Get the mesh and make an asset for it
				Mesh mesh = MegaUtils.GetSharedMesh(pobj);

				if ( mesh )
				{
					string mname = mesh.name;
					int ix = mname.IndexOf("Instance");
					if ( ix != -1 )
						mname = mname.Remove(ix);

					string meshpath = "Assets/MegaPrefabs/" + mname + ".prefab"; 
					AssetDatabase.CreateAsset(mesh, meshpath);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}

			MegaWrap[] wraps = newobj.GetComponentsInChildren<MegaWrap>();

			for ( int i = 0; i < wraps.Length; i++ )
			{
				// Need method to get the base mesh
				GameObject pobj = wraps[i].gameObject;

				// Get the mesh and make an asset for it
				Mesh mesh = MegaUtils.GetSharedMesh(pobj);

				if ( mesh )
				{
					string mname = mesh.name;

					int ix = mname.IndexOf("Instance");
					if ( ix != -1 )
						mname = mname.Remove(ix);

					string meshpath = "Assets/MegaPrefabs/" + mname + ".prefab";
					AssetDatabase.CreateAsset(mesh, meshpath);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}

			Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/MegaPrefabs/" + newobj.name + "_Prefab.prefab");
			//EditorUtility.ReplacePrefab(newobj, prefab, ReplacePrefabOptions.ConnectToPrefab);
			PrefabUtility.ReplacePrefab(newobj, prefab, ReplacePrefabOptions.ConnectToPrefab);
			DestroyImmediate(newobj);
		}
	}

	[MenuItem("GameObject/Mega Duplicate Object (old)")]
	static void DupObject()
	{
		GameObject from = Selection.activeGameObject;
		MegaCopyObject.DuplicateObject(from);
	}

	[MenuItem("GameObject/Mega Duplicate Object New")]
	static void DupObjectNew()
	{
		SceneView.lastActiveSceneView.Focus();
		if ( Selection.activeGameObject )
		{
			MegaModifyObject[] oldmods = (MegaModifyObject[])Selection.activeGameObject.GetComponentsInChildren<MegaModifyObject>();
			bool[] enabled = new bool[oldmods.Length];
			for ( int i = 0; i < oldmods.Length; i++ )
			{
				enabled[i] = oldmods[i].enabled;
				oldmods[i].enabled = false;
				oldmods[i].ResetMeshInfo();
			}

			EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Duplicate"));

			GameObject go = Selection.activeGameObject;

			MegaModifyObject[] mods = (MegaModifyObject[])go.GetComponentsInChildren<MegaModifyObject>();

			for ( int i = 0; i < mods.Length; i++ )
			{
				mods[i].enabled = enabled[i];
				mods[i].MeshUpdated();
				oldmods[i].enabled = enabled[i];
			}
		}
	}

	[MenuItem("GameObject/Create Mega Prefab")]
	static void CreatePrefab()
	{
		if ( Selection.activeGameObject != null )
		{
			if ( !Directory.Exists("Assets/MegaPrefabs") )
				AssetDatabase.CreateFolder("Assets", "MegaPrefabs");

			GameObject obj = Selection.activeGameObject;

			// Make a copy?
			GameObject newobj = MegaCopyObject.DuplicateObjectForPrefab(obj);
			newobj.name = obj.name;

			// Get all modifyObjects in children
			MegaModifyObject[] mods = newobj.GetComponentsInChildren<MegaModifyObject>();

			int id = 0;

			for ( int i = 0; i < mods.Length; i++ )
			{
				// Need method to get the base mesh
				GameObject pobj = mods[i].gameObject;

				// Get the mesh and make an asset for it
				//Mesh mesh = MegaUtils.GetSharedMesh(pobj);
				GameObject inobj = null;
				Mesh mesh = MegaModifyObject.FindMesh(pobj, out inobj);

				if ( mesh )
				{
					//mods[i].ResetMeshInfo();
					//mesh.vertices = mods[i].verts;	//_verts;	// mesh.vertices = GetVerts(true);
					//mesh.uv = mods[i].uvs;	//GetUVs(true);
					//mods[i].sverts = mods[i].verts;

					//if ( mods[i].recalcnorms )
						//mods[i].RecalcNormals();

					//if ( mods[i].recalcbounds )
						//mesh.RecalculateBounds();

					if ( !AssetDatabase.Contains(mesh) )
					{
						string mname = mesh.name;
						int ix = mname.IndexOf("Instance");
						if ( ix != -1 )
							mname = mname.Remove(ix);

						string meshpath = "Assets/MegaPrefabs/" + mname + ".prefab";
						id++;
						AssetDatabase.CreateAsset(mesh, meshpath);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
				}
			}

			MegaWrap[] wraps = newobj.GetComponentsInChildren<MegaWrap>();

			for ( int i = 0; i < wraps.Length; i++ )
			{
				// Need method to get the base mesh
				GameObject pobj = wraps[i].gameObject;

				// Get the mesh and make an asset for it
				//Mesh mesh = MegaUtils.GetSharedMesh(pobj);
				GameObject inobj = null;
				Mesh mesh = MegaModifyObject.FindMesh(pobj, out inobj);

				if ( mesh )
				{
					if ( !AssetDatabase.Contains(mesh) )
					{
						string mname = mesh.name;

						int ix = mname.IndexOf("Instance");
						if ( ix != -1 )
							mname = mname.Remove(ix);

						string meshpath = "Assets/MegaPrefabs/" + mname + ".prefab";
						id++;
						AssetDatabase.CreateAsset(mesh, meshpath);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
				}
			}

			Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/MegaPrefabs/" + newobj.name + "_Prefab.prefab");
			//EditorUtility.ReplacePrefab(newobj, prefab, ReplacePrefabOptions.ConnectToPrefab);
			PrefabUtility.ReplacePrefab(newobj, prefab, ReplacePrefabOptions.ConnectToPrefab);
			DestroyImmediate(newobj, true);
		}
	}
}
#endif