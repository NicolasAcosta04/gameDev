using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{

    public Vector2 moveValue;
    public float speed;
    private Vector3 lastPos;
    private int count;
    private int numPickUps = 6;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI playerPosText;
    public TextMeshProUGUI playerVelocityText;


    void Start() { 
        count = 0;
        winText.text = "";
        lastPos = GameObject.FindGameObjectsWithTag("Player")[0].transform.position;
        SetCountText ();
    }

    void OnMove(InputValue value)
    {
        moveValue = value.Get<Vector2>();
    }

    void FixedUpdate()
    {

        Vector3 movement = new Vector3(moveValue.x, 0.0f, moveValue.y);

        GetComponent<Rigidbody>().AddForce(movement * speed * Time.fixedDeltaTime);

        var currentPos = GameObject.FindGameObjectsWithTag("Player")[0].transform.position;
        playerPosText.text = currentPos.ToString("0.00");
        var velocity = (currentPos - lastPos) / Time.fixedDeltaTime;
        playerVelocityText.text = velocity.magnitude.ToString("0.00") + "m/s";

        lastPos = currentPos;
    }

    void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "PickUp") {
            other.gameObject.SetActive(false);
            count++;
            SetCountText();
        }
    }

    private void SetCountText() {
        scoreText.text = "Score: " + count.ToString();
        if (count >= numPickUps) {
            winText.text = "You win!";
        }
    }
}
