using UnityEngine;
using MagicLeap.Soundfield;
using System;

public class AnalysisDemo : MonoBehaviour {
    public GameObject VoiceMeter;
    public GameObject BackgroundMeter;
    private MLAcousticAnalysis myAnalysis;
    private float lastVoice = 0f;
    private float lastAmbient = 0f;
    private const float delta = 0.2f;
    // Start is called before the first frame update
    void Start() {
        myAnalysis = gameObject.GetComponent<MLAcousticAnalysis>();
        Debug.Log("Analysis " + myAnalysis.ToString());
    }

    // Update is called once per frame
    void Update() {
        if (myAnalysis) {
            // scale voice
            Vector3 scale = VoiceMeter.transform.localScale;
            lastVoice = (1f - delta) * lastVoice + delta * (float)Math.Pow(10.0, myAnalysis.userVoiceLevelDbfsLastValue / 20.0);
            scale.y = lastVoice;
            VoiceMeter.transform.localScale = scale;
            //Debug.Log("last " + lastVoice.ToString());
            // scale background
            scale = BackgroundMeter.transform.localScale;
            lastAmbient = (1f - delta) * lastAmbient + delta * (float)Math.Pow(10.0, myAnalysis.ambientSoundLevelDbfsLastValue / 20.0);
            scale.y = lastAmbient;
            BackgroundMeter.transform.localScale = scale;
        }
    }
}