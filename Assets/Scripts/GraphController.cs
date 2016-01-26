using UnityEngine;
using Leap;

public class GraphController : MonoBehaviour
{
    Controller controller;

    Hand mainHand;

    Vector3 currPosition;
    Vector3 prevPosition;
    Vector3 handDelta;

    // called when the script instance is being loaded.
    void Awake()
    {
        controller = new Controller();
    }

    void Update()
    {
        mainHand = controller.Frame().Hands.Rightmost;

        currPosition = mainHand.PalmPosition.ToUnityScaled();

		if (controller.Frame().Hands.Count == 1 && mainHand.PinchStrength == 1.0)
        {
            handDelta = currPosition - prevPosition;
            handDelta *= 1000;

            transform.rotation = Quaternion.Euler(handDelta.y, -handDelta.x, 0) * transform.rotation;
        }

        prevPosition = currPosition;
    }
}