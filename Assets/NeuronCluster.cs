using System.Collections.Generic;
using UnityEngine;

public class NeuronCluster {
    int numNeurons;
    int numExposed;
    public const int CONNECTION_MAGNITUDE_CAP = 64;
    public const int CONNECTION_MAGNITUDE_THRESH = 8;
    public const int ACTIVATION_TIMER = 32;
    public const int COOLDOWN_TIMER = 64;
    public const int SUM_THRESHOLD = 3;
    int[] externalInputs;
    int[] externalOutputs;
    int[] activations;
    int[] sumTotals;
    int[] cooldowns;
    int[][] connections;

    public NeuronCluster(int numNeurons, int numExposed) {
        this.numNeurons = numNeurons;
        this.numExposed = numExposed;
        externalInputs = new int[numExposed];
        externalOutputs = new int[numExposed];
        activations = new int[numNeurons];
        sumTotals = new int[numNeurons];
        cooldowns = new int[numNeurons];
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
        CommitActivationSums();
        Learn(finalEmotionalState);
        EnforceConnectionRules();
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
                srcActivation = Mathf.Clamp(externalInputs[srcIdx], 0, ACTIVATION_TIMER);
            }
            for (int destIdx = 0; destIdx < numNeurons; destIdx++) {
                int connection = setOfConnections[destIdx];
                int connectionMult = (connection > CONNECTION_MAGNITUDE_THRESH ? 1 : (connection < -CONNECTION_MAGNITUDE_THRESH ? -1 : 0));
                int cooldownMult = (cooldowns[destIdx] == 0 ? 1 : 0);
                // sometimes pretend its a strong connection
                //if (Random.value < .05f) {
                //    connectionMult = 1;
                //}
                int add = (srcActivation > 0 ? 1 : 0);
                sumTotals[destIdx] += add * connectionMult * cooldownMult;

                // sometimes perturb the connection value
                if (Random.value < .005f) {
                    connections[srcIdx][destIdx] = Mathf.Clamp(connections[srcIdx][destIdx] + (-1 + Mathf.FloorToInt(2 * Random.value)),
                            -CONNECTION_MAGNITUDE_CAP, CONNECTION_MAGNITUDE_CAP);
                }
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
            // test activating all at once
            bool fired = sumTotals[idx] > SUM_THRESHOLD;
            // randomly fire neurons to try and create activity? it tends to erase legitimate connections though.
            if (fired || Random.value < .002f) {
                activations[idx] = ACTIVATION_TIMER;
                cooldowns[idx] = COOLDOWN_TIMER;
            } else {
                activations[idx] = Mathf.Max(activations[idx] - 1, 0);
                cooldowns[idx] = Mathf.Max(cooldowns[idx] - 1, 0);
            }
        }
    }

    // TODO ACTUALLY DO THE LEARNING BY CHANGING THE CONNECTION STRENGTHS

    /* DESIGN FOR HOW THE LEARNING CALCULATION WORKS */
    /*
     * should there be negative activation?
     * hmm
     * no.
     * how would a negative weight even happen
     * well so you can get positive or negative reactions
     * but activations are only positive? yes
     * i will write: src dest "emotion" --> weight change. +, -, 0
     * + + + -> +
     * 0 + + -> 0
     * + 0 + -> -
     * 0 0 + -> 0
     * + + - -> -
     * 0 + - -> 0
     * + 0 - -> +
     * 0 0 - -> 0
     * This causes things to never passively decay. Maybe needs some randomness for that
     */
     
    // finalEmotionState should be -1, +1, or 0
    void Learn(int inputEmotionState) {
        // TODO TEST: learn always. Learn faster if emotionstate is set.
        int finalEmotionState = inputEmotionState * 4 + 1;
        for (var src = 0; src < numNeurons; src++) {
            bool srcActive = activations[src] > 0;
            for (var dest = 0; dest < numNeurons; dest++) {
                bool destActive = activations[dest] > 0;
                if (/*activations[dest] != ACTIVATION_TIMER ||*/ activations[src] >= activations[dest]) {
                    // TEST: only learn when we just fired
                    // TEST: only learn when src fired before dest
                    continue;
                }
                // if src implies dest, change the weighting
                if (srcActive) {
                    // TODO: try doing this exponintially
                    connections[src][dest] = Mathf.Clamp(connections[src][dest] + 2 * finalEmotionState,
                            -CONNECTION_MAGNITUDE_CAP, CONNECTION_MAGNITUDE_CAP); // LEARN
                } else if (!srcActive) {
                    // if src and dest dont predict, un-learn the weighting....maybe
                    if (finalEmotionState > 0) {
                        connections[src][dest] = Mathf.Clamp(connections[src][dest] - finalEmotionState,
                                0, CONNECTION_MAGNITUDE_CAP); // UNLEARN
                    } else if (finalEmotionState < 0) {
                        connections[src][dest] = Mathf.Clamp(connections[src][dest] - finalEmotionState,
                                -CONNECTION_MAGNITUDE_CAP, 0); // UNLEARN
                    }
                }
            }
        }
    }

    void EnforceConnectionRules() {
        for (int i = 0; i < numNeurons; i++) {
            for (int j = 0; j < numNeurons; j++) {
                // I can only be connected to J if i < j
                // unless J is an output
                // i and j cannot be the same
                // inputs cannot connect straight to output
                if ((j >= numExposed && i >= j) || i == j || (j < numExposed && i < numExposed)) {
                    connections[i][j] = 0;
                }
            }
        }
    }
   
    public void PrintToTexture(Texture2D tex, int startX, int startY) {
        for (int i = 0; i < numExposed; i++) {
            tex.SetPixel(startX + i, startY + 0, Color.magenta / ACTIVATION_TIMER * externalInputs[i]);
            tex.SetPixel(startX + i, startY + 1, Color.cyan / ACTIVATION_TIMER * externalOutputs[i]);
        }
        for (int i = numExposed; i < numNeurons; i++) {
            Color cStart = Color.green / ACTIVATION_TIMER;
            if (activations[i] > 0) {
                cStart = Color.white / ACTIVATION_TIMER;
            }
            tex.SetPixel(startX + i, startY + 0, cStart * activations[i]);
            tex.SetPixel(startX + i, startY + 1, cStart * activations[i]);// double up for visibility
        }
        for (int i = 0; i < numNeurons; i++) {
            for (int j = 0; j < numNeurons; j++) {
                Color cStart = Color.yellow / CONNECTION_MAGNITUDE_CAP;
                if (connections[i][j] < 0) {
                    cStart = Color.red / CONNECTION_MAGNITUDE_CAP;
                }
                tex.SetPixel(startX + i, startY + 2 + j, cStart * Mathf.Abs(connections[i][j]));
            }
        }
    }

    public void ToIntList(List<int> result) {
        result.Add(numNeurons);
        result.Add(numExposed);
        for (int i = 0; i < numExposed; i++) {
            result.Add(externalInputs[i]);
        }
        for (int i = 0; i < numExposed; i++) {
            result.Add(externalOutputs[i]);
        }
        for (int i = 0; i < numNeurons; i++) {
            result.Add(activations[i]);
        }
        for (int i = 0; i < numNeurons; i++) {
            result.Add(cooldowns[i]);
        }
        for (int i = 0; i < numNeurons; i++) {
            for (int j = 0; j < numNeurons; j++) {
                result.Add(connections[i][j]);
            }
        }
    }

    public static NeuronCluster FromIntList(IEnumerator<int> list) {
        int numNeurons = list.Current;
        list.MoveNext();
        int numExposed = list.Current;
        list.MoveNext();
        NeuronCluster result = new NeuronCluster(numNeurons, numExposed);
        for (int i = 0; i < numExposed; i++) {
            result.externalInputs[i] = list.Current;
            list.MoveNext();
        }
        for (int i = 0; i < numExposed; i++) {
            result.externalOutputs[i] = list.Current;
            list.MoveNext();
        }
        for (int i = 0; i < numNeurons; i++) {
            result.activations[i] = list.Current;
            list.MoveNext();
        }
        for (int i = 0; i < numNeurons; i++) {
            result.cooldowns[i] = list.Current;
            list.MoveNext();
        }
        for (int i = 0; i < numNeurons; i++) {
            for (int j = 0; j < numNeurons; j++) {
                result.connections[i][j] = list.Current;
                list.MoveNext();
            }
        }
        return result;
    }
}
