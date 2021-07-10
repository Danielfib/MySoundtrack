using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCamLook : MonoBehaviour {

    [SerializeField]
    public float sensitivity = 5.0f, smoothing = 2.0f;
    public GameObject character;
    private Vector2 mouseLook, smoothVector;

	void Start () {
        character = this.transform.parent.gameObject;
	}
	
	void Update () {
        var md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothVector.x = Mathf.Lerp(smoothVector.x, md.x, 1f / smoothing);
        smoothVector.y = Mathf.Lerp(smoothVector.y, md.y, 1f / smoothing);
        mouseLook += smoothVector;

        transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
        character.transform.localRotation = Quaternion.AngleAxis(mouseLook.x, character.transform.up);
    }
}