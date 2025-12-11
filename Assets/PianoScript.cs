using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ActiveNote
{
    public float baseFrequency, velocity, timeOn, timeOff, pitchBend;
}

public class PianoScript : MonoBehaviour
{
    [SerializeField] InteractiveBody interactiveBody;
    [SerializeField] List<GameObject> PianoKeyObjects = new List<GameObject>();
    [SerializeField] List<KeyCode> keyButtons = new List<KeyCode>();
    [SerializeField] float semitoneMultiplier = 40f;
    //[SerializeField] List<AudioSource> audioSources = new List<AudioSource>();

    [Header("ADSR")]
    public float attackTime = 0.05f, decayTime = 0.1f, sustainLevel = 0.7f, releaseTime = 0.1f;
    public float yAmplitude = 20f, yLerp = 10f;
    
    const int totalNotes = 108;
    ActiveNote[] notes = new ActiveNote[totalNotes]; 
    
    Vector3[] originalObjScales, originalObjPositions;
    Coroutine[] activeCoroutines;
    float targetY, currentY, currentX = 0f;

    

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
        // stores held notes for color splitting
        List<float> activeFreqs = new List<float>();
        float activeFreq = 0f;
        float latestTime = 0f;
        int activeNoteIndex = -1;
        
        for (int n = 0; n < totalNotes; n++)
        {
            if (notes[n].timeOn > 0 && notes[n].timeOff == 0)
            {
                float freq = notes[n].baseFrequency * Mathf.Pow(2f, notes[n].pitchBend / 12f);
                activeFreqs.Add(freq);
            }
            
            // Find most recent note for position
            if (notes[n].timeOn > latestTime)
            {
                latestTime = notes[n].timeOn;
                activeNoteIndex = n;
                activeFreq = notes[n].baseFrequency * Mathf.Pow(2f, notes[n].pitchBend / 12f);
            }
        }
        
        // x position of boids is set by frequency, y is set by sliding up and down
        if (activeFreq > 0)
        {
            // set for the octave around 500hz to 2000hz
            float normalized = (Mathf.Clamp(activeFreq, 500f, 2000f) - 500f) / (2000f - 500f);
            float targetX = -80f + normalized * (60f - (-80f));
            currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * yLerp);
            float pitchBend = activeNoteIndex >= 0 ? notes[activeNoteIndex].pitchBend : 0f;
            Debug.Log($"Freq: {activeFreq:F1}Hz, PitchBend: {pitchBend:F2}st, BaseFreq: {notes[activeNoteIndex].baseFrequency:F1}Hz, X: {currentX:F1}, TargetX: {targetX:F1}, Y: {currentY:F1}");
        }
        float yPos = 10f + (ccValue * ccValue * 2f - 1f) * 30f;
        currentY = Mathf.Lerp(currentY, yPos, Time.deltaTime * yLerp);
        
        if (interactiveBody != null)
        {
            interactiveBody.followPosition = new Vector3(currentX, currentY, 180f);
            interactiveBody.currentFrequency = activeFreq;
            interactiveBody.ccValue = ccValue;
            interactiveBody.activeFrequencies = activeFreqs.ToArray();
        }
    }

    IEnumerator FadeInAttack(int i){
        float timer = 0;
        GameObject key = PianoKeyObjects[i];
        Vector3 targetScale = new Vector3(originalObjScales[i].x, 2f, originalObjScales[i].z);
        Vector3 targetPos = new Vector3(originalObjPositions[i].x, originalObjPositions[i].y - 6.5f, originalObjPositions[i].z);
        
        while (timer < attackTime){
            timer += Time.deltaTime;
            float t = timer / attackTime;
            key.transform.localScale = Vector3.Lerp(key.transform.localScale, targetScale, t);
            key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, targetPos, t);
            yield return null;
        }
        key.transform.localScale = targetScale;
        key.transform.localPosition = targetPos;
    }

    IEnumerator FadeOutRelease(int i){
        float timer = 0;
        GameObject key = PianoKeyObjects[i];
        while (timer < releaseTime){
            timer += Time.deltaTime;
            float t = timer / releaseTime;
            key.transform.localScale = Vector3.Lerp(key.transform.localScale, originalObjScales[i], t);
            key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, originalObjPositions[i], t);
            yield return null;
        }
        key.transform.localScale = originalObjScales[i];
        key.transform.localPosition = originalObjPositions[i];
    }

    float onAudioTime, sampleRate, ccValue = 0.707f; // sqrt(0.5) - centers Y at 10

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = 0f;
            for (int n = 0; n < totalNotes; n++)
            {
                if (notes[n].timeOn == 0) continue;
                
                // Take base freq, then the Pow will convert semitones to frequency multiplier
                // so sliding left and right will change the note played
                float freq = notes[n].baseFrequency * Mathf.Pow(2f, notes[n].pitchBend / 12f);
                float amp = GetEnvelopeAmplitude(n, onAudioTime);

                // boosts velocities from being too low (0.5 floor)
                float vel = Mathf.Sqrt(notes[n].velocity * 0.3f + 0.5f); 
                
                // add on overtone waves to make note sound more "whole"
                if (amp > 0 && freq > 0)
                    sample += WaveWithHarmonics(onAudioTime, freq) * amp * vel;
            }
            
            // outputting to channels
            for (int c = 0; c < channels; c++)
                data[i + c] = Mathf.Clamp(sample, -1f, 1f);
            
            onAudioTime += 1f / sampleRate;
        }
    }
    
    float WaveWithHarmonics(float time, float freq)
    {
        float sample = 0f, sum = 0f;
        for (int h = 1; h <= 12; h++)
        {
            float amp = 1f / h;
            sample += Mathf.Sin(time * 2f * Mathf.PI * freq * h) * amp;
            sum += amp;
        }
        return sample / sum;
    }
    
    float GetEnvelopeAmplitude(int n, float time)
    {
        ActiveNote note = notes[n];
        float elapsed = time - note.timeOn;
        
        if (note.timeOff > 0)
        {
            float releaseElapsed = time - note.timeOff;
            if (releaseElapsed >= releaseTime) return 0f;
            float releaseStart = GetAmpAtTime(elapsed - releaseElapsed);
            return Mathf.Lerp(releaseStart, 0f, releaseElapsed / releaseTime);
        }
        
        return GetAmpAtTime(elapsed);
    }
    
    float GetAmpAtTime(float elapsed)
    {
        if (elapsed < attackTime) return elapsed / attackTime;
        if (elapsed < attackTime + decayTime) return Mathf.Lerp(1f, sustainLevel, (elapsed - attackTime) / decayTime);
        return sustainLevel * (ccValue * ccValue * 2f); // ccValue changes amplitude by sliding up and down
    }
    
    public void MIDIControlChange(float value) 
    { 
        ccValue = value; 
    }

    public void MIDINoteOn(int noteNum, float velocity)
    {
        int i = Mathf.Clamp(noteNum - 12, 0, totalNotes - 1);
        notes[i] = new ActiveNote
        {
            baseFrequency = 440f * Mathf.Pow(2f, (noteNum - 69f) / 12f),
            velocity = Mathf.Clamp01(velocity),
            timeOn = onAudioTime,
            timeOff = 0f,
            pitchBend = notes[i].pitchBend
        };
        
        int keyI = noteNum - 60;
        if (keyI >= 0 && keyI < PianoKeyObjects.Count)
        {
            if (activeCoroutines[keyI] != null) StopCoroutine(activeCoroutines[keyI]);
            activeCoroutines[keyI] = StartCoroutine(FadeInAttack(keyI));
        }
    }

    public void MIDINoteOff(int noteNum)
    {
        int i = Mathf.Clamp(noteNum - 12, 0, totalNotes - 1);
        if (notes[i].timeOn > 0) notes[i].timeOff = onAudioTime;
        
        int keyI = noteNum - 60;
        if (keyI >= 0 && keyI < PianoKeyObjects.Count)
        {
            if (activeCoroutines[keyI] != null) StopCoroutine(activeCoroutines[keyI]);
            activeCoroutines[keyI] = StartCoroutine(FadeOutRelease(keyI));
        }
    }
    
    public void MIDIPitchBend(float bendValue)
    {
        float semitones = bendValue * semitoneMultiplier;
        for (int i = 0; i < totalNotes; i++)
            if (notes[i].timeOn > 0) notes[i].pitchBend = semitones;
    }
}


