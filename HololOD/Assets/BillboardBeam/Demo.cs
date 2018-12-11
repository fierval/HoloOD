using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Demo : MonoBehaviour
{
	private VolumetricLaserBeam m_BBMgr = null;
	private int m_CurrDemo = 0;

	class Beam
	{
		public Vector3 start;
		public Vector3 end;
		public Color color;
		public float size;
	}
	private List<Beam> m_BeamList = new List<Beam>();
	private System.Random m_Rnd = new System.Random();
	private Color[] m_ColLib = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };
	[Range(1, 8)] public float m_BeamSize = 3;

	void Start ()
	{
		m_BBMgr = GetComponent<VolumetricLaserBeam>();
		m_BBMgr.PreAlloc (this.gameObject.transform, 512);
		InvokeRepeating ("GenBeamRandom", 0.5f, 0.3f);
	}
	void Update ()
	{
		m_BBMgr.Begin ();
		if (m_CurrDemo == 0)
		{
			m_BBMgr.GenerateBeam (Camera.main, new Vector3 (-20.0f, -20.0f, 40.0f), new Vector3 (-20.0f, -20.0f, 240.0f), Color.red, Color.yellow, m_BeamSize);
			m_BBMgr.GenerateBeam (Camera.main, new Vector3 (-20.0f,  20.0f, 40.0f), new Vector3 (-20.0f,  20.0f, 240.0f), Color.yellow, Color.red, m_BeamSize);
			m_BBMgr.GenerateBeam (Camera.main, new Vector3 (-20.0f, -8.0f, 40.0f), new Vector3 (-20.0f, -8.0f, 240.0f), Color.green, Color.blue, m_BeamSize);
			m_BBMgr.GenerateBeam (Camera.main, new Vector3 (-20.0f,  8.0f, 40.0f), new Vector3 (-20.0f,  8.0f, 240.0f), Color.blue, Color.green, m_BeamSize);
		}
		else if (m_CurrDemo == 1)
		{
			// add a beam start from camera position
			if (Input.GetMouseButtonDown (0))
			{
				Beam b = new Beam ();
				Vector3 startPos = Camera.main.transform.position - new Vector3 (6, 6, 0);
				Vector3 Offset = new Vector3 (0, 0, -30);
				b.start = startPos;
				b.end = startPos - Offset;
				b.color = m_ColLib[ m_Rnd.Next (0, 4) ];
				b.size = 3;
				m_BeamList.Add (b);
			}

			// remove beam too far away
			List<Beam> temp = new List <Beam>();
			foreach (Beam obj in m_BeamList)
			{
				if (obj.end.z < 500)
					temp.Add (obj);
			}
			m_BeamList = temp;

			// update and render all beam
			m_BeamList.ForEach(delegate(Beam obj)
			{
				Vector3 speed = new Vector3 (0, 0, 2);
				obj.start += speed;
				obj.end += speed;
				m_BBMgr.GenerateBeam (Camera.main, obj.start, obj.end, obj.color, obj.size);
			});
		}
		else if (m_CurrDemo == 2)
		{
			// remove beam too far away
			const float k_DistDisappear = 500f;
			const float k_DistFading = 300f;
			int cnt = m_BeamList.Count;
			List<Beam> temp = new List <Beam> (cnt);
			for (int i = 0; i < cnt; i++)
			{
				Beam obj = m_BeamList[i];
				if (obj.end.z < k_DistDisappear)
					temp.Add (obj);
			}
			m_BeamList = temp;

			// update and render all beam
			cnt = m_BeamList.Count;
			for (int i = 0; i < cnt; i++)
			{
				Beam obj = m_BeamList[i];
				Vector3 speed = new Vector3 (0, 0, 1);
				obj.start += speed;
				obj.end += speed;
				float f = (k_DistDisappear - obj.end.z) / (k_DistDisappear - k_DistFading);
				if (obj.end.z > k_DistFading)
					obj.color = Color.Lerp (Color.black, obj.color, f);
				m_BBMgr.GenerateBeam (Camera.main, obj.start, obj.end, obj.color, m_BeamSize);
			}
		}
		m_BBMgr.End ();
	}
	private void GenBeamRandom ()
	{
		if (m_CurrDemo != 2)
			return;

		for (int i = 0; i < 10; i++)
		{
			Beam b = new Beam ();
			Vector3 startPos = new Vector3 (
				m_Rnd.Next (-200, 200),
				m_Rnd.Next (-200, 200),
				Camera.main.transform.position.z);
			Vector3 Offset = new Vector3 (0, 0, -30);
			b.start = startPos;
			b.end = startPos - Offset;
			b.color = m_ColLib[ m_Rnd.Next (0, 4) ];
//			b.size = 3;
			m_BeamList.Add (b);
		}
	}
	private void OnGUI()
	{
		int btnW = 70;
		int btnH = 30;
		GUI.Label (new Rect (10, 10, 250, 30), "Volumetric Laser Beam Demo");
		if (GUI.Button (new Rect (10, 30, btnW, btnH), "Demo 1"))
		{
			m_CurrDemo = 0;
			m_BeamList.Clear ();
		}
		if (GUI.Button (new Rect (10, btnH * 1 + 30, btnW, btnH), "Demo 2"))
		{	
			m_CurrDemo = 1;
			m_BeamList.Clear ();
		}
		if (GUI.Button (new Rect (10, btnH * 2 + 30, btnW, btnH), "Demo 3"))
		{
			m_CurrDemo = 2;
			m_BeamList.Clear ();
		}
		GUI.Label (new Rect (10, btnH * 3 + 30, 250, 30), "Beam " + m_BBMgr.Size () + "/" + m_BBMgr.Capacity ());
		if (m_CurrDemo == 1)
			GUI.Label (new Rect (10, btnH * 4 + 30, 250, 30), "Click left mouse, Fire a laser");
	}
}
