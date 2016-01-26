using UnityEngine;
using System.Collections;
using Leap;

public class CameraController : MonoBehaviour {

	public Camera camera;

	Controller controller;

	Hand rightHand;
	Hand leftHand;

	Vector3 currMainPosition;
	Vector3 currOffPosition;
	Vector3 prevMainPosition;
	Vector3 prevOffPosition;

	//Vector3 handMainDelta;
	//Vector3 handOffDelta;
	Vector3 distDelta;

	Vector3 currDistance;
	Vector3 prevDistance;

	float magnitude;

	// called when the script instance is being loaded.
	void Awake()
	{
		controller = new Controller();
	}

	void Update()
	{
		rightHand = controller.Frame().Hands.Rightmost;
		leftHand = controller.Frame().Hands.Leftmost;

		currMainPosition = rightHand.PalmPosition.ToUnityScaled();
		currOffPosition = leftHand.PalmPosition.ToUnityScaled();

		currDistance = currMainPosition - currOffPosition;

		if (controller.Frame().Hands.Count == 2 && (rightHand.PinchStrength == 1.0 && leftHand.PinchStrength == 1.0))
		{
			
			magnitude = currDistance.magnitude - prevDistance.magnitude;

			//magnitude = distDelta.magnitude;

			magnitude *= -600;


			if (camera.fieldOfView + magnitude > 180)
				camera.fieldOfView = 179;

			else if (camera.fieldOfView + magnitude < 0)
				camera.fieldOfView = 1;

			else
				camera.fieldOfView += magnitude;

			//distDelta *= 1000;

			//transform.rotation = Quaternion.Euler(handDelta.y, -handDelta.x, 0) * transform.rotation;
		}

		prevDistance = currDistance;
	}
}
