
using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public enum MegaCacheAxis
{
	X,
	Y,
	Z,
}

public class MegaCacheUtils
{
	static public Bounds GetBounds(Vector3[] vals)
	{
		Bounds b = new Bounds(Vector3.zero, Vector3.zero);

		if ( vals != null && vals.Length > 0 )
		{
			b.Encapsulate(vals[0]);

			for ( int i = 1; i < vals.Length; i++ )
				b.Encapsulate(vals[i]);
		}

		return b;
	}

	static public Bounds GetBounds(Vector2[] vals)
	{
		Bounds b = new Bounds(Vector3.zero, Vector3.zero);

		if ( vals != null && vals.Length > 0 )
		{
			Vector2 p = Vector2.zero;

			p = vals[0];
			b.Encapsulate(p);

			for ( int i = 1; i < vals.Length; i++ )
			{
				p = vals[i];
				b.Encapsulate(p);
			}
		}

		return b;
	}

	static public Bounds GetBounds(List<Vector3> vals)
	{
		Bounds b = new Bounds(Vector3.zero, Vector3.zero);

		if ( vals != null && vals.Count > 0 )
		{
			b.Encapsulate(vals[0]);

			for ( int i = 1; i < vals.Count; i++ )
				b.Encapsulate(vals[i]);
		}

		return b;
	}

	static public Bounds GetBounds(List<float> vals)
	{
		Bounds b = new Bounds(Vector3.zero, Vector3.zero);

		if ( vals != null && vals.Count > 0 )
		{
			Vector3 p = Vector3.zero;

			p.x = vals[0];
			b.Encapsulate(p);

			for ( int i = 1; i < vals.Count; i++ )
			{
				p.x = vals[i];
				b.Encapsulate(p);
			}
		}

		return b;
	}

	static public string MakeFileName(string file, ref int format)
	{
		//Debug.Log("filein " + file);
		string ret = "";
		format = 0;
		for ( int i = file.Length - 1; i >= 0; i-- )
		{
			char c = file[i];
			if ( Char.IsNumber(c) )
			{
				format++;
			}
			else
			{
				ret = file.Substring(0, i + 1);
				Debug.Log("ret " + ret + " format " + format);
				break;
			}
		}

		return ret;
	}
}