using UnityEngine;

public class NeuronCluster {
    int numNeurons;
    int numExposed;
    public const int CONNECTION_MAGNITUDE_CAP = 16;
    public const int ACTIVATION_MAGNITUDE_CAP = 16;
    public const int ACTIVATION_MAGNITUDE_THRESH = 8;
    public const int SUM_THRESHOLD = 4;
    int[] activations;
    int[] activationTotals;
    int[] nextTimestepActivations;
    int[][] connections;

    // ASSIGN NEURONS TO INCOMING AND OUTGOING. try figuring out how to make them the SAME???

    // drives reinforcement positive or negative of connections, basically learning rate
    public int externalEmotionalState;
    public const int EMOTION_ACTIVATION_THRESHOLD = 4;

    public NeuronCluster(int numNeurons, int numExposed) {
        this.numNeurons = numNeurons;
        this.numExposed = numExposed;
        activations = new int[numNeurons];
        activationTotals = new int[numNeurons];
        nextTimestepActivations = new int[numNeurons];
        connections = new int[numNeurons][];
        for (int i = 0; i < connections.Length; i++) {
            connections[i] = new int[numNeurons];
        }
    }

    public void ClearActivationTotals() {
        for (int i = 0; i < numNeurons; i++) {
            activationTotals[i] = 0;
        }
    }

    public void AddInternalActivations() {
        for (int srcIdx = 0; srcIdx < numNeurons; srcIdx++) {
            int[] setOfConnections = connections[srcIdx];
            for (int destIdx = 0; destIdx < numNeurons; destIdx++) {
                int srcActivation = activations[srcIdx];
                int add = (srcActivation > ACTIVATION_MAGNITUDE_THRESH ? 1 : (srcActivation < -ACTIVATION_MAGNITUDE_THRESH ? -1 : 0));
                activationTotals[destIdx] += add;
            }
        }
    }

    public void AddToExposedActivationSum(int idx, int srcActivation) {
        if (idx >= numExposed) {
            Debug.LogError("Tried to externally change non-exposed neuron " + idx);
            return;
        }
        int add = (srcActivation > ACTIVATION_MAGNITUDE_THRESH ? 1 : (srcActivation < -ACTIVATION_MAGNITUDE_THRESH ? -1 : 0));
        activationTotals[idx] += add;
    }

    public int GetExposedActivation(int idx) {
        if (idx >= numExposed) {
            Debug.LogError("Tried to externally read non-exposed neuron " + idx);
            return 0;
        }
        return activations[idx];
    }

    public void CommitActivationSums() {
        for (int idx = 0; idx < numNeurons; idx++) {
            int add = activationTotals[idx] > SUM_THRESHOLD ? 1 : -1;
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
     */

    /* DESIGN FOR HOW ACTIVATION WORKS */
    /*
     * uh. so if a weight has its SECOND TOP BIT SET, it counts for +1 or -1,
     * depending on whether the TOP BIT is set (positive or negative)
     * if the sum is more or less than SUM_ACTIVATION_MAGNITUDE, then the activation is 
     * increased or decreased.
     */

    // TODO CONNECT WITH OTHER MODULES

    public void ApplyNextTimestepActivations() {
        int[] buffer = activations;
        activations = nextTimestepActivations;
        nextTimestepActivations = buffer;
        // can clear out next timestep activations, but no need
    }
}
