using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackGenerator : MonoBehaviour
{
	public float areaWidth;
	public float areaLength;
	public float trackWidth;
	// used for the angle of turns
	public float difficulty;
	public float numTurns;
	public int smoothness;
	public int steepness = 0;
	private float height;
	private Mesh trackMesh;
	private Vector3 massCentre;	
	// Inner and outer padding for the coordinates to be generated between
	private float outerPaddingWidth;
	private float outerPaddingLength;
	private float innerPaddingWidth;
	private float innerPaddingLength;
	// Data for the track
	private List<Vector3> baseCoords;
	private List<Vector3> diffCoords;
	private List<Vector3> meshCoords;
	
	// Use this for initialization
	void Start ()
	{
		// Track height boundaries based on the area it covers
		height = ((areaWidth + areaLength) / 50.0f) * steepness;
		
		outerPaddingWidth = areaWidth / 6;
		outerPaddingLength = areaLength / 6;
		innerPaddingWidth = areaWidth / 6;
		innerPaddingLength = areaLength / 6;
		
		// Access the Mesh Filter
		trackMesh = GetComponent<MeshFilter> ().mesh;
		// And Generate the track
		
		GenTrackCoords ();
		TrackDifficulty (difficulty);
		ConstructMesh ();
	}
	
	// Generates the track points
	private void GenTrackCoords ()
	{
		float x, y, z;
		bool zDirecton;
		massCentre = new Vector3 ();
		baseCoords = new List<Vector3> ();
		Vector3 currentCoord;
		
		for (int i = 0; i < numTurns; i++) {
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
			baseCoords.Add (currentCoord);
			
			// Calculate the mass centre as the track coords are generated
			massCentre += currentCoord;
		}
		// Average the mass centre to get it
		massCentre /= numTurns;
		
		// Sort the vertices based on the centre mass and their angle with it
		baseCoords.Sort (compare);
	}
	
	// Helps to order vectors based on their angle with a third one
	// in this case the centre mass of all vectors
	private int compare (Vector3 p1, Vector3 p2)
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
	
	// Simulates the difficulty of the track by displacing points
	private void TrackDifficulty (float diff)
	{
		diffCoords = new List<Vector3> ();
		int trackSize = baseCoords.Count;
		Vector3 prev = baseCoords [trackSize - 1];
		Vector3 cur;
		Vector3 next;
		
		for (int i = 0; i < trackSize; i++) {
			cur = baseCoords [i % trackSize];
			next = baseCoords [(i + 1) % trackSize];
			
			// This is the midpoint between next and previous
			Vector3 midpoint = (next + prev) / 2.0f;
			Vector3 curToMidPoint = midpoint - cur;
			
			// Translate middle vector - new middle = middle + (midpoint * difficulty)
			cur = cur + curToMidPoint * (1.0f - diff);
			prev = cur;
			diffCoords.Add (cur);
		}
		diffCoords.Add (diffCoords [0]);
	}
	
	private void ConstructMesh ()
	{
		trackMesh.Clear ();
		List<Vector2> texCoords = new List<Vector2> ();
		List<int> indices = new List<int> ();
		meshCoords = new List<Vector3> ();
		CatmullRom CRS = new CatmullRom (diffCoords);
		int diffCoordsSize = CRS.pts.Count;
		float step = 1.0f / (diffCoordsSize * smoothness);
		float iterator = 0.0f;
		
		List<Vector3> outerCoords = new List<Vector3> ();
		List<Vector3> innerCoords = new List<Vector3> ();
		
		for (int i = 0; i < diffCoordsSize * smoothness; i++) {
			outerCoords.Add (CRS.DisplaceBy (trackWidth, iterator, step));
			innerCoords.Add (CRS.Interpolate (iterator));
			iterator += step;
		}

		WeldOverlappingSections (outerCoords, innerCoords);

		for (int i = 0; i < diffCoordsSize * smoothness; i++) {
			meshCoords.Add (innerCoords [i]);
			meshCoords.Add (outerCoords [i]);
			meshCoords.Add (innerCoords [i]);
			meshCoords.Add (outerCoords [i]);

			texCoords.Add (new Vector2 (0, 0));
			texCoords.Add (new Vector2 (1, 0));
			texCoords.Add (new Vector2 (0, 1));
			texCoords.Add (new Vector2 (1, 1));

		}
		
		int meshCount = meshCoords.Count;
		for (int i = 0; i < meshCount; i++) {	
			// Tri 1
			indices.Add (i % meshCount);
			indices.Add ((i + 2) % meshCount);
			indices.Add ((i + 1) % meshCount);
			// Tri 2
			indices.Add ((i + 2) % meshCount);
			indices.Add ((i + 3) % meshCount);
			indices.Add ((i + 1) % meshCount);
		} 
		
		trackMesh.vertices = meshCoords.ToArray ();
		trackMesh.uv = texCoords.ToArray ();
		trackMesh.RecalculateNormals ();
		trackMesh.Optimize ();
		trackMesh.SetIndices (indices.ToArray (), MeshTopology.LineStrip, 0);
		
	}
	
	private void WeldOverlappingSections (List<Vector3> outerCoords, List<Vector3> innerCoords)
	{
		Vector3 ps1;
		Vector3 pe1;
		Vector3 ps2;
		Vector3 pe2;
		int size = outerCoords.Count;
		Vector3 contactPoint;
		
		for (int i = 0; i < size; i++) {
			ps1 = outerCoords [i];
			pe1 = outerCoords [(i + 1) % size];
			
			for (int j = i + 2; j < i + size / 2; j++) {
				ps2 = outerCoords [j % size];
				pe2 = outerCoords [(j + 1) % size];
				contactPoint = LineIntersectionPoint (ps1, pe1, ps2, pe2);
				
				if (contactPoint != Vector3.zero) {
					float removedLines = j + 1 - i;
					Vector3 innerFirst = innerCoords [i % size];
					Vector3 innerLast = innerCoords [j % size];
					float step = 1.0f / removedLines; 

					for (int k = i; k < j + 1; k++) {
						Vector3 lerp = Vector3.Lerp (innerFirst, innerLast, (k - i) * step);
						innerCoords [k % size] = contactPoint + (lerp - contactPoint).normalized * trackWidth; 
						outerCoords [k % size] = contactPoint;
					}
					i = j - 2;
					break;
				}
			}
		}
	}
	
	private Vector3 LineIntersectionPoint (Vector3 ps1, Vector3 pe1, Vector3 ps2, Vector3 pe2)
	{
		// Get A,B,C of first line - points : ps1 to pe1
		float A1 = pe1.z - ps1.z;
		float B1 = ps1.x - pe1.x;
		float C1 = A1 * ps1.x + B1 * ps1.z;
		
		// Get A,B,C of second line - points : ps2 to pe2
		float A2 = pe2.z - ps2.z;
		float B2 = ps2.x - pe2.x;
		float C2 = A2 * ps2.x + B2 * ps2.z;
		
		// Get delta
		float delta = A1 * B2 - A2 * B1;
		// If the lines are not intersecting return zero
		if (delta == 0.0f) {
			return Vector3.zero;
		}
		float x = (B2 * C1 - B1 * C2) / delta;
		float z = (A1 * C2 - A2 * C1) / delta;
		
		// If the intersection point does not lie within one of the vectors return zero
		if (x < Mathf.Min (ps1.x, pe1.x) || x > Mathf.Max (ps1.x, pe1.x)) {
			return Vector3.zero;
		}
		if (x < Mathf.Min (ps2.x, pe2.x) || x > Mathf.Max (ps2.x, pe2.x)) {
			return Vector3.zero;
		}
		
		// now return the Vector3 intersection point
		float y = (ps1.y + pe1.y + ps2.y + pe2.y) / 4.0f;
		return new Vector3 (x, y, z);
	}
	
	public void ChangeAreaWidth (float width)
	{
		areaWidth = width;
		GenTrackCoords ();
		TrackDifficulty (difficulty);
		ConstructMesh ();
		
	}
	
	public void ChangeAreaLength (float length)
	{
		areaLength = length;
		GenTrackCoords ();
		TrackDifficulty (difficulty);
		ConstructMesh ();
	}
	
	public void ChangeNumberOfTurns (float newNum)
	{
		numTurns = newNum;
		GenTrackCoords ();
		TrackDifficulty (difficulty);
		ConstructMesh ();
	}
	
	public void ChangeTrackWidth (float width)
	{
		trackWidth = width;
		ConstructMesh ();
	}
	
	public void ChangeDifficulty (float diff)
	{
		difficulty = diff;
		TrackDifficulty (difficulty);
		ConstructMesh ();
	}
	
	public void ChangeSmoothness (float smooth)
	{
		smoothness = (int)smooth;
		ConstructMesh ();
	}

}

