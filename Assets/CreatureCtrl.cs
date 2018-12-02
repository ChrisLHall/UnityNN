using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureCtrl : MonoBehaviour {
    Rigidbody rb;

    // Use this for initialization
    void Awake () {
        rb = GetComponent<Rigidbody>();
    }
	
    // Update is called once per frame
    void Update () {
	if (Input.GetKey(KeyCode.UpArrow)) {
            rb.AddForce(transform.forward * 10f);
        }
        if (Input.GetKey(KeyCode.LeftArrow)) {
            rb.AddTorque(transform.up * -10f);
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            rb.AddTorque(transform.up * 10f);
        }
    }
}
