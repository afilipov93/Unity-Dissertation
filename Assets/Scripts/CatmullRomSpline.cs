using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CatmullRom {
	public List<Vector3> pts;
	
	public CatmullRom(List<Vector3> ptsInput) 
	{
		pts = new List<Vector3>(ptsInput);
		pts.Add(pts[pts.Count - 1]);
		pts.Insert(0,pts[0] - (pts[0] - pts[1]));
	}
	
	public Vector3 Interpolate(float t) 
	{
		int numSections = pts.Count - 3;
		int currPt = Mathf.Min(Mathf.FloorToInt(t * (float) numSections), numSections - 1);
		float u = t * (float) numSections - (float) currPt;
		
		Vector3 a = pts[currPt];
		Vector3 b = pts[currPt + 1];
		Vector3 c = pts[currPt + 2];
		Vector3 d = pts[currPt + 3];
		
		return 0.5f * (
			(-a + 3f * b - 3f * c + d) * (u * u * u)
			+ (2f * a - 5f * b + 4f * c - d) * (u * u)
			+ (-a + c) * u
			+ 2f * b
			);
	}

	public Vector3 DisplaceBy(float amount, float time, float timeStep)
	{
		Vector3 prev = Interpolate((time - timeStep + 1.0f) % 1.0f);
		Vector3 cur = Interpolate(time);
		Vector3 next = Interpolate((time + timeStep) % 1.0f);
		Vector3 v1 = (cur - prev).normalized;
		Vector3 v2 = (next - cur).normalized;
		Vector3 dir = (Vector3.Cross(Vector3.up, v1) + Vector3.Cross(Vector3.up, v2)).normalized;
	
		return cur + dir * amount;
	}
}