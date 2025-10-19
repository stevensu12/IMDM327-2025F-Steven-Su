using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoScript : MonoBehaviour
{
    [SerializeField] InteractiveBody interactiveBody;
    [SerializeField] List<GameObject> PianoKeyObjects = new List<GameObject>();
    [SerializeField] List<KeyCode> keyButtons = new List<KeyCode>();
    [SerializeField] List<AudioSource> audioSources = new List<AudioSource>();

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
                activeCoroutines[i] = StartCoroutine(FadeInAttack(audioSources[i], i));
            }
            if (Input.GetKeyUp(keyButtons[i])){
                if (activeCoroutines[i] != null){
                    StopCoroutine(activeCoroutines[i]);
                }
                activeCoroutines[i] = StartCoroutine(FadeOutRelease(audioSources[i], i));
            }
        }
        
        // Update y position continuously
        interactiveBody.followPosition = new Vector3(interactiveBody.followPosition.x, currentY, interactiveBody.followPosition.z);
    }

    IEnumerator FadeInAttack(AudioSource aS, int index){
        float timer = 0;
        aS.volume = 0;
        aS.Play();
        
        GameObject key = PianoKeyObjects[index];
        Vector3 ogScale = new Vector3(originalObjScales[index].x, 2f, originalObjScales[index].z);
        Vector3 ogPosition = new Vector3(originalObjPositions[index].x, originalObjPositions[index].y - 6.5f, originalObjPositions[index].z);
        
        while (timer < attackTime){
            timer += Time.deltaTime;
            aS.volume = Mathf.Lerp(0, 1, timer / attackTime);
            key.transform.localScale = Vector3.Lerp(key.transform.localScale, ogScale, timer / attackTime);
            key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, ogPosition, timer / attackTime);
            yield return null;
        }
        
        // Ensure final values
        key.transform.localScale = ogScale;
        key.transform.localPosition = ogPosition;
    }

    IEnumerator FadeOutRelease(AudioSource aS, int index){
        float timer = 0;
        float currentVolume = aS.volume;
        GameObject key = PianoKeyObjects[index];
        while (timer < releaseTime){
            timer += Time.deltaTime;
            aS.volume = Mathf.Lerp(currentVolume, 0, timer / releaseTime);
            key.transform.localScale = Vector3.Lerp(key.transform.localScale, originalObjScales[index], timer / releaseTime);
            key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, originalObjPositions[index], timer / releaseTime);
            yield return null;
        }
        
        // Ensure final values
        key.transform.localScale = originalObjScales[index];
        key.transform.localPosition = originalObjPositions[index];
        aS.Stop();
    }
}
