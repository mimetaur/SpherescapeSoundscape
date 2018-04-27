
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

// 01313123931 bt handler

public class MegaOBJExport : MonoBehaviour
{
	public bool	sequence = false;
	public int framerate = 30;
	public KeyCode key = KeyCode.O;
	public string	path = "";

	public static string MeshToString(MeshFilter mf)
	{
#if UNITY_EDITOR
		Mesh m = mf.sharedMesh;
		MeshRenderer mr = mf.GetComponent<MeshRenderer>();
		Material[] mats = mr.sharedMaterials;

		StringBuilder sb = new StringBuilder();

		sb.Append("g ").Append(mf.name).Append("\n");
		foreach ( Vector3 v in m.vertices )
		{
			sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
		}
		sb.Append("\n");
		foreach ( Vector3 v in m.normals )
		{
			sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
		}
		sb.Append("\n");
		foreach ( Vector3 v in m.uv )
		{
			sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
		}
		for ( int material = 0; material < m.subMeshCount; material++ )
		{
			sb.Append("\n");
			sb.Append("usemtl ").Append(mats[material].name).Append("\n");
			sb.Append("usemap ").Append(mats[material].name).Append("\n");

			int[] triangles = m.GetTriangles(material);
			for ( int i = 0; i < triangles.Length; i += 3 )
			{
				sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
					triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
			}
		}
		return sb.ToString();
#else
		return "";
#endif
	}

	public static void MeshToFile(MeshFilter mf, string filename)
	{
#if UNITY_EDITOR
		using ( StreamWriter sw = new StreamWriter(filename) )
		{
			sw.Write(MeshToString(mf));
		}
#endif
	}

	int frame = 0;

#if UNITY_EDITOR
	void Update()
	{
		if ( sequence )
		{
			if ( Input.GetKey(key) )
			{
				Time.captureFramerate = framerate;
				MeshFilter mf = (MeshFilter)GetComponent<MeshFilter>();
				MeshToFile(mf, path + "/" + gameObject.name + "-" + frame + ".obj");
				frame++;
			}
			else
				Time.captureFramerate = 0;
		}
		else
		{
			if ( Input.GetKeyDown(key) )
			{
				MeshFilter mf = (MeshFilter)GetComponent<MeshFilter>();
				MeshToFile(mf, path + "/" + gameObject.name + ".obj");
			}
		}
	}
#endif
}