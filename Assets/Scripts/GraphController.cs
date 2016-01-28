using UnityEngine;
using System;
using Leap;

public class GraphController : MonoBehaviour {
	
	public Camera camera;
	public GameObject cursor;
	public GameObject hoveredNode;
	public GameObject selectedNode;
    Controller controller;
	bool deselect;
	static readgml.GRAPH graph;
	int selectMode = 1;

    Hand rightHand;
	Hand leftHand;
	Finger indexFinger;
	Finger thumbFinger;
	FingerList extendedR;
	FingerList extendedL;

    Vector3 currPosition;
    Vector3 prevPosition;
    Vector3 handDelta;
	Vector3 pinchDistance;
	float tipDistance;

	Vector3 currMainPosition;
	Vector3 currOffPosition;
	Vector3 currDistance;
	Vector3 prevDistance;
	float magnitude;

	bool startAction;
	float lastTime;
	float timeDiff;

    // called when the script instance is being loaded.
    void Awake()
    {
        controller = new Controller();
		startAction = false;
		deselect = false;
    }

	public static void setGraph (readgml.GRAPH g) {
		graph = g;
	}

    void Update()
    {
		// Label variables
		GameObject taggedText = GameObject.FindWithTag ("CurrAction");

		// Rotate variables
        rightHand = controller.Frame().Hands.Rightmost;
		leftHand = controller.Frame ().Hands.Leftmost;
		extendedR = rightHand.Fingers.Extended ();
		extendedL = leftHand.Fingers.Extended ();
		indexFinger = controller.Frame().Hands.Rightmost.Fingers [(int)Finger.FingerType.TYPE_INDEX];
		thumbFinger = controller.Frame().Hands.Rightmost.Fingers [(int)Finger.FingerType.TYPE_THUMB];
		pinchDistance = (indexFinger.TipPosition - thumbFinger.TipPosition).ToUnityScaled();
		tipDistance = pinchDistance.magnitude;
		currPosition = rightHand.PalmPosition.ToUnityScaled();

		// Zoom variables
		currMainPosition = rightHand.PalmPosition.ToUnityScaled();
		currOffPosition = leftHand.PalmPosition.ToUnityScaled();
		currDistance = currMainPosition - currOffPosition;

	//////////////////////////////////////////////////////////////////////////////////////////

		// USING CURSOR
		if (extendedR.Count == 1) {
			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Using Cursor";
			cursor.transform.position = new Vector3(indexFinger.TipPosition.ToUnityScaled ().x * 50, 
				(indexFinger.TipPosition.ToUnityScaled ().y * 30) - 5,
				-60);

			Ray ray = new Ray(camera.transform.position, cursor.transform.position - camera.transform.position);
			RaycastHit raycastInfo;					
			Color c = new Color (255/255f, 116/255f, 26/255f, 255/255f);
			if(Physics.Raycast(ray, out raycastInfo, 1000)) {
				if(hoveredNode != null && hoveredNode.name.CompareTo(raycastInfo.transform.gameObject.name) != 0) {
					hoveredNode.GetComponent<Renderer> ().material.SetColor("_Color", c);
					deselect = false;
				}

				hoveredNode = raycastInfo.transform.gameObject;
				if (!deselect) {
					hoveredNode.GetComponent<Renderer> ().material.SetColor ("_Color", Color.blue);
					int x = 0; Int32.TryParse (hoveredNode.name, out x);
					string dolphinName = graph.nodes [x].label;
					GameObject.FindWithTag ("DolphinName").GetComponent<UnityEngine.UI.Text> ().text = dolphinName;
				}
				if (deselect) {
					hoveredNode.GetComponent<Renderer> ().material.SetColor("_Color", c);
					GameObject.FindWithTag ("DolphinName").GetComponent<UnityEngine.UI.Text> ().text = "";
					hoveredNode = null;
				}
			}
			else {
				if (hoveredNode != null) {
					deselect = true;
				} else {
					deselect = false;
				}
			}

		}

		// CHANGE SELECT MODE (LEFT HAND): 1 FINGER = NORMAL SELECT; 2 FINGER = VOLUME SELECT; 3 FINGER = SEQUENTIAL SELECT
		else if(controller.Frame().Hands.Count == 1 && rightHand.IsLeft) {
			if (extendedL.Count >= 1 && extendedL.Count <= 3) {
				selectMode = extendedL.Count;
			}
		}

		// ROTATE GRAPH
		else if (controller.Frame().Hands.Count == 1 && leftHand.IsRight && tipDistance < 0.017 && rightHand.PinchStrength == 1.0 
			&& rightHand.GrabStrength != 1.0 && extendedR.Count > 1)
        {
            handDelta = currPosition - prevPosition;
            handDelta *= 1000;

            transform.rotation = Quaternion.Euler(handDelta.y, -handDelta.x, 0) * transform.rotation;
			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Rotating";
        }

		// ZOOM GRAPH
		else if (controller.Frame().Hands.Count == 2 && (rightHand.PinchStrength == 1.0 && leftHand.PinchStrength == 1.0))
		{
			magnitude = currDistance.magnitude - prevDistance.magnitude;
			magnitude *= -600;

			if (camera.fieldOfView + magnitude > 180)
				camera.fieldOfView = 179;

			else if (camera.fieldOfView + magnitude < 0)
				camera.fieldOfView = 1;

			else
				camera.fieldOfView += magnitude;

			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Zooming";
		}

		// RESET ZOOM
		else if (controller.Frame ().Hands.Count == 1 && rightHand.GrabStrength == 1.0) {
			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Resetting view...";
			if(startAction == false) {
				startAction = true;
				lastTime = Time.time;
				timeDiff = 0;
			}
			else {
				timeDiff = Time.time - lastTime;
				if(timeDiff > 2) {
					startAction = false;
					// do action here
					camera.fieldOfView = 65;
				}
			}
		}

		// STANDING BY
		else {
			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Standing By";
		}
			
	//////////////////////////////////////////////////////////////////////////////////////////

		// RESET ZOOM CONTROL
		if (controller.Frame ().Hands.Count != 1 || rightHand.GrabStrength != 1.0) {
			startAction = false;

		}


		prevDistance = currDistance;




		// used for rotate
        prevPosition = currPosition;
    }
}