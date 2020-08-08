using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RegularHexpressionsModule : MonoBehaviour
{
	public KMBombInfo Bomb;
	public KMBombModule Module;
	public KMAudio Audio;

	public GameObject topWordText;
	public GameObject initialCoordinateText;

	// Must be in the same order as Hexahedron.Vertex
	public GameObject[] vertexObjects;

	// Must be in the same order as Hexahedron.Face
	public KMSelectable faceButtonU;
	public KMSelectable faceButtonD;
	public KMSelectable faceButtonF;
	public KMSelectable faceButtonB;
	public KMSelectable faceButtonL;
	public KMSelectable faceButtonR;

	public KMSelectable submitButton;

	private RegularHexpressionsController controller;

	private Dictionary<Hexahedron.Vertex, Vector3> vertexPositions;

	private static int _moduleIdCounter = 1;
	private int _moduleId;

	void Start()
	{
		_moduleId = _moduleIdCounter++;

		controller = new RegularHexpressionsController(Bomb, LogPrefix());

		vertexPositions = new Dictionary<Hexahedron.Vertex, Vector3>();
		for (int vertexIndex = 0; vertexIndex < vertexObjects.Length; vertexIndex++) {
			vertexPositions[(Hexahedron.Vertex)vertexIndex] = vertexObjects[vertexIndex].transform.localPosition;
		}

		faceButtonU.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.UP, faceButtonU);
		faceButtonD.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.DOWN, faceButtonD);
		faceButtonF.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.FRONT, faceButtonF);
		faceButtonB.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.BACK, faceButtonB);
		faceButtonL.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.LEFT, faceButtonL);
		faceButtonR.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.RIGHT, faceButtonR);

		submitButton.OnInteract += GetSubmitButtonPressHandler(submitButton);

		Render();
	}

	private KMSelectable.OnInteractHandler GetFaceButtonPressHandler(Hexahedron.Face face, KMSelectable button)
	{
		return delegate {
			button.AddInteractionPunch();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			if (controller.StartFaceTurn(face)) {
				Render();
				StartCoroutine(FaceTurnAnimation(face));
			}
			return false;
		};
	}

	private KMSelectable.OnInteractHandler GetSubmitButtonPressHandler(KMSelectable button)
	{
		return delegate {
			button.AddInteractionPunch();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

			switch (controller.Submit()) {
			case RegularHexpressionsController.SubmitResult.FAILURE:
				Module.HandleStrike();
				Render();
				break;
			case RegularHexpressionsController.SubmitResult.SUCCESS:
				Module.HandlePass();
				Render();
				break;
			}
			return false;
		};
	}

	private struct VertexAnimationData
	{
		public GameObject vertexObject;
		public Vector3 startPosition;
		public Vector3 endPosition;
	}

	private IEnumerator FaceTurnAnimation(Hexahedron.Face face)
	{
		Hexahedron.Vertex[] faceVertices = Hexahedron.VERTICES_BY_FACE[(int)face];
		VertexAnimationData[] animationData = faceVertices.Select((vertex, faceVertexIndex) => new VertexAnimationData {
			vertexObject = vertexObjects[(int)controller.vertexPermutationInverse[vertex]],
			startPosition = vertexPositions[vertex],
			endPosition = vertexPositions[faceVertices[(faceVertexIndex + 1) % faceVertices.Length]],
		}).ToArray();

		float elapsedTime = 0f;
		float duration = 0.6f;
		while (elapsedTime < duration) {
			yield return null;
			elapsedTime += Time.deltaTime;
			foreach (VertexAnimationData ad in animationData) {
				float progress = Mathf.Min(1f, elapsedTime / duration);
				ad.vertexObject.transform.localPosition = Vector3.Lerp(ad.startPosition, ad.endPosition, progress);
			}
		}

		// Ensure vertices stop at exactly the correct position
		foreach (VertexAnimationData ad in animationData) {
			ad.vertexObject.transform.localPosition = ad.endPosition;
		}

		controller.FinishFaceTurn();
		Render();
	}

	private void Render()
	{
		topWordText.GetComponent<TextMesh>().text = controller.GetTopWord();
		topWordText.GetComponent<MeshRenderer>().enabled = !controller.TopWordIsMoving();

		initialCoordinateText.GetComponent<TextMesh>().text = controller.GetInitialCoordinates();
		initialCoordinateText.GetComponent<MeshRenderer>().enabled = controller.TopWordIsMoving();
	}

	private string LogPrefix()
	{
		return string.Format("[Regular Hexpressions #{0}]", _moduleId);
	}

	void Update()
	{

	}
}
