using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour {
    Vector3 startPos;
    float spawnedTime;
    Rigidbody rb;
    
    void Start () {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        spawnedTime = Time.time;
    }

    public bool CanPickup { get { return Time.time - spawnedTime > .5f; } }
    
    public void Respawn () {
        transform.position = startPos;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        spawnedTime = Time.time;
    }

    private void Update() {
        if (transform.position.y < -100) {
            Respawn();
        }
    }
}
