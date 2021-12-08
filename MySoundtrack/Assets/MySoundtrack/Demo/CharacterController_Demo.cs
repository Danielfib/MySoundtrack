using UnityEngine;

public class CharacterController_Demo : MonoBehaviour
{
    public float moveSpeed = 10.0f;
    private float frontMovement;
    private float sideMovement;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        frontMovement = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        sideMovement = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        transform.Translate(sideMovement, 0, frontMovement);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}