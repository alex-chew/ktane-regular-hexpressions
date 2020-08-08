using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rnd = UnityEngine.Random;
using UnityEngine;
using KModkit;

using HF = Hexahedron.Face;
using HV = Hexahedron.Vertex;
using VP = System.Collections.Generic.Dictionary<Hexahedron.Vertex, Hexahedron.Vertex>;

public class RegularHexpressionsController
{

	private static readonly HV[] CENTER_COLUMN_VERTICES = { HV.UBR, HV.DBR, HV.UFL, HV.DFL };
	private static readonly Dictionary<HV, string> VERTEX_LABELS = new Dictionary<HV, string> {
		{ HV.UBR, "+" },
		{ HV.UBL, "*" },
		{ HV.DBR, "^" },
		{ HV.UFR, "?" },
		{ HV.DBL, "$" },
		{ HV.UFL, "." },
		{ HV.DFR, "|" },
		{ HV.DFL, "-" },
	};

	private string logPrefix;
	private PuzzleData.Puzzle puzzle;
	private RegexCoordinates regexCoords;
	private Dictionary<HV, string> vertexWords;
	private Dictionary<HV, string> vertexLabeledWords;

	// Vertex originally position i is now in position vertexPermutation[i]
	public VP vertexPermutation { get; private set; }

	// Vertex at position i was originally at vertexPermutationInverse[i]
	public VP vertexPermutationInverse { get; private set; }

	public bool isSolved { get; private set; }

	public HF? turningFace { get; private set; }

	public RegularHexpressionsController(KMBombInfo bomb, string logPrefix)
	{
		this.logPrefix = logPrefix;

		regexCoords = SampleRegexCoordinates(bomb);
		puzzle = PuzzleData.SamplePuzzle(
			regexCoords.rowNorth, regexCoords.rowSouth, regexCoords.colWest, regexCoords.colEast);
		
		Debug.LogFormat("{0} North-west regex '{1}' matches word '{2}'.",
			logPrefix, puzzle.regexData[0].realStr, puzzle.matchingWords[0]);
		Debug.LogFormat("{0} North-east regex '{1}' matches word '{2}'.",
			logPrefix, puzzle.regexData[1].realStr, puzzle.matchingWords[1]);
		Debug.LogFormat("{0} South-east regex '{1}' matches word '{2}'.",
			logPrefix, puzzle.regexData[2].realStr, puzzle.matchingWords[2]);
		Debug.LogFormat("{0} South-west regex '{1}' matches word '{2}'.",
			logPrefix, puzzle.regexData[3].realStr, puzzle.matchingWords[3]);

		// Assign words to vertices such that at most 2 words are in the correct locations
		string[] wordOrder = puzzle.matchingWords.Concat(puzzle.decoyWords).ToArray();
		int correctLocations;
		do {
			wordOrder.Shuffle();
			correctLocations = CENTER_COLUMN_VERTICES
				.Where((vertex, i) => wordOrder[(int)vertex] == puzzle.matchingWords[i])
				.Count();
		} while (correctLocations > 2);
		vertexWords = new Dictionary<HV, string>(wordOrder.Length);
		vertexLabeledWords = new Dictionary<HV, string>(wordOrder.Length);
		for (int i = 0; i < wordOrder.Length; i++) {
			HV vertex = (HV)i;
			vertexWords[vertex] = wordOrder[i];
			vertexLabeledWords[vertex] = string.Format("({0} {1})", VERTEX_LABELS[vertex], wordOrder[i]);
		}
		Debug.LogFormat("{0} Vertex labels and words: {1}",
			logPrefix,
			vertexLabeledWords.Select(pair => pair.Value).Join(" "));

		// Both permutation and inverse are identity
		vertexPermutation = new VP();
		foreach (HV vertex in System.Enum.GetValues(typeof(HV)))
			vertexPermutation[vertex] = vertex;
		vertexPermutationInverse = new VP(vertexPermutation);
		AssertPermutationInverseIsValid();

		isSolved = false;
		turningFace = null;
	}

	public string GetTopWord()
	{
		HV topVertex = vertexPermutationInverse[0];
		return vertexWords[topVertex];
	}

	public bool TopWordIsMoving()
	{
		return turningFace.HasValue && Hexahedron.VERTICES_BY_FACE[(int)turningFace.Value].Contains(HV.UBR);
	}

	public string GetInitialCoordinates()
	{
		return string.Format("R{0} C{1}", regexCoords.initialRow, regexCoords.initialCol);
	}

	public enum SubmitResult
	{
		IGNORED,
		SUCCESS,
		FAILURE,
	}

	public SubmitResult Submit()
	{
		if (isSolved || turningFace.HasValue)
			return SubmitResult.IGNORED;

		List<HV> submissionVertices = CENTER_COLUMN_VERTICES
			.Select(vertex => vertexPermutationInverse[vertex]).ToList();
		List<string> submission = submissionVertices
			.Select(vertex => vertexWords[vertex]).ToList();
		string submissionText = submissionVertices
			.Select(vertex => vertexLabeledWords[vertex]).Join(", ");
		Debug.LogFormat("{0} Submission {1}.",
			logPrefix, submissionText);
		Debug.LogFormat("{0} Expected words {1}.",
			logPrefix,
			puzzle.matchingWords.Select(word => string.Format("'{0}'", word)).Join(", "));
		
		if (!submission.SequenceEqual(puzzle.matchingWords)) {
			Debug.LogFormat("{0} Strike.", logPrefix);
			return SubmitResult.FAILURE;
		}

		isSolved = true;
		Debug.LogFormat("{0} Pass.", logPrefix);
		return SubmitResult.SUCCESS;
	}

	public bool StartFaceTurn(HF face)
	{
		if (turningFace.HasValue)
			return false;
		
		turningFace = face;
		return true;
	}

	public void FinishFaceTurn()
	{
		Debug.Assert(turningFace.HasValue);

		HV[] faceVertices = Hexahedron.VERTICES_BY_FACE[(int)turningFace.Value];

		// Compute new permutation
		VP newPermutation = new VP(vertexPermutation);
		for (int vertexIndex = 0; vertexIndex < faceVertices.Length; vertexIndex++) {
			HV source = faceVertices[vertexIndex];
			HV destination = faceVertices[(vertexIndex + 1) % faceVertices.Length];
			HV target = vertexPermutationInverse[source];
			newPermutation[target] = destination;
		}

		// Copy into perm and perm inverse
		foreach (HV vertex in System.Enum.GetValues(typeof(HV))) {
			vertexPermutation[vertex] = newPermutation[vertex];
			vertexPermutationInverse[newPermutation[vertex]] = vertex;
		}

		AssertPermutationInverseIsValid();

		turningFace = null;
	}

	private struct RegexCoordinates
	{
		public int initialRow;
		public int initialCol;

		public int rowNorth;
		public int rowSouth;
		public int colWest;
		public int colEast;
	}

	private RegexCoordinates SampleRegexCoordinates(KMBombInfo bomb)
	{
		int initialRow = Random.Range(0, PuzzleData.REGEX_TABLE_ROWS);
		int initialCol = Random.Range(0, PuzzleData.REGEX_TABLE_COLS);
		Debug.LogFormat("{0} Initial regex row is {1}, initial regex column is {2}.",
			logPrefix, initialRow, initialCol);

		int rowOffset = bomb.GetSolvableModuleNames().Count() % (PuzzleData.REGEX_TABLE_ROWS - 1) + 1;
		int finalRow = (initialRow + rowOffset) % PuzzleData.REGEX_TABLE_ROWS;
		Debug.LogFormat("{0} Bomb has {1} non-needy modules,"
		+ " so regex row offset is {1} mod {2} = {3}."
		+ " Final row is ({4} + {3}) mod {5} = {6}.",
			logPrefix,
			bomb.GetSolvableModuleNames().Count(),
			PuzzleData.REGEX_TABLE_ROWS - 1,
			rowOffset,
			initialRow,
			PuzzleData.REGEX_TABLE_ROWS,
			finalRow
		);

		int colOffset = (bomb.GetBatteryCount() + bomb.GetIndicators().Count()) % (PuzzleData.REGEX_TABLE_COLS - 1) + 1;
		int finalCol = (initialCol + colOffset) % PuzzleData.REGEX_TABLE_COLS;
		Debug.LogFormat("{0} Module has {1} batteries and {2} indicators,"
		+ " so regex column offset is ({1} + {2}) mod {3} = {4}."
		+ " Final column is ({5} + {4}) mod {6} = {7}.",
			logPrefix,
			bomb.GetBatteryCount(),
			bomb.GetIndicators().Count(),
			PuzzleData.REGEX_TABLE_COLS - 1,
			colOffset,
			initialCol,
			PuzzleData.REGEX_TABLE_COLS,
			finalCol
		);

		return new RegexCoordinates {
			initialRow = initialRow,
			initialCol = initialCol,
			rowNorth = System.Math.Min(initialRow, finalRow),
			rowSouth = System.Math.Max(initialRow, finalRow),
			colWest = System.Math.Min(initialCol, finalCol),
			colEast = System.Math.Max(initialCol, finalCol),
		};
	}

	private void AssertPermutationInverseIsValid()
	{
		foreach (HV vertex in System.Enum.GetValues(typeof(HV))) {
			Debug.Assert(vertexPermutation[vertexPermutationInverse[vertex]] == vertex);
			Debug.Assert(vertexPermutationInverse[vertexPermutation[vertex]] == vertex);
		}
	}

}
