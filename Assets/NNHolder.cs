using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNHolder : MonoBehaviour {
    [HideInInspector]
    public int[] inputVector;
    [HideInInspector]
    public int[] outputVector;
    [HideInInspector]
    public NeuronCluster[] subNets;
    [HideInInspector]
    public NNConnection[] connections;
    
    public struct NNConnection {
        // idx -1 is the vectors
        // this is bidirectional
        public int srcNet;
        public int srcIdx;
        public int destNet;
        public int destIdx;
    }

    public int subnetNeurons;
    public int layers;
    public int baseConnNum;
    // base layer has baseConnNum connections from upper layers, and inputOutputConnNum going to the IO vectors
    public int inputOutputConnNum; 
    public int EmotionState { get; set; }

    public int InputOutputConnTotal { get { return (int)Mathf.Pow(2, layers - 1) * inputOutputConnNum; } }

    private void Start() {
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
    }

    public void DoTimestep() {
        // TODO figure out how emotion should work
        EmotionState = -1 + Mathf.FloorToInt(Random.value * 3f);
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
            subNets[i].FullTimestep(EmotionState);
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

        // then let the world simulate for a sec
    }
}
