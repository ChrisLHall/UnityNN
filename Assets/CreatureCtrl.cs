using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreatureCtrl : MonoBehaviour {
    Rigidbody rb;
    NNHolder brain;
    Camera eyeCamera;
    public RenderTexture visionTexture;
    public Texture2D brainTexture;
    public Text statusText;
    public int eyeCameraWidth;
    public int eyeCameraHeight;
    int totalFood;

    // Use this for initialization
    void Awake () {
        rb = GetComponent<Rigidbody>();
        brain = GetComponent<NNHolder>();
        eyeCamera = GetComponentInChildren<Camera>();
    }


    float lastThinkTime;
    float lastRenderTime;
    void Update () {
        if (Time.time > lastThinkTime + .1f) {
            lastThinkTime = Time.time;
            // copy vision into brain texture
            //RenderTexture.active = visionTexture;
            //brainTexture.ReadPixels(new Rect(0, 0, visionTexture.width, visionTexture.height), 0, 0);
            //brainTexture.Apply();
            for (int x = 0; x < eyeCameraWidth; x++) {
                Ray camCast = eyeCamera.ViewportPointToRay(new Vector3((x + .5f) / eyeCameraWidth, .5f, .5f));
                Debug.DrawRay(camCast.origin, camCast.direction, Color.green, .1f);
                RaycastHit rh;

                bool hit = Physics.Raycast(camCast, out rh, 10f);
                bool hitFood = hit ? rh.transform.gameObject.GetComponent<Food>() != null : false;
                brain.SetInput(x, hitFood ? NeuronCluster.ACTIVATION_TIMER - 1 : 0);
                brainTexture.SetPixel(x, 0, hitFood ? Color.red : Color.black);

                hit = Physics.Raycast(camCast, out rh, 1f);
                bool hitWall = hit ? rh.transform.gameObject.GetComponent<Food>() == null : false;
                brain.SetInput(eyeCameraWidth + x, hitWall ? NeuronCluster.ACTIVATION_TIMER - 1 : 0);
                brainTexture.SetPixel(x, 1, hitWall ? Color.gray : Color.black);
            }
            brainTexture.Apply();
            // inputs are already up to date
            // now do timestep
            brain.DoTimestep();
        }
        if (Time.unscaledTime > lastRenderTime + .2f) {
            lastRenderTime = Time.unscaledTime;
            brain.PrintToTexture(DebugBrainViewer.inst.Texture);
            statusText.text = string.Format("Emotion (learning) state, press g or b to change: {0}. Time scale, press f or s to change: {1}. Food collected: {2}", brain.EmotionState, Time.timeScale, totalFood);
        }

        if (Input.GetKeyDown(KeyCode.G)) {
            brain.SetGoodEmotion();
        }
        if (Input.GetKeyDown(KeyCode.B)) {
            brain.SetBadEmotion();
        }
        if (Input.GetKeyDown(KeyCode.F)) {
            Time.timeScale += .2f;
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            Time.timeScale -= .2f;
        }
    }

    private void FixedUpdate() {
        // constantly set inputs
        bool fwd = brain.GetOutput(64) > 0;
        if (Input.GetKey(KeyCode.UpArrow)) {
            fwd = true;
            brain.SetInput(64, NeuronCluster.ACTIVATION_TIMER - 1);
        } else {
            brain.SetInput(64, 0);
        }
        if (fwd) {
            rb.MovePosition(transform.position + transform.forward * 1f * Time.fixedDeltaTime);
        }

        bool back = brain.GetOutput(67) > 0;
        if (Input.GetKey(KeyCode.DownArrow)) {
            back = true;
            brain.SetInput(67, NeuronCluster.ACTIVATION_TIMER - 1);
        } else {
            brain.SetInput(67, 0);
        }
        if (back) {
            rb.MovePosition(transform.position + transform.forward * -.8f * Time.fixedDeltaTime);
        }

        bool left = brain.GetOutput(65) > 0;
        if (Input.GetKey(KeyCode.LeftArrow)) {
            left = true;
            brain.SetInput(65, NeuronCluster.ACTIVATION_TIMER - 1);
        } else {
            brain.SetInput(65, 0);
        }
        if (left) {
            rb.MoveRotation(Quaternion.AngleAxis(-3f, transform.up) * transform.rotation);
        }

        bool right = brain.GetOutput(66) > 0;
        if (Input.GetKey(KeyCode.RightArrow)) {
            right = true;
            brain.SetInput(66, NeuronCluster.ACTIVATION_TIMER - 1);
        } else {
            brain.SetInput(66, 0);
        }
        if (right) {
            rb.MoveRotation(Quaternion.AngleAxis(3f, transform.up) * transform.rotation);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        var food = collision.gameObject.GetComponent<Food>();
        if (null != food) {
            if (food.isBadFood) {
                brain.SetBadEmotion();
            } else {
                totalFood++;
                brain.SetGoodEmotion();
            }
            food.Respawn();
        } else {
            // Be sad when hitting the wall i guess, who fuckin knows at this point
            brain.SetBadEmotion();
        }
    }
}
