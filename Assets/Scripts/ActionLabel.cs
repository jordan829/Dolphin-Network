/*using UnityEngine;
using System.Collections;
using Leap;

public class ActionLabel : MonoBehaviour {

	Controller controller;
	Hand rightHand;
	Hand leftHand;

	Finger indexFinger;
	Finger thumbFinger;
	Vector3 pinchDistance;
	float tipDistance;

	FingerList rhExtended;

	// Use this for initialization
	void Start () {

	}

	void Awake() {
		controller = new Controller ();
	}
	
	// Update is called once per frame
	void Update () {
		rightHand = controller.Frame ().Hands.Rightmost;
		leftHand = controller.Frame ().Hands.Leftmost;
		rhExtended = rightHand.Fingers.Extended ();

		indexFinger = controller.Frame().Hands.Rightmost.Fingers [(int)Finger.FingerType.TYPE_INDEX];
		thumbFinger = controller.Frame().Hands.Rightmost.Fingers [(int)Finger.FingerType.TYPE_THUMB];
		pinchDistance = (indexFinger.TipPosition - thumbFinger.TipPosition).ToUnityScaled();
		tipDistance = pinchDistance.magnitude;


		GameObject taggedText = GameObject.FindWithTag ("CurrAction");
		if(taggedText != null) {
			if (rhExtended.Count == 1) {
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Pointing";
			}
			else if (controller.Frame ().Hands.Count == 2 && (rightHand.PinchStrength == 1.0 && leftHand.PinchStrength == 1.0)) {
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Zooming";
			} 
			else if (controller.Frame ().Hands.Count == 1 && tipDistance < 0.017 && rightHand.PinchStrength == 1.0 && rightHand.GrabStrength != 1.0) {
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Rotating";
			} 
			else if (controller.Frame ().Hands.Count == 1 && rightHand.GrabStrength == 1.0) {
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Resetting view... Hold for 3 seconds";
			}
			else {
				taggedText.GetComponent<UnityEngine.UI.Text> ().text = "Standing By";
			}
		}
	}
}*/
