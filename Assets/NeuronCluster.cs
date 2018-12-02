using System.Collections.Generic;
using UnityEngine;

public class NeuronCluster {
    int numNeurons;
    int numExposed;
    public const int CONNECTION_MAGNITUDE_CAP = 16;
    public const int CONNECTION_MAGNITUDE_THRESH = 4;
    public const int ACTIVATION_MAGNITUDE_CAP = 16;
    public const int ACTIVATION_MAGNITUDE_THRESH = 8;
    public const int SUM_THRESHOLD = 4;
    int[] externalInputs;
    int[] externalOutputs;
    int[] activations;
    int[] sumTotals;
    int[] nextTimestepActivations;
    int[][] connections;

    public NeuronCluster(int numNeurons, int numExposed) {
        this.numNeurons = numNeurons;
        this.numExposed = numExposed;
        externalInputs = new int[numExposed];
        externalOutputs = new int[numExposed];
        activations = new int[numNeurons];
        sumTotals = new int[numNeurons];
        nextTimestepActivations = new int[numNeurons];
        connections = new int[numNeurons][];
        for (int i = 0; i < connections.Length; i++) {
            connections[i] = new int[numNeurons];
            for (int j = 0; j < numNeurons; j++) {
                // fully randomize connections
                connections[i][j] = -CONNECTION_MAGNITUDE_CAP + Mathf.FloorToInt(Random.value * (2 * CONNECTION_MAGNITUDE_CAP + 1));
            }
        }
    }

    public void SetExternalInput(int idx, int input) {
        if (idx >= externalInputs.Length) {
            Debug.LogError(string.Format("Tried to set external input {0} of only {1}", idx, externalInputs.Length));
            return;
        }
        externalInputs[idx] = input;
    }

    public int GetExternalOutput(int idx) {
        if (idx >= externalOutputs.Length) {
            Debug.LogError(string.Format("Tried to get external output {0} of only {1}", idx, externalOutputs.Length));
            return 0;
        }
        return externalOutputs[idx];
    }

    public void FullTimestep(int finalEmotionalState) {
        // assume inputs are already setup
        ClearActivationTotals();
        SumInternalActivations();
        Learn(finalEmotionalState);
        CommitActivationSums();
        ApplyNextTimestepActivations();
        UpdateExternalOutputs();
    }

    void ClearActivationTotals() {
        for (int i = 0; i < numNeurons; i++) {
            sumTotals[i] = 0;
        }
    }

    void SumInternalActivations() {
        for (int srcIdx = 0; srcIdx < numNeurons; srcIdx++) {
            int[] setOfConnections = connections[srcIdx];
            int srcActivation = activations[srcIdx];
            if (srcIdx < numExposed) {
                srcActivation = externalInputs[srcIdx];
            }
            for (int destIdx = 0; destIdx < numNeurons; destIdx++) {
                int connection = setOfConnections[destIdx];
                int connectionMult = (connection > CONNECTION_MAGNITUDE_THRESH ? 1 : (connection < -CONNECTION_MAGNITUDE_THRESH ? -1 : 0));
                int add = (srcActivation > ACTIVATION_MAGNITUDE_THRESH ? 1 : (srcActivation < -ACTIVATION_MAGNITUDE_THRESH ? -1 : 0));
                sumTotals[destIdx] += add * connectionMult;
            }
        }
    }

    /*
    public void AddToExposedActivationSum(int idx, int srcActivation) {
        if (idx >= numExposed) {
            Debug.LogError("Tried to externally change non-exposed neuron " + idx);
            return;
        }
        int add = (srcActivation > ACTIVATION_MAGNITUDE_THRESH ? 1 : (srcActivation < -ACTIVATION_MAGNITUDE_THRESH ? -1 : 0));
        inputTotals[idx] += add;
    }
    */

    void UpdateExternalOutputs() {
        for (int i = 0; i < numExposed; i++) {
            externalOutputs[i] = activations[i];
        }
    }
    
    void CommitActivationSums() {
        for (int idx = 0; idx < numNeurons; idx++) {
            int add = sumTotals[idx] > SUM_THRESHOLD ? 1 : -1;
            nextTimestepActivations[idx] = Mathf.Clamp(nextTimestepActivations[idx] + add, 0, ACTIVATION_MAGNITUDE_CAP);
        }
    }

    // TODO ACTUALLY DO THE LEARNING BY CHANGING THE CONNECTION STRENGTHS

    /* DESIGN FOR HOW THE LEARNING CALCULATION WORKS */
    /*
     * should there be negative activation?
     * hmm
     * i dont think so?
     * how would a negative weight even happen
     * well so you can get positive or negative reactions
     * but activations are only positive? yes
     * i will write: src dest "emotion" --> weight change. +, -, 0
     * + + + -> +
     * 0 + + -> 0
     * + 0 + -> 0
     * 0 0 + -> 0
     * + + - -> -
     * 0 + - -> 0
     * + 0 - -> 0
     * 0 0 - -> 0
     * This causes things to never passively decay. Maybe needs some randomness for that
     */
     
    // finalEmotionState should be -1, +1, or 0
    void Learn(int finalEmotionState) {
        for (var src = 0; src < numNeurons; src++) {
            bool srcActive = activations[src] > ACTIVATION_MAGNITUDE_THRESH;
            for (var dest = 0; dest < numNeurons; dest++) {
                // skip learning for external ones? maybe keep their weights always 0.
                // its still a bit unclear how those work. There need to be separate input and output ones..?
                // TODO how to apply randomization? randomize weights, or sometimes have random outputs?
                bool destPastThreshold = sumTotals[dest] > SUM_THRESHOLD;
                // if src implies dest, change the weighting
                if (srcActive && destPastThreshold) {
                    connections[src][dest] = Mathf.Clamp(connections[src][dest] + finalEmotionState,
                            -CONNECTION_MAGNITUDE_CAP, CONNECTION_MAGNITUDE_CAP); // LEARN
                }
            }
        }
    }
   
    // TODO CONNECT WITH OTHER MODULES

    void ApplyNextTimestepActivations() {
        int[] buffer = activations;
        activations = nextTimestepActivations;
        nextTimestepActivations = buffer;
        // can clear out next timestep activations, but no need
    }

    public List<int> ToIntList() {
        List<int> result = new List<int>();
        result.Add(numNeurons);
        result.Add(numExposed);
        for (int i = 0; i < externalInputs.Length; i++) {
            result.Add(externalInputs[i]);
        }
        for (int i = 0; i < externalOutputs.Length; i++) {
            result.Add(externalInputs[i]);
        }
        for (int i = 0; i < numNeurons; i++) {
            result.Add(activations[i]);
        }
        for (int i = 0; i < numNeurons; i++) {
            for (int j = 0; j < numNeurons; j++) {
                result.Add(connections[i][j]);
            }
        }
        return result;
    }

    public void FromIntList(List<int> list) {
        // TODO
    }
}
