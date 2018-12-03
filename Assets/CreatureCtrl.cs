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
                for (int y = 0; y < eyeCameraHeight; y++) {
                    Ray camCast = eyeCamera.ViewportPointToRay(new Vector3((x + .5f) / eyeCameraWidth, (y + .5f) / eyeCameraHeight, 1f));
                    RaycastHit rh;
                    bool hit = Physics.Raycast(camCast, out rh, 10f);
                    bool isRed = hit ? rh.transform.gameObject.GetComponent<Food>() != null : false;
                    brainTexture.SetPixel(x, y, isRed ? Color.red : Color.black);
                    brain.SetInput(y * 16 + x, isRed ? 16 : 0);
                }
            }
            brainTexture.Apply();
            // inputs are already up to date
            // now do timestep
            brain.DoTimestep();
        }
        if (Time.unscaledTime > lastRenderTime + .2f) {
            lastRenderTime = Time.unscaledTime;
            brain.PrintToTexture(DebugBrainViewer.inst.Texture);
            statusText.text = string.Format("Emotion (learning) state, press g or b to change: {0}. Time scale, press f or s to change: {1}", brain.EmotionState, Time.timeScale);
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
        bool fwd = brain.GetOutput(128) > 1;
        if (Input.GetKey(KeyCode.UpArrow)) {
            fwd = true;
            brain.SetInput(128, 16);
        } else {
            brain.SetInput(128, 0);
        }
        if (fwd) {
            rb.AddForce(transform.forward * 10f);
        }
        
        bool left = brain.GetOutput(129) > 1;
        if (Input.GetKey(KeyCode.LeftArrow)) {
            left = true;
            brain.SetInput(129, 16);
        } else {
            brain.SetInput(129, 0);
        }
        if (left) {
            rb.AddTorque(transform.up * -10f);
        }

        bool right = brain.GetOutput(130) > 1;
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
        if (null != food) {
            brain.SetGoodEmotion();
            food.Respawn();
        }
    }
}
