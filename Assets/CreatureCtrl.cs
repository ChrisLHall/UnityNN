using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureCtrl : MonoBehaviour {
    Rigidbody rb;
    NNHolder brain;
    public RenderTexture visionTexture;
    public Texture2D brainTexture;

    // Use this for initialization
    void Awake () {
        rb = GetComponent<Rigidbody>();
        brain = GetComponent<NNHolder>();
    }


    float lastThinkTime;
    void Update () {
        if (Time.time > lastThinkTime + .1f) {
            lastThinkTime = Time.time;
            // copy vision into brain texture
            RenderTexture.active = visionTexture;
            brainTexture.ReadPixels(new Rect(0, 0, visionTexture.width, visionTexture.height), 0, 0);
            brainTexture.Apply();
            for (int x = 0; x < 16; x++) {
                for (int y = 0; y < 8; y++) {
                    bool isRed = brainTexture.GetPixel(x, y).r > .4;
                    brainTexture.SetPixel(x, y, isRed ? Color.red : Color.black);
                    brain.SetInput(y * 16 + x, isRed ? 16 : 0);
                }
            }
            brainTexture.Apply();
            // inputs are already up to date
            // now do timestep
            brain.DoTimestep();
        }

        // constantly set inputs
        bool fwd = brain.GetOutput(128) > 4;
	if (Input.GetKey(KeyCode.UpArrow)) {
            fwd = true;
            brain.SetInput(128, 16);
        } else {
            brain.SetInput(128, 0);
        }
        if (fwd) {
            rb.AddForce(transform.forward * 10f);
        }

        bool left = brain.GetOutput(129) > 4;
        if (Input.GetKey(KeyCode.LeftArrow)) {
            left = true;
            brain.SetInput(129, 16);
        } else {
            brain.SetInput(129, 0);
        }
        if (left) {
            rb.AddTorque(transform.up * -10f);
        }

        bool right = brain.GetOutput(130) > 4;
        if (Input.GetKey(KeyCode.RightArrow)) {
            right = true;
            brain.SetInput(130, 16);
        } else {
            brain.SetInput(130, 0);
        }
        if (right) {
            rb.AddTorque(transform.up * 10f);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        var food = collision.gameObject.GetComponent<Food>();
        Debug.Log("Hit " + collision.gameObject.name);
        if (null != food) {
            brain.SetGoodEmotion();
            food.Respawn();
        }
    }
}
