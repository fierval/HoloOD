using UnityEngine;
using System;

public class VolumetricLaserBeam : MonoBehaviour
{
	public Material m_Mat;
	private GameObject[] m_Pool;
	private MeshFilter[] m_PoolMeshFilter;
	private int m_NumOfBeam = 0;
	private Vector3[] m_Pos = new Vector3[6];
	private Color[] m_Col = new Color[6];
	private readonly int[] k_Tri = { 0,1,2, 2,1,3, 2,3,4, 4,3,5 };
	private readonly Vector2[] k_TexCoord = 
	{
		new Vector2 (0.0f, 0.0f),
		new Vector2 (0.0f, 0.5f),
		new Vector2 (0.5f, 0.0f),
		new Vector2 (0.0f, 1.0f),
		new Vector2 (0.5f, 0.5f),
		new Vector2 (0.5f, 1.0f)
	};
	
	public int Capacity ()   { return m_Pool.Length; }
	public int Size ()       { return m_NumOfBeam; }
	public void PreAlloc (Transform parent, int num)
	{
		m_Pool = new GameObject[num];
		m_PoolMeshFilter = new MeshFilter[num];
		for (int i = 0; i < num; i++)
		{
			m_Pool[i] = new GameObject ("Beam", typeof (MeshFilter), typeof (MeshRenderer));
			m_Pool[i].transform.parent = parent;
			MeshRenderer mr = m_Pool[i].GetComponent<MeshRenderer> ();
			mr.material = m_Mat;
			m_PoolMeshFilter[i] = m_Pool[i].GetComponent<MeshFilter> ();
		}
		Reset ();
	}
	public void Begin ()
	{
		int n = m_Pool.Length;
		for (int i = 0; i < n; i++)
		{
			m_Pool[i].SetActive (false);
			m_PoolMeshFilter[i].mesh.Clear ();
		}
		Reset ();
	}
	public void End ()
	{
		// program of symmetry
	}
	public void GenerateBeam (Camera cam, Vector3 p1, Vector3 p2, Color c, float sz)
	{
		GenerateBeam (cam, p1, p2, c, c, sz);
	}
	public void GenerateBeam (Camera cam, Vector3 p1, Vector3 p2, Color c1, Color c2, float sz)
	{
		Matrix4x4 c2w = cam.cameraToWorldMatrix;
		Vector3 eye = new Vector3 (c2w.GetColumn (3).x, c2w.GetColumn (3).y, c2w.GetColumn (3).z) - p1;
		Vector3 beam = p1 - p2;
		Vector3 perpbeam = Vector3.Cross (beam, eye);
		Vector3 front = c2w.GetColumn (2);
		Vector3 up = Vector3.Cross (front, perpbeam);
		up.Normalize ();
		Vector3 right = Vector3.Cross (front, up);
		
		Matrix4x4 m1 = new Matrix4x4 ();
		m1 = Matrix4x4.identity;
		m1.SetColumn (0, right);
		m1.SetColumn (1, up);
		m1.SetColumn (2, front);
		m1.SetColumn (3, p1);
		
		Matrix4x4 m2 = new Matrix4x4 ();
		m2 = Matrix4x4.identity;
		m2.SetColumn (0, right);
		m2.SetColumn (1, up);
		m2.SetColumn (2, front);
		m2.SetColumn (3, p2);
		
		Vector3 v00 = m1.MultiplyPoint3x4 (new Vector3 (  0,  sz, 0));
		Vector3 v0h = m1.MultiplyPoint3x4 (new Vector3 (-sz,   0, 0));
		Vector3 vh0 = m1.MultiplyPoint3x4 (new Vector3 ( sz,   0, 0));
		Vector3 v1h = m2.MultiplyPoint3x4 (new Vector3 ( sz,   0, 0));
		Vector3 vh1 = m2.MultiplyPoint3x4 (new Vector3 (-sz,   0, 0));
		Vector3 v11 = m2.MultiplyPoint3x4 (new Vector3 (  0, -sz, 0));
		
		m_Pos[0] = v00;
		m_Pos[1] = vh0;
		m_Pos[2] = v0h;
		m_Pos[3] = v1h;
		m_Pos[4] = vh1;
		m_Pos[5] = v11;

		m_Col[0] = c1;
		m_Col[1] = c1;
		m_Col[2] = c1;
		m_Col[3] = c2;
		m_Col[4] = c2;
		m_Col[5] = c2;

		if (m_NumOfBeam >= m_Pool.Length)
		{
			Debug.Log ("warning: current num of beams are larger than pool !");
			return;
		}
		m_Pool[ m_NumOfBeam ].SetActive (true);
		Mesh mh = m_PoolMeshFilter[ m_NumOfBeam ].mesh;
		mh.vertices = m_Pos;
		mh.colors = m_Col;
		mh.uv = k_TexCoord;
		mh.triangles = k_Tri;
		++m_NumOfBeam;
	}
	private void Reset ()
	{
		m_NumOfBeam = 0;
		Array.Clear (m_Pos, 0, m_Pos.Length);
		Array.Clear (m_Col, 0, m_Col.Length);
	}
}
