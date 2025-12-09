using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoScript : MonoBehaviour
{
    [SerializeField] InteractiveBody interactiveBody;
    [SerializeField] List<GameObject> PianoKeyObjects = new List<GameObject>();
    [SerializeField] List<KeyCode> keyButtons = new List<KeyCode>();
    //[SerializeField] List<AudioSource> audioSources = new List<AudioSource>();

    public float attackTime = 0.05f;
    public float releaseTime = 0.2f;
    public float yAmplitude = 20f;
    public float yLerp = 10f; 
    
    private Vector3[] originalObjScales;
    private Vector3[] originalObjPositions;
    private Coroutine[] activeCoroutines;
    private bool noKeyPressed = false;
    private float targetY = 0f;
    private float currentY = 0f;

    void Start(){
        originalObjScales = new Vector3[PianoKeyObjects.Count];
        originalObjPositions = new Vector3[PianoKeyObjects.Count];
        activeCoroutines = new Coroutine[PianoKeyObjects.Count];
        
        for (int i = 0; i < PianoKeyObjects.Count; i++)
        {
            originalObjScales[i] = PianoKeyObjects[i].transform.localScale;
            originalObjPositions[i] = PianoKeyObjects[i].transform.localPosition;
        }
        
        // For OnAudioFilterRead, sampleRate has to be cached in Start()
        sampleRate = AudioSettings.outputSampleRate;
    }

    void Update(){
        // Check if any key is currently pressed
        noKeyPressed = true;
        for (int i = 0; i < keyButtons.Count; i++){
            if (Input.GetKey(keyButtons[i])){
                noKeyPressed = false;
                break;
            }
        }
        if (noKeyPressed) targetY = 0;
        else targetY = yAmplitude;

        
        // Lerp currentY towards targetY
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * yLerp);
        
        for (int i = 0; i < keyButtons.Count; i++){
            if (Input.GetKeyDown(keyButtons[i])){

                //setting position for interactive body
                interactiveBody.followPosition = new Vector3(i * 20f - 80f, currentY, interactiveBody.followPosition.z);

                if (activeCoroutines[i] != null) // makes playing feel smoother
                {
                    StopCoroutine(activeCoroutines[i]);
                }
                activeCoroutines[i] = StartCoroutine(FadeInAttack(/*audioSources[i], */i));
            }
            if (Input.GetKeyUp(keyButtons[i])){
                if (activeCoroutines[i] != null){
                    StopCoroutine(activeCoroutines[i]);
                }
                activeCoroutines[i] = StartCoroutine(FadeOutRelease(/*audioSources[i], */i));
            }
        }
        
        // Update y position continuously
        interactiveBody.followPosition = new Vector3(interactiveBody.followPosition.x, currentY, interactiveBody.followPosition.z);
    }

    IEnumerator FadeInAttack(/*AudioSource aS, */int index){
        float timer = 0;
        //aS.volume = 0;
        //aS.Play();
        
        GameObject key = PianoKeyObjects[index];
        Vector3 ogScale = new Vector3(originalObjScales[index].x, 2f, originalObjScales[index].z);
        Vector3 ogPosition = new Vector3(originalObjPositions[index].x, originalObjPositions[index].y - 6.5f, originalObjPositions[index].z);
        
        while (timer < attackTime){
            timer += Time.deltaTime;
            //aS.volume = Mathf.Lerp(0, 1, timer / attackTime);
            key.transform.localScale = Vector3.Lerp(key.transform.localScale, ogScale, timer / attackTime);
            key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, ogPosition, timer / attackTime);
            yield return null;
        }
        
        // Ensure final values
        key.transform.localScale = ogScale;
        key.transform.localPosition = ogPosition;
    }

    IEnumerator FadeOutRelease(/*AudioSource aS, */int index){
        float timer = 0;
        //float currentVolume = aS.volume;
        GameObject key = PianoKeyObjects[index];
        while (timer < releaseTime){
            timer += Time.deltaTime;
            //aS.volume = Mathf.Lerp(currentVolume, 0, timer / releaseTime);
            key.transform.localScale = Vector3.Lerp(key.transform.localScale, originalObjScales[index], timer / releaseTime);
            key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, originalObjPositions[index], timer / releaseTime);
            yield return null;
        }
        
        // Ensure final values
        key.transform.localScale = originalObjScales[index];
        key.transform.localPosition = originalObjPositions[index];
        //aS.Stop();
    }

    /*
    waveFunction acts as the periodic displacement of the speaker cone (sin wave).
    Normal sin period has 2pi, period = 1/frequency which is 0.1592 Hz. Since this is lower than
    the minimmum range of perceivable sounds, sin has to be normalized to a period of 1 and then
    multiplied to get a desired frequency. */
    float waveFunction(float time, float frequency){
        return Mathf.Sin(time * 2f * Mathf.PI * frequency);
    }

    private float onAudioTime = 0f;
    private float sampleRate; // cached in start

    /* OnAudioFilterRead generates sound if an AudioSource component is attached. Runs continuously every second. 
    float[] data is empty at first but is filled with 1000+ audio samples ranging from -1 to 1,
    -1 is max negative displacement, 0 is the speaker at rest, 1 is max positive displacement. 
    Each sample is a position of the sound wave at the current exact moment, so playing them together creates continuous sound.
    int channels specifies the number of audio output channels (automatically set by Unity, most systems use stereo so = 2), 
    ie 1 = mono, 2 stereo (with channel 0 being left speaker, channel 1 being right speaker). */

    // think of method like a camera: each sample is like an individual frame, 
    // connecting and playing them quickly together creates a "moving" image 
    // or in this case a continuous sound

    float frequency = 0;
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (frequency <= 0)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0f;
            }
            return;
        }
        
        // loop through data array to process each audio sample at a time
        for (int i = 0; i < data.Length; i += channels)
        {
            // generates sample at this exact moment on the sound wave
            float sample = waveFunction(onAudioTime, frequency);
            
            // writes the value to all channels
            for (int channel = 0; channel < channels; channel++)
            {
                data[i + channel] = sample;
            }
            
            // move time forward by the duration of one sample 
            onAudioTime += 1f / sampleRate;
        }
    }

    public void MIDINoteOn(int noteNum, float velocity){
        frequency = 440f * Mathf.Pow(2f, (noteNum - 69f) / 12f);
        Debug.Log("calculated frequency: " + frequency);

        int keyIndex = noteNum - 60; 
        if (keyIndex < 0 || keyIndex >= PianoKeyObjects.Count) return;

        // animate press
        if (activeCoroutines[keyIndex] != null)
            StopCoroutine(activeCoroutines[keyIndex]);

        activeCoroutines[keyIndex] = StartCoroutine(FadeInAttack(keyIndex));
    }

    public void MIDINoteOff(int noteNum)
    {
        frequency = 0;
        int keyIndex = noteNum - 60;
        if (keyIndex < 0 || keyIndex >= PianoKeyObjects.Count) return;

        if (activeCoroutines[keyIndex] != null)
            StopCoroutine(activeCoroutines[keyIndex]);

        activeCoroutines[keyIndex] = StartCoroutine(FadeOutRelease(keyIndex));
    }
}
