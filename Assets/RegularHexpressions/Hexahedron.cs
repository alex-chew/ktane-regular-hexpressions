using System.Collections;
using System.Collections.Generic;

public class Hexahedron
{

	public enum Face
	{
		UP,
		DOWN,
		FRONT,
		BACK,
		LEFT,
		RIGHT,
	}

	// Order by rows as shown on module
	public enum Vertex
	{
		UBR = 0,
		UBL = 1,
		DBR = 2,
		UFR = 3,
		DBL = 4,
		UFL = 5,
		DFR = 6,
		DFL = 7,
	}

	// In each array, vertices are listed in rotation order
	public static readonly Vertex[][] VERTICES_BY_FACE = new Vertex[][] {
		new Vertex[] { Vertex.UBL, Vertex.UBR, Vertex.UFR, Vertex.UFL },  // UP
		new Vertex[] { Vertex.DBL, Vertex.DBR, Vertex.DFR, Vertex.DFL },  // DOWN
		new Vertex[] { Vertex.UFL, Vertex.UFR, Vertex.DFR, Vertex.DFL },  // FRONT
		new Vertex[] { Vertex.UBL, Vertex.UBR, Vertex.DBR, Vertex.DBL },  // BACK
		new Vertex[] { Vertex.UBL, Vertex.UFL, Vertex.DFL, Vertex.DBL },  // LEFT
		new Vertex[] { Vertex.UBR, Vertex.UFR, Vertex.DFR, Vertex.DBR },  // RIGHT
	};

}
