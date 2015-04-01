using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackGenerator : MonoBehaviour
{
	
	
	public int numberOfTurns = 40;
	// Track dimensions (essentially passed from the terrain dimensions)
	public float areaWidth = 200.0f;
	public float areaLength = 400.0f;
	// used for the angle of turns
	public float difficulty = 0.0f;
	public float steepness = 1.0f;
	public float trackWidth = 5.0f;
	public int smoothness = 10;

	private float height;
	private Mesh trackMesh;
	private Vector3 massCentre;	
	// Inner and outer padding for the coordinates to be generated between
	private float outerPaddingWidth;
	private float outerPaddingLength;
	private float innerPaddingWidth;
	private float innerPaddingLength;
	// Data for the track
	private List<Vector3> trackCoords;
	private List<int> trackIndices;

	// Use this for initialization
	void Start ()
	{
		// Track max height is a ratio between the terrain area and steepness factor
		// 1 unit is added to avoid z-Fighting but later this will be resolved by merging
		// the terrain with the track
		height = ((areaWidth + areaLength) / 50.0f) * steepness;


		outerPaddingWidth = areaWidth / 4;
		outerPaddingLength = areaLength / 4;
		innerPaddingWidth = areaWidth / 10;
		innerPaddingLength = areaLength / 10;
		
		// Access the Mesh Filter
		trackMesh = GetComponent<MeshFilter> ().mesh;
		// And Generate the track
		GenerateTrack (smoothness, trackWidth);
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}
	
	// Generates the track
	List<Vector3> genTrackCoords ()
	{
		float x, y, z;
		bool zDirecton;
		massCentre = new Vector3 ();
		Vector3 currentCoord;
		trackCoords = new List<Vector3>();
		
		for (int i = 0; i < numberOfTurns; i++) {
			// Randomize x
			x = Random.Range (outerPaddingWidth - areaWidth, areaWidth - outerPaddingWidth);
			
			// Handles the "No-go zone" to position the track in a nice way
			if (x > -innerPaddingWidth && x < innerPaddingWidth) {
				// Choose a direction at random
				zDirecton = (Random.Range (0, 2) == 0) ? false : true;
				
				z = (zDirecton) ? Random.Range (outerPaddingLength - areaLength, -innerPaddingLength)
					: Random.Range (innerPaddingLength, areaLength - outerPaddingLength);
			} else {
				z = Random.Range (outerPaddingLength - areaLength, areaLength - outerPaddingLength);
			}
			
			// Set a random height of the vertex
			y = Random.Range (0.0f, height);
			currentCoord = new Vector3 (x, y, z);
			trackCoords.Add (currentCoord);
			
			// Calculate the mass centre as the track coords are generated
			massCentre += currentCoord;
		}
		// Average the mass centre to get it
		massCentre /= numberOfTurns;
		
		
		// Sort the vertices based on the centre mass and their angle with it
		trackCoords.Sort (compare);
		NormalizeHeight();
		return trackCoords;
	}
	
	// Helps to order vectors based on their angle with a third one
	// in this case the centre mass of all vectors
	int compare (Vector3 p1, Vector3 p2)
	{
		// Get the vector from every point to the massCentre
		// and compare the angles they have
		float dx1 = p1.x - massCentre.x;
		float dy1 = p1.z - massCentre.z;
		float a1 = Mathf.Atan2 (dy1, dx1);
		
		float dx2 = p2.x - massCentre.x;
		float dy2 = p2.z - massCentre.z;
		float a2 = Mathf.Atan2 (dy2, dx2);
		
		// If the angles are the same, the distance is considered
		if (a1 == a2) {
			float d1 = dx1 * dx1 + dy1 * dy1;
			float d2 = dx2 * dx2 + dy2 * dy2;
			return (d1 - d2 < 0) ? -1 : 1;
		}
		return (a1 - a2 < 0) ? -1 : 1;
	}
	
	// Simulates the difficulty of the track where 1 is max difficulty
	void trackDifficulty (float diff)
	{
		Vector3 first;
		Vector3 middle;
		Vector3 last;
		int trackSize = numberOfTurns;
		int mid;
		int lst;
		
		for (int i = 0; i < trackSize; i+=2) {
			mid = (i + 1) % trackSize;
			lst = (i + 2) % trackSize;
			first = trackCoords [i % trackSize];
			middle = trackCoords [mid];
			last = trackCoords [lst];
			
			// Vectors pointing from the minddle point to the first one 
			// and from the middle point to the one before
			Vector3 midToFirst = new  Vector3 (first.x - middle.x, first.y - middle.y, first.z - middle.z);
			Vector3 midToLast = new Vector3 (last.x - middle.x, last.y - middle.y, last.z - middle.z);
			
			// This is the midpoint between first and last
			Vector3 midpoint = (midToLast + midToFirst) / 2;
			
			// Translate middle vector - new middle = middle + (midpoint * difficulty)
			middle = middle + midpoint * (1.0f - diff);
			trackCoords [mid] = middle;
		}
	}
	
	void GenerateTrack (int smoothFac, float width)
	{
		// Generate the list
		trackCoords = genTrackCoords ();
		trackIndices = new List<int>();
		
		// Always normalise the track turns once and then 
		// use the user specified difficulty to further normalise the track
		trackDifficulty (0.5f);
		trackDifficulty (difficulty);
		
		
		// Create the Mesh
		ConstructMesh (smoothFac, width);
		
		// Add the list to the mesh
		trackMesh.Clear ();
		trackMesh.vertices = trackCoords.ToArray ();
		trackMesh.Optimize ();
		trackMesh.SetIndices (trackIndices.ToArray (), MeshTopology.LineStrip, 0);
	}
	
	void ConstructMesh (int smoothFac, float width)
	{
		int ind = 0; 
		int trackCoordsSize = trackCoords.Count;
		List<Vector3> vertices = new List<Vector3> ();
		List<Vector3> meshVertices = new List<Vector3>();
		List<int> indices = new List<int> ();
		Vector3 outer = new Vector3 ();
		
		for (int i = 0; i < trackCoordsSize; i++) {
			Vector3 previous = trackCoords [(i + trackCoordsSize - 1) % trackCoordsSize];
			Vector3 current = trackCoords [i % trackCoordsSize];
			Vector3 next = trackCoords [(i + 1) % trackCoordsSize];
			
			vertices.AddRange( MakeHalfCurve(previous, current, next, smoothFac));
		}
		
		int vertCount = vertices.Count;
		for (int i = 0; i < vertCount; i+=2) 
		{
			Vector3 prev = vertices [(i + vertCount - 1) % vertCount];
			Vector3 cur = vertices [i];
			Vector3 next = vertices [(i + 1) % vertCount];
			
			Extrusion(prev,ref cur, next, ref outer, width);

			meshVertices.Add(cur);
			meshVertices.Add(outer);

			
			// Tri 1
			indices.Add (ind);
			indices.Add((ind + 2) % vertCount);
			indices.Add((ind + 1) % vertCount);
			// Tri 2
			indices.Add((ind + 2) % vertCount);
			indices.Add((ind + 3) % vertCount);
			indices.Add((ind + 1) % vertCount);

			ind += 2;
		}
		
		trackCoords = meshVertices;
		trackIndices = indices;
		
	}
	
	// Given three points find the curve 
	// midpoints is the amount of points
	public List<Vector3> MakeHalfCurve (Vector3 a, Vector3 b, Vector3 c, int midpoints)
	{
		float step = 1.0f / midpoints; 
		float t = step;
		List<Vector3> result = new List<Vector3> ();
		Vector3 temp;
		Vector3 midAB = (a + b) / 2.0f;
		Vector3 midBC = (b + c) / 2.0f;
		Vector3 midB = (b + (midAB + midBC) / 2.0f) / 2.0f;
		
		result.Add (midAB);
		for (int i = 0; i < midpoints; i++) {
			if (t >= 1.0f) {
				t = 1.0f;
			}
			temp = midAB * Mathf.Pow (1.0f - t, 2) + midB * 2 * t * (1.0f - t) + midBC * Mathf.Pow (t, 2);  
			result.Add (temp);
			t += step;
		}
		result.Add (midBC);
		return result;
	}
	
	// Displaces the point based on its direction towards the next point and the local space
	public void Extrusion (Vector3 prev,ref Vector3 cur, Vector3 next, ref Vector3 outer, float amount)
	{
		Vector3 dir = Vector3.Cross(next- prev, Vector3.down).normalized;
		
		// Get Angle
		Vector3 toCur = (cur - prev).normalized;
		Vector3 pastCur = (next - cur).normalized;
		float angle = Vector3.Angle(toCur , pastCur);
		Vector3 angleSign = Vector3.Cross(toCur, pastCur).normalized;
		if(angleSign.y < 0) { angle = -angle; }
		
		if(angle > 0)
		{
			dir = Quaternion.Euler(0, -angle, 0) * dir; 
			cur += -dir * amount / 2;
			outer = cur + dir * amount;
		}
		else if (angle < 0) 
		{
			dir = Quaternion.Euler(0, -angle, 0) * dir;
			cur += -dir * amount / 2;
			outer = cur + dir * amount;
			
		}
		else
		{
			cur += -dir * amount / 2;
			outer = cur + dir * amount;
		}
	}

	// Fixes drastic track altitudes
	public void NormalizeHeight()
	{
		int tSize = trackCoords.Count;
		for(int i = 0 ; i < tSize; i+=2 )
		{
			Vector3 prev = trackCoords [(i + tSize - 1) % tSize];
			Vector3 cur = trackCoords [i];
			Vector3 next = trackCoords [(i + 1) % tSize];

			cur.y = (prev.y + next.y) / 2.0f;
			trackCoords[i] = cur;
		}
	}

	
}

