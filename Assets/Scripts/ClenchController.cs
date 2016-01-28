/*using UnityEngine;
using System.Collections;
using Leap;

public class ClenchController : MonoBehaviour {

	public Camera camera;

	Controller controller;

	Hand mainHand;

	Vector3 currPosition;
	Vector3 prevPosition;
	Vector3 handDelta;
	bool startAction;
	float lastTime;
	float timeDiff;

	// called when the script instance is being loaded.
	void Awake()
	{
		controller = new Controller();
		startAction = false;
	}

	void Update()
	{
		mainHand = controller.Frame ().Hands.Rightmost;

		currPosition = mainHand.PalmPosition.ToUnityScaled ();

		if (controller.Frame ().Hands.Count == 1 && mainHand.GrabStrength == 1.0) {
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
					camera.fieldOfView = 60;
				}
			}
		}
		else {
			startAction = false;

		}

		prevPosition = currPosition;
	}
}
*/