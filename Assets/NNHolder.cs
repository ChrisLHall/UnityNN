using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNHolder : MonoBehaviour {
    public struct NNConnection {
        // idx -1 is the vectors
        // this is bidirectional
        public int srcNet;
        public int srcIdx;
        public int destNet;
        public int destIdx;

        public void ToIntList(List<int> result) {
            result.Add(srcNet);
            result.Add(srcIdx);
            result.Add(destNet);
            result.Add(destIdx);
        }

        public static NNConnection FromIntList(IEnumerator<int> list) {
            NNConnection result = new NNConnection();
            result.srcNet = list.Current;
            list.MoveNext();
            result.srcIdx = list.Current;
            list.MoveNext();
            result.destNet = list.Current;
            list.MoveNext();
            result.destIdx = list.Current;
            list.MoveNext();
            return result;
        }
    }

    public int subnetNeurons;
    public int layers;
    public int baseConnNum;
    // base layer has baseConnNum connections from upper layers, and inputOutputConnNum going to the IO vectors
    public int inputOutputConnNum; 
    public int EmotionState { get; set; }
    const int EMOTION_MAGNITUDE_CAP = 16;
    const int EMOTION_MAGNITUDE_THRESH = 4;

    [HideInInspector]
    public int[] inputVector;
    [HideInInspector]
    public int[] outputVector;
    [HideInInspector]
    public NeuronCluster[] subNets;
    [HideInInspector]
    public NNConnection[] connections;

    public int InputOutputConnTotal { get { return (int)Mathf.Pow(2, layers - 1) * inputOutputConnNum; } }

    private void Start() {
        Init();
    }

    private void Init() {
        inputVector = new int[InputOutputConnTotal];
        outputVector = new int[InputOutputConnTotal];
        subNets = new NeuronCluster[(int)Mathf.Pow(2, layers) - 1];
        List<NNConnection> listConnections = new List<NNConnection>();
        for (int layer = 0; layer < layers; layer++) {
            int startIdx = (int)Mathf.Pow(2, layer) - 1;
            int endIdx = (int)Mathf.Pow(2, layer + 1) - 1;
            for (int i = startIdx; i < endIdx; i++) {
                int exposed = 3 * baseConnNum;
                if (layer == 0) {
                    exposed = 2 * baseConnNum;
                } else if (layer == layers - 1) {
                    exposed = inputOutputConnNum + baseConnNum;
                }
                subNets[i] = new NeuronCluster(subnetNeurons, exposed);
                // set up connections. Connect first 2 * base to the lower layer and last 1 * base to the higher layer
                // top layer only has the 2 * base to the lower layer
                // the index of the net in the next layer is the index of this one times two, and times two plus one
                int idxInLayer = i - startIdx;
                if (layer < layers - 1) {
                    int destNetIdx = endIdx + 2 * idxInLayer;
                    int destConnStart = 2 * baseConnNum;
                    if (layer == layers - 2) {
                        // the dest conns on the last layer hook up differently
                        destConnStart = inputOutputConnNum;
                    }
                    for (int c = 0; c < baseConnNum; c++) {
                        listConnections.Add(new NNConnection() {
                            srcNet = i,
                            destNet = destNetIdx,
                            srcIdx = c,
                            destIdx = destConnStart + c,
                        });
                    }
                    for (int c = 0; c < baseConnNum; c++) {
                        listConnections.Add(new NNConnection() {
                            srcNet = i,
                            destNet = destNetIdx + 1,
                            srcIdx = baseConnNum + c,
                            destIdx = destConnStart + c,
                        });
                    }
                } else {
                    // connect to inputs
                    for (int c = 0; c < inputOutputConnNum; c++) {
                        listConnections.Add(new NNConnection() {
                            srcNet = i,
                            destNet = -1, // the IO connections
                            srcIdx = c,
                            destIdx = idxInLayer * inputOutputConnNum + c,
                        });
                    }
                }
            }
        }
        connections = listConnections.ToArray();

        DebugBrainViewer.inst.SetupTexture(DebugTextureWidthPixels, DebugTextureHeightPixels);
    }

    public void DoTimestep() {
        if (EmotionState > 0) {
            EmotionState--;
        } else if (EmotionState < 0) {
            EmotionState++;
        }

        int finalEmotionState = (EmotionState > EMOTION_MAGNITUDE_THRESH ? 1 : (EmotionState < -EMOTION_MAGNITUDE_THRESH ? -1 : 0));
        // First, copy inputs / connection values into all subnets
        for (int i = 0; i < connections.Length; i++) {
            NNConnection conn = connections[i];
            NeuronCluster srcNet = subNets[conn.srcNet];
            // destNet can be -1, which means access the IO ports
            if (conn.destNet >= 0) {
                NeuronCluster destNet = subNets[conn.destNet];
                srcNet.SetExternalInput(conn.srcIdx, destNet.GetExternalOutput(conn.destIdx));
                destNet.SetExternalInput(conn.destIdx, srcNet.GetExternalOutput(conn.srcIdx));
            } else {
                srcNet.SetExternalInput(conn.srcIdx, inputVector[conn.destIdx]);
                // copy outputs after processing
                // outputVector[conn.destIdx] = srcNet.GetExternalOutput(conn.srcIdx);
            }
        }
        // Second, subnets process based on emotional state
        for (int i = 0; i < subNets.Length; i++) {
            subNets[i].FullTimestep(finalEmotionState);
        }
        // Third, set outputs
        for (int i = 0; i < connections.Length; i++) {
            NNConnection conn = connections[i];
            // destNet must be -1
            if (conn.destNet != -1) {
                continue;
            }
            NeuronCluster srcNet = subNets[conn.srcNet];
            outputVector[conn.destIdx] = srcNet.GetExternalOutput(conn.srcIdx);
        }
    }

    public void SetInput(int idx, int value) {
        if (idx >= InputOutputConnTotal) {
            Debug.LogError(string.Format("Cannot set NNHolder IO idx {0} of {1}", idx, InputOutputConnTotal));
            return;
        }
        inputVector[idx] = value;
    }

    public int GetOutput(int idx) {
        if (idx >= InputOutputConnTotal) {
            Debug.LogError(string.Format("Cannot get NNHolder IO idx {0} of {1}", idx, InputOutputConnTotal));
            return 0;
        }
        return outputVector[idx];
    }

    public void SetGoodEmotion() {
        EmotionState = Mathf.Clamp(EmotionState + EMOTION_MAGNITUDE_CAP, -EMOTION_MAGNITUDE_CAP, EMOTION_MAGNITUDE_CAP);
    }
    public void SetBadEmotion() {
        EmotionState = Mathf.Clamp(EmotionState - EMOTION_MAGNITUDE_CAP, -EMOTION_MAGNITUDE_CAP, EMOTION_MAGNITUDE_CAP);
    }

    public int DebugTextureWidthTiles { get { return Mathf.CeilToInt(Mathf.Pow(2, layers / 2f)); } }
    public int DebugTextureWidthPixels { get { return DebugTextureWidthTiles * subnetNeurons; } }
    public int DebugTextureHeightPixels {  get { return DebugTextureWidthTiles * (subnetNeurons + 3)
            + 2 * Mathf.CeilToInt((float)InputOutputConnTotal / DebugTextureWidthPixels); } }

    public void PrintToTexture(Texture2D tex) {
        for (int i = 0; i < subNets.Length; i++) {
            int tileX = i % DebugTextureWidthTiles;
            int tileY = Mathf.FloorToInt((float)i / DebugTextureWidthTiles);
            subNets[i].PrintToTexture(tex, tileX * subnetNeurons, tileY * (subnetNeurons + 3));
        }
        int rowsForIO = Mathf.CeilToInt((float)InputOutputConnTotal / DebugTextureWidthPixels);
        for (int i = 0; i < inputVector.Length; i++) {
            tex.SetPixel(i % DebugTextureWidthPixels, DebugTextureWidthPixels + Mathf.FloorToInt((float)i / DebugTextureWidthPixels), Color.magenta / 16f * inputVector[i]);
            tex.SetPixel(i % DebugTextureWidthPixels, rowsForIO + DebugTextureWidthPixels + Mathf.FloorToInt((float)i / DebugTextureWidthPixels), Color.blue / 16f * outputVector[i]);
        }
        tex.Apply();
    }

    public void ToIntList(List<int> result) {
        result.Add(subnetNeurons);
        result.Add(layers);
        result.Add(baseConnNum);
        result.Add(inputOutputConnNum);
        result.Add(EmotionState);
        result.Add(subNets.Length);
        for (int i = 0; i < subNets.Length; i++) {
            subNets[i].ToIntList(result);
        }
        result.Add(connections.Length);
        for (int i = 0; i < connections.Length; i++) {
            connections[i].ToIntList(result);
        }
    }

    public void FromIntList(IEnumerator<int> list) {
        subnetNeurons = list.Current;
        list.MoveNext();
        layers = list.Current;
        list.MoveNext();
        baseConnNum = list.Current;
        list.MoveNext();
        inputOutputConnNum = list.Current;
        list.MoveNext();
        EmotionState = list.Current;
        list.MoveNext();

        Init();

        subNets = new NeuronCluster[list.Current];
        list.MoveNext();
        for (int i = 0; i < subNets.Length; i++) {
            subNets[i] = NeuronCluster.FromIntList(list);
        }
        connections = new NNConnection[list.Current];
        list.MoveNext();
        for (int i = 0; i < connections.Length; i++) {
            connections[i] = NNConnection.FromIntList(list);
        }
    }
}
