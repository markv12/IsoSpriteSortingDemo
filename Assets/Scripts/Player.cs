using UnityEngine;

public class Player : MonoBehaviour {
    public Transform t;
    public Rigidbody2D rgd;
    public float speed = 5f;

    protected Vector2 _input;

    void Update() {
        _input = Vector2.zero;

        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
            _input.y = -speed;
        } else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
            _input.y = speed;
        }

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
            _input.x = -speed;
        } else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
            _input.x = speed;
        }
    }

    void FixedUpdate() {
        rgd.MovePosition(transform.position + (Vector3)_input * Time.deltaTime);
    }
}
