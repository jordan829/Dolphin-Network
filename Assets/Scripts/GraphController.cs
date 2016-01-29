using UnityEngine;
using System;
using Leap;
using System.Collections.Generic;

public class GraphController : MonoBehaviour {

	public Camera camera;
	public GameObject cursor;
	public GameObject wideCursor;
	public GameObject hoveredNode;
	public List<GameObject> selectedNodes;
    Controller controller;
	bool deselect;
	static readgml.GRAPH graph;
	int selectMode = 1;
	bool showingConnections;
	bool cursOnNode;

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

	bool startLeftClench;
	float lastTimeLeftClench;
	float timeDiffLeftClench;

    // called when the script instance is being loaded.
    void Awake()
    {
        controller = new Controller();
		startAction = false;
		deselect = false;

		// enable swipes
        controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
        controller.Config.SetFloat("Gesture.Swipe.MinLength", 200.0f); // minimum distance of swipe: 200 mm
        controller.Config.SetFloat("Gesture.Swipe.MinVelocity", 750f); // minimum velocity of swipe: 750 mm / s
        //controller.Config.Save();

		controller.EnableGesture(Gesture.GestureType.TYPE_KEY_TAP);
		controller.Config.SetFloat("Gesture.KeyTap.MinDownVelocity", 35.0f); // minimum velocity: 70 mm/s
		controller.Config.SetFloat("Gesture.KeyTap.MinDistance", 3.0f); // minimum distance: mm/s
		controller.Config.Save();

        defaultFOV = 65.0f;
        camera.fieldOfView = defaultFOV;
		showingConnections = false;
		selectedNodes = new List<GameObject> ();
		cursOnNode = false;
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
		if (extendedR.Count == 1 && rightHand.IsRight && selectMode == 1) {
			if (!showingConnections)
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Using Cursor";
			else
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Selected. Pausing Cursor";
			cursor.transform.position = new Vector3 (indexFinger.TipPosition.ToUnityScaled ().x * 150, 
				(indexFinger.TipPosition.ToUnityScaled ().y * 90) - 25,
				camera.transform.position.z + 10);

			Ray ray = new Ray (camera.transform.position, cursor.transform.position - camera.transform.position);
			RaycastHit raycastInfo;					
			Color c = new Color (255 / 255f, 116 / 255f, 26 / 255f, 255 / 255f);
			if (Physics.Raycast (ray, out raycastInfo, 1000) && !showingConnections) {
				if (hoveredNode != null && hoveredNode.name.CompareTo (raycastInfo.transform.gameObject.name) != 0) {
					// if the node selected is different from the last one
					hoveredNode.GetComponent<Renderer> ().material.SetColor ("_Color", c);
					deselect = false;
				}

				// assign the new node on both cases
				hoveredNode = raycastInfo.transform.gameObject;
				if (!deselect) {
					hoveredNode.GetComponent<Renderer> ().material.SetColor ("_Color", Color.blue);
					int x = 0;
					Int32.TryParse (hoveredNode.name, out x);
					string dolphinName = graph.nodes [x].label;
					GameObject.FindWithTag ("DolphinName").GetComponent<UnityEngine.UI.Text> ().text = dolphinName;
				}
				if (deselect) {
					// un-assign the node if the new node is the same as the old one
					hoveredNode.GetComponent<Renderer> ().material.SetColor ("_Color", c);
					GameObject.FindWithTag ("DolphinName").GetComponent<UnityEngine.UI.Text> ().text = "";
					hoveredNode = null;
				}
			} else if(!showingConnections) {
				if (hoveredNode != null) {
					deselect = true;
				} else {
					deselect = false;
				}
			}

			// DETECT A LEFT HAND SWIPE TO DELETE A NODE
			GestureList maybeGestures = controller.Frame ().Gestures ();
			for (int i = 0; i < maybeGestures.Count; i++) {
				Gesture gesture = maybeGestures [i];
				// check if the gesture is invalid which can happen
				if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_SWIPE) {
					HandList involvedHands = gesture.Hands;
					if (involvedHands.Count == 1 && involvedHands [0].IsLeft) {
						// call DestroyNode()
						DestroyNode (hoveredNode); // always destroys input node
					}
				}
			}

			// DETECT A LEFT HAND CIRCLE TO SHOW CONNECTIONS
			for (int i = 0; i < maybeGestures.Count; i++) {
				Gesture gesture = maybeGestures [i];
				// check if gesture is invalid
				if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_KEY_TAP) {
					HandList involvedHands = gesture.Hands;
					if (involvedHands.Count == 1 && involvedHands [0].IsLeft) {
						//toggle showConnections
						showingConnections = !showingConnections;
						ShowConnect (hoveredNode);
					}
				}
			}

			// DETECT A HELD FIST IN LEFT HAND
			if (controller.Frame ().Hands.Count == 2 && leftHand.GrabStrength == 1.0 && leftHand.IsLeft) {
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Centering Object...";
				if (startLeftClench == false) {
					startLeftClench = true;
					lastTimeLeftClench = Time.time;
					timeDiffLeftClench = 0;
				} else {
					timeDiffLeftClench = Time.time - lastTimeLeftClench;
					if (timeDiffLeftClench > 3) {
						startLeftClench = false;
						// do action here
						Vector3 fromDir = hoveredNode.transform.position;
						Vector3 toDir = new Vector3 (0, 0, -5);
						transform.rotation = Quaternion.FromToRotation (fromDir, toDir) * transform.rotation;
					}
				}
			}

		} // end Using Cursor for mode 1 ///////////////////////////////////////////////////////////////////

		else if (extendedR.Count == 1 && rightHand.IsRight && selectMode == 3) {
			if(!showingConnections)
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Using Cursor";
			else
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Selected. Pausing Cursor";
			cursor.transform.position = new Vector3 (indexFinger.TipPosition.ToUnityScaled ().x * 150, 
				(indexFinger.TipPosition.ToUnityScaled ().y * 90) - 25,
				camera.transform.position.z + 10);

			Ray ray = new Ray (camera.transform.position, cursor.transform.position - camera.transform.position);
			RaycastHit raycastInfo;					
			Color c = new Color (255 / 255f, 116 / 255f, 26 / 255f, 255 / 255f);
			if (Physics.Raycast (ray, out raycastInfo, 1000) && !showingConnections) {
				if (selectedNodes.Contains (raycastInfo.transform.gameObject) && !cursOnNode) {
					// reset node color if the node is already selected
					selectedNodes [selectedNodes.IndexOf (raycastInfo.transform.gameObject)].GetComponent<Renderer> ().material.SetColor ("_Color", c);
					selectedNodes.RemoveAt (selectedNodes.IndexOf (raycastInfo.transform.gameObject));
				} 
				else if(!cursOnNode) {
					raycastInfo.transform.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", Color.blue);
					selectedNodes.Add (raycastInfo.transform.gameObject);
				}
				cursOnNode = true;
			} 
			else {
				cursOnNode = false;
			}

			// DETECT A LEFT HAND SWIPE TO DELETE A NODE
			GestureList maybeGestures = controller.Frame ().Gestures ();
			for (int i = 0; i < maybeGestures.Count; i++) {
				Gesture gesture = maybeGestures [i];
				// check if the gesture is invalid which can happen
				if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_SWIPE) {
					HandList involvedHands = gesture.Hands;
					if (involvedHands.Count == 1 && involvedHands [0].IsLeft) {
						// call DestroyNode()
						while(selectedNodes.Count > 0) {
							DestroyNode (selectedNodes[0]);
							selectedNodes.RemoveAt (0);
						}
					}
				}
			}

			// DETECT A LEFT HAND CIRCLE TO SHOW CONNECTIONS
			for (int i = 0; i < maybeGestures.Count; i++) {
				Gesture gesture = maybeGestures [i];
				// check if gesture is invalid
				if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_KEY_TAP) {
					HandList involvedHands = gesture.Hands;
					if (involvedHands.Count == 1 && involvedHands [0].IsLeft) {
						//toggle showConnections
						showingConnections = !showingConnections;
						for (int n = 0; n < selectedNodes.Count; n++) {
							ShowConnect (selectedNodes[n]);
						}
					}
				}
			}
		} /////// end of Sequential Select mode ///////////////////////////////////////////////


		else if (extendedR.Count == 1 && rightHand.IsRight && selectMode == 2) {
			if(!showingConnections)
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Using Cursor";
			else
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Selected. Pausing Cursor";
			wideCursor.transform.position = new Vector3 (indexFinger.TipPosition.ToUnityScaled ().x * 200, 
				(indexFinger.TipPosition.ToUnityScaled ().y * 130) - 42,
				0);

			Color c = new Color (255 / 255f, 116 / 255f, 26 / 255f, 255 / 255f);

			if (!showingConnections) {
				// clear all selection
				while (selectedNodes.Count > 0) {
					selectedNodes [0].GetComponent<Renderer> ().material.SetColor ("_Color", c);
					selectedNodes.RemoveAt (0);
				}

				// choose all selected
				for (int n = 0; n < ForceDirected.nodeList.Count; n++) {
					GameObject node = ForceDirected.nodeList [n].gameObject;
					float znode = node.transform.position.z - camera.transform.position.z;
					float zcursor = wideCursor.transform.position.z - camera.transform.position.z;
					float xmin = znode * (wideCursor.transform.position.x - 10) / (zcursor);
					float xmax = znode * (wideCursor.transform.position.x + 10) / (zcursor);
					float ymin = znode * (wideCursor.transform.position.y - 10) / (zcursor);
					float ymax = znode * (wideCursor.transform.position.y + 10) / (zcursor);

					if (node.transform.position.x >= xmin &&
					  node.transform.position.x <= xmax &&
					  node.transform.position.y >= ymin &&
					  node.transform.position.y <= ymax) {
						node.GetComponent<Renderer> ().material.SetColor ("_Color", Color.blue);
						selectedNodes.Add (node);
					}
				}
			}

			// DETECT A LEFT HAND SWIPE TO DELETE A NODE
			GestureList maybeGestures = controller.Frame ().Gestures ();
			for (int i = 0; i < maybeGestures.Count; i++) {
				Gesture gesture = maybeGestures [i];
				// check if the gesture is invalid which can happen
				if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_SWIPE) {
					HandList involvedHands = gesture.Hands;
					if (involvedHands.Count == 1 && involvedHands [0].IsLeft) {
						// call DestroyNode()
						while(selectedNodes.Count > 0) {
							DestroyNode (selectedNodes[0]);
							selectedNodes.RemoveAt (0);
						}
					}
				}
			}

			// DETECT A LEFT HAND CIRCLE TO SHOW CONNECTIONS
			for (int i = 0; i < maybeGestures.Count; i++) {
				Gesture gesture = maybeGestures [i];
				// check if gesture is invalid
				if (gesture.IsValid && gesture.Type == Gesture.GestureType.TYPE_KEY_TAP) {
					HandList involvedHands = gesture.Hands;
					if (involvedHands.Count == 1 && involvedHands [0].IsLeft) {
						//toggle showConnections
						showingConnections = !showingConnections;
						for (int n = 0; n < selectedNodes.Count; n++) {
							ShowConnect (selectedNodes[n]);
						}
					}
				}
			}
		} /////// end of Volume Select mode /////////////////////////////////////


		// CHANGE SELECT MODE (LEFT HAND): 1 FINGER = NORMAL SELECT; 2 FINGER = VOLUME SELECT; 3 FINGER = SEQUENTIAL SELECT
		else if(controller.Frame().Hands.Count == 1 && rightHand.IsLeft) {
			if (extendedL.Count >= 1 && extendedL.Count <= 3 && !showingConnections) {
				selectMode = extendedL.Count;
				GameObject modeText = GameObject.FindWithTag ("Mode");
				switch (extendedL.Count) {
					case 2:
						modeText.GetComponent<UnityEngine.UI.Text> ().text = "Volume";
						cursor.SetActive (false);
						wideCursor.SetActive (true);
						break;
					case 3:
						modeText.GetComponent<UnityEngine.UI.Text> ().text = "Sequential";
						cursor.SetActive (true);
						wideCursor.SetActive (false);
						break;
					case 1:
						modeText.GetComponent<UnityEngine.UI.Text> ().text = "Single";
						cursor.SetActive (true);
						wideCursor.SetActive (false);
						break;
					default:
						break;
				}

				// When switching modes, deselect everything!
				Color c = new Color (255 / 255f, 116 / 255f, 26 / 255f, 255 / 255f);
				if(hoveredNode != null)
					hoveredNode.GetComponent<Renderer> ().material.SetColor ("_Color", c);
				hoveredNode = null;
				deselect = false;

				while (selectedNodes.Count > 0) {
					selectedNodes [0].GetComponent<Renderer> ().material.SetColor ("_Color", c);
					selectedNodes.RemoveAt (0);
					cursOnNode = false;
				}
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
			magnitude *= 300;
			/*
			if (camera.fieldOfView + magnitude > 180)
				camera.fieldOfView = 179;

			else if (camera.fieldOfView + magnitude < 0)
				camera.fieldOfView = 1;

			else
				camera.fieldOfView += magnitude;*/

			camera.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 
				camera.transform.position.z + magnitude);
			cursor.transform.position = new Vector3(cursor.transform.position.x, cursor.transform.position.y, 
				camera.transform.position.z + 10);

			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Zooming";
		}

		// RESET ZOOM
		else if (controller.Frame ().Hands.Count == 1 && rightHand.GrabStrength == 1.0 && rightHand.IsRight) {
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
					float maxNewZ = 1000;
					for (int n = 0; n < ForceDirected.nodeList.Count; n++) {
						GameObject node = ForceDirected.nodeList [n].gameObject;
						float maxOfXY = Mathf.Abs(node.transform.position.x) > Mathf.Abs(node.transform.position.y) ? Mathf.Abs(node.transform.position.x) : Mathf.Abs(node.transform.position.y);
						// float zDist = Mathf.Abs(node.transform.position.z - camera.transform.position.z);
						// positive max distance the camera needs to be away from this node's z value
						float zNew = (maxOfXY / (float) Math.Tan (65.0f * 3.1415926f / 360.0f));

						// get the absolute position for the camera
						zNew = node.transform.position.z - zNew;
						maxNewZ = maxNewZ < zNew ? maxNewZ : zNew;
					}
					camera.transform.position = new Vector3(0, 0, maxNewZ - 10);
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
	} // end Update()


	// Destroy the passed in node (unity gameObject)
	void DestroyNode(GameObject hoveredNode) {
		GameObject taggedText = GameObject.FindWithTag ("CurrAction");

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

				// delete the edges from the other involved nodes' lists too
				if (toDeleteNode.id == tDNEdge.source) {
					// delete from target
					readgml.NODE target = graph.nodes[tDNEdge.target];
					for(int j = 0; j < target.edgeList.Count; j++) {
						if (tDNEdge.source == target.edgeList [j].source &&
							tDNEdge.target == target.edgeList [j].target) {
							target.edgeList.RemoveAt (j);
							graph.nodes [tDNEdge.target] = target;
						}
					}
				}
				else {
					// delete from source
					readgml.NODE source = graph.nodes[tDNEdge.source];
					for(int j = 0; j < source.edgeList.Count; j++) {
						if (tDNEdge.source == source.edgeList [j].source &&
							tDNEdge.target == source.edgeList [j].target) {
							source.edgeList.RemoveAt (j);
							graph.nodes [tDNEdge.target] = source;
						}
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
			if (defaultFOV < 85) {			
				defaultFOV = defaultFOV + 5;
			}
			camera.fieldOfView = defaultFOV;
		}
	}

	void ShowConnect(GameObject hoveredNode) {
		GameObject taggedText = GameObject.FindWithTag ("CurrAction");

		if (showingConnections) {
			//change colors of edges
			hoveredNode.GetComponent<Renderer>().material.SetColor("_Color", Color.green);


			int dolphinID = 0; Int32.TryParse(hoveredNode.name, out dolphinID);
			readgml.NODE thisNode = graph.nodes[dolphinID];

			for (int j = 0; j < thisNode.edgeList.Count; j++) {
				// color both nodes of the edge
				Transform nodeT = ForceDirected.nodeList [thisNode.edgeList [j].source];
				nodeT.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", Color.green);

				nodeT = ForceDirected.nodeList [thisNode.edgeList [j].target];
				nodeT.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", Color.green);

				// color the edge
				Transform edgeT;
				// loop through the list of transforms of all edges
				for (int e = 0; e < ForceDirected.edgeList.Count; e++) {
					string[] nodeIDs = ForceDirected.edgeList [e].name.Split (',');
					int checkSource, checkTarget;
					Int32.TryParse (nodeIDs [0], out checkSource);
					Int32.TryParse (nodeIDs [1], out checkTarget);
					if (thisNode.edgeList [j].source == checkSource && thisNode.edgeList [j].target == checkTarget) {
						edgeT = ForceDirected.edgeList [e];
						edgeT.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", Color.red);
						break;
					}
				}
			}
			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Selected. Pausing Cursor";
		}
		else {
			int dolphinID = 0; Int32.TryParse(hoveredNode.name, out dolphinID);
			readgml.NODE thisNode = graph.nodes[dolphinID];
			Color edgeColor = new Color (2/255f, 101/255f, 3/255f, 255/255f);
			Color c = new Color (255/255f, 116/255f, 26/255f, 255/255f);

			for (int j = 0; j < thisNode.edgeList.Count; j++) {
				// color both nodes of the edge
				Transform nodeT = ForceDirected.nodeList [thisNode.edgeList [j].source];
				nodeT.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", c);

				nodeT = ForceDirected.nodeList [thisNode.edgeList [j].target];
				nodeT.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", c);

				// color the edge
				Transform edgeT;
				// loop through the list of transforms of all edges
				for (int e = 0; e < ForceDirected.edgeList.Count; e++) {
					string[] nodeIDs = ForceDirected.edgeList [e].name.Split (',');
					int checkSource, checkTarget;
					Int32.TryParse (nodeIDs [0], out checkSource);
					Int32.TryParse (nodeIDs [1], out checkTarget);
					if (thisNode.edgeList [j].source == checkSource && thisNode.edgeList [j].target == checkTarget) {
						edgeT = ForceDirected.edgeList [e];
						edgeT.gameObject.GetComponent<Renderer> ().material.SetColor ("_Color", edgeColor);
						break;
					}
				}
			}

			hoveredNode.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
			taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Using Cursor";
		}
	}








}