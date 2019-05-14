using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform t;
    public Rigidbody2D rgd;
    private const float speed = 100f;
    void Update()
    {
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            rgd.velocity = new Vector2(0, -speed) * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            rgd.velocity = new Vector2(0, speed) * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            rgd.velocity = new Vector2(-speed, 0) * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            rgd.velocity = new Vector2(speed, 0) * Time.deltaTime;
        }
        else
        {
            rgd.velocity = Vector2.zero;
        }
        rgd.angularVelocity = 0;
        rgd.rotation = 0;
    }
}
