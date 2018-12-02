using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugBrainViewer : MonoBehaviour {
    public static DebugBrainViewer inst;
    RawImage image;
    Texture2D myTexture;
    void Awake() {
        inst = this;
        image = GetComponent<RawImage>();
    }

    public void SetupTexture(int width, int height) {
        myTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        myTexture.filterMode = FilterMode.Point;
        image.texture = myTexture;
    }

    public Texture2D Texture { get { return myTexture; } }
}
