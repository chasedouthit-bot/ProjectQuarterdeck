using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCase : MonoBehaviour {

    [SerializeField]
    GameObject ObjectToRotate;
    [SerializeField]
    float RotationRate = 3f;
    public bool Rotate = false;

   

	// Update is called once per frame
	void Update () {
		if (ObjectToRotate != null && Rotate)
        {
            ObjectToRotate.transform.Rotate(ObjectToRotate.transform.up, RotationRate * Time.deltaTime);
        }
	}
}
