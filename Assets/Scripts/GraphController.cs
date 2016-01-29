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
    float defaultFOV;

    // called when the script instance is being loaded.
    void Awake()
    {
        controller = new Controller();
		startAction = false;
		deselect = false;
        controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
        controller.Config.SetFloat("Gesture.Swipe.MinLength", 200.0f); // minimum distance of swipe: 200 mm
        controller.Config.SetFloat("Gesture.Swipe.MinVelocity", 750f); // minimum velocity of swipe: 750 mm / s
        controller.Config.Save();
        defaultFOV = 65.0f;
        camera.fieldOfView = defaultFOV;
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
		if (extendedR.Count == 1 && rightHand.IsRight) {
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


            // DETECT A  LEFT HAND SWIPE TO DELETE A NODE
            GestureList maybeGestures = controller.Frame().Gestures();
            for (int i = 0; i < maybeGestures.Count; i++) {
                Gesture gesture = maybeGestures[i];
                // check if the gesture is invalid which can happen
                if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_SWIPE) {
                    HandList involvedHands = gesture.Hands;
                    if (involvedHands.Count > 0 && involvedHands.Count < 2 && involvedHands[0].IsLeft)
                    {
                        // delete the node
                        if (hoveredNode != null) {
                            int dolphinID = 0; Int32.TryParse(hoveredNode.name, out dolphinID);
                            readgml.NODE toDeleteNode = graph.nodes[dolphinID];
                            
                            // remove the edges from graph
                            for (int e = 0; e < toDeleteNode.edgeList.Count; e++) {
                                readgml.EDGE tDNEdge = toDeleteNode.edgeList[e];

                                // find it in the graph list and delete it
                                for(int graphE = 0; graphE < graph.edges.Count; graphE++) {
                                    readgml.EDGE edge = graph.edges[graphE];
                                    if (edge.source == tDNEdge.source && edge.target == tDNEdge.target) {
                                        graph.edges.RemoveAt(graphE);
                                        Transform edgeToRemove = ForceDirected.edgeList[graphE];
                                        ForceDirected.edgeList.RemoveAt(graphE);
                                        GameObject.Destroy(edgeToRemove.gameObject);
                                        graph.n_edges--;
                                        break;
                                    }
                                }
                            }

                            // remove node from graph
                            graph.nodes.Remove(dolphinID);
                            ForceDirected.nodeList[dolphinID].gameObject.SetActive(false);

                            // recalculate the graph
                            ForceDirected.InitGraph(graph);
                            ForceDirected.GenerateGraph();
                            taggedText.GetComponent<UnityEngine.UI.Text>().text = "Try deleting";
                            defaultFOV = defaultFOV + 5;
                            camera.fieldOfView = defaultFOV;
                        }
                    }
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
		else if (controller.Frame().Hands.Count == 1 && leftHand.IsRight && tipDistance < 0.02 && rightHand.PinchStrength == 1.0 
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
					camera.fieldOfView = defaultFOV;
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