// InteractiveBody Starter Code
// Fall 2025. IMDM 327
// Instructor. Myungin Lee
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class InteractiveBody : MonoBehaviour
{
    public float G = 1f; // Gravity constant https://en.wikipedia.org/wiki/Gravitational_constant
    private GameObject[] body;
    BodyProperty[] bp;
    private int numberOfSphere = 200;
    public float fastforwardConst = 1f;
    TrailRenderer[] trailRenderer;
    public float tRTime = 5.0f;
    public float tRStartWidth = 0.7f;
    public float tREndWidth = 0.1f;
    private GameObject interactivePoint;
    public Vector3 interactPoint;// where to interact 
    private Vector3 previousInteractivePoint; 
    public float interactiveMass; // how much to interact
    MediaPipeBodyTracker mp;
    public float maxVelocity;
    public float closeDistance;
    int frameCount;

    struct BodyProperty // why struct?
    {                   // https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct
        public float mass;
        public Vector3 velocity;
        public Vector3 acceleration;
    }


    void Start()
    {
        /*if (mp == null)
        {
            mp = FindObjectOfType<MediaPipeBodyTracker>();
            if (mp == null)
            {
                Debug.LogWarning("InteractiveBody could not locate a MediaPipeBodyTracker in the scene.");
            }
        }*/
        // init condition
        //maxVelocity = 30f;
        //interactiveMass = 30f;
        //closeDistance = 16f; // sqrt value
        // interactive point 
        interactivePoint = new GameObject();//GameObject.CreatePrimitive(PrimitiveType.Sphere);
        interactivePoint.transform.position = new Vector3(0f, 0f, 180f);
        // Just like GO, computer should know how many room for struct is required:
        bp = new BodyProperty[numberOfSphere];
        body = new GameObject[numberOfSphere];
        trailRenderer = new TrailRenderer[numberOfSphere];
        // Loop generating the gameobject and assign initial conditions (type, position, (mass/velocity/acceleration)
        for (int i = 0; i < numberOfSphere; i++)
        {
            // Our gameobjects are created here:
            body[i] = GameObject.CreatePrimitive(PrimitiveType.Cube); // why sphere? try different options.
            // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

            // initial conditions
            float r = 100f;
            // position is (x,y,z). In this case, I want to plot them on the circle with r
            // ******** Fill in this part ******** // Initialization of the position
            body[i].transform.position = new Vector3(r * Mathf.Sin(i * 2f * Mathf.PI / numberOfSphere),
                                                      r * Mathf.Cos(i * 2f * Mathf.PI / numberOfSphere),
                                                      180f + Random.Range(-10f, 10f));
            // z = 180 to see this happen in front of me. Try something else (randomize) too.

            bp[i].velocity = new Vector3(0, 0, 0); // Try different initial condition
            bp[i].mass = Random.Range(1f, 5f); // Simplified. Try different initial condition
            body[i].GetComponent<MeshRenderer>().enabled = false;

            // + This is just pretty trails
            trailRenderer[i] = body[i].AddComponent<TrailRenderer>();
            // Configure the TrailRenderer's properties
            trailRenderer[i].time = tRTime;  // Duration of the trail
            trailRenderer[i].startWidth = tRStartWidth;  // Width of the trail at the start
            trailRenderer[i].endWidth = tREndWidth;    // Width of the trail at the end
            // a material to the trail
            trailRenderer[i].material = new Material(Shader.Find("Sprites/Default"));
            // Set the trail color
            Gradient gradient = new Gradient();
            float h = (i / (float)numberOfSphere) % 1f;
            float s = 0.45f;        
            float v = 0.98f;        
            Color c = Color.HSVToRGB(h, s, v); 

            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(c, 0.0f),
                                        new GradientColorKey(c, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer[i].colorGradient = gradient;

        }
    }

    public Vector3 followPosition = new Vector3(0f, 0f, 0f);
    public float currentFrequency = 0f; // for color mapping
    public float ccValue = 0.7f; // for opacity
    public float[] activeFrequencies = new float[0]; // for color splitting boids in groups

    void FixedUpdate()
    {
        // Draw interactivePoint
        interactivePoint.transform.position = interactPoint;

        // Loop for N-body gravity
        // How should we design the loop?
        // initailize 
        for (int i = 0; i < numberOfSphere; i++)
        {
            bp[i].acceleration = Vector3.zero; // important
        }

        // Acceleration (Force)  
        for (int i = 0; i < numberOfSphere; i++)
        {
            for (int j = i + 1; j < numberOfSphere; j++)
            {
                // Vector from i to j body. Make sure which vector you are getting.
                Vector3 distance = body[j].transform.position - body[i].transform.position;
                // Gravity
                Vector3 gravity = CalculateGravity(distance, bp[i].mass, bp[j].mass);
                // Apply Gravity
                // F = ma -> a = F/m
                // Gravity is push and pull with same amount. Force: m1 <-> m2

                // .. only if it is not too close
                if (distance.sqrMagnitude > closeDistance)
                {
                    bp[i].acceleration += gravity / bp[i].mass; // why is this +?
                    bp[j].acceleration -= gravity / bp[j].mass; // why is this -? What decided the direction?                   
                }
                else // apply opposite gravity (push) if too close. 
                { // Hatred is stronger than attraction.
                    bp[i].acceleration -= 3f * gravity / bp[i].mass; // 
                    bp[j].acceleration += 3f *gravity / bp[j].mass; // 
                }

            }
        }
    
        // (Force) Hesitation: randomly hover the space for natural behavior  
        for (int i = 0; i < numberOfSphere; i++)
        {
            float randomScale = 10f;
            if (Random.Range(0f, 1.05f) > 1f)
            {
                bp[i].acceleration += new Vector3(randomScale * Random.Range(-1f, 1f), randomScale * Random.Range(-1f, 1f), randomScale * Random.Range(-1f, 1f));
            }
        }


        // (Force) Interactive Acceleration : reacts to the actuation of the interactive point
        /*if (mp == null)
        {
            mp = FindObjectOfType<MediaPipeBodyTracker>();
            if (mp == null)
            {
                return;
            }
        }*/

        //Vector3 rightHandOffset = new Vector3(100f, 100f, 180f);
        Vector3 rightHandOffset = followPosition;
        interactPoint = /*-mp.RightHandPosition * 200f +*/ rightHandOffset;
        float actuation = 1f+ (previousInteractivePoint - interactPoint).sqrMagnitude;
        //if (mp.RightHandPinch)
        //{
            for (int i = 0; i < numberOfSphere; i++)
            {
                Vector3 distance = interactPoint - body[i].transform.position;
                bp[i].acceleration += CalculateGravity(distance, bp[i].mass, interactiveMass) / bp[i].mass * actuation;
            }
            previousInteractivePoint = interactPoint;
            // G = 1f + actuation * 0.01f;
        //}

        // Apply acceleration to velocity, to position
        for (int i = 0; i < numberOfSphere; i++)
        {
            // velocity is sigma(Acceleration*time)
            bp[i].velocity += bp[i].acceleration * Time.deltaTime * fastforwardConst;
            // Prevent extra ordinary speed
            body[i].transform.position += bp[i].velocity * Time.deltaTime * fastforwardConst;
            body[i].transform.LookAt(body[i].transform.position + bp[i].velocity);

            // Limit the maximum velocity
            if (bp[i].velocity.magnitude > maxVelocity)
            {
                bp[i].velocity = maxVelocity * bp[i].velocity.normalized;
            }
        }

        // Color update
        {
            int activeCount = 1;
            if (activeFrequencies.Length > 0)
                activeCount = activeFrequencies.Length;
            
            float defaultHue = 0.5f;
            if (currentFrequency > 0)
                defaultHue = Mathf.Clamp01((currentFrequency - 500f) / (2000f - 500f));
            
            for (int i = 0; i < numberOfSphere; i++)
            {
                // splitting into different groups
                int groupIndex = 0;
                if (activeCount > 1)
                    groupIndex = i * activeCount / numberOfSphere;
                
                float freq = currentFrequency;
                if (activeCount > 0 && groupIndex < activeFrequencies.Length)
                    freq = activeFrequencies[groupIndex];
                
                float frequencyHue = defaultHue;
                if (freq > 0)
                    frequencyHue = Mathf.Clamp01((freq - 500f) / (2000f - 500f));
                
                Gradient gradient = new Gradient();
                float h = frequencyHue;
                float s = 0.45f + bp[i].acceleration.sqrMagnitude / 1000f;
                float v = 0.98f + bp[i].acceleration.sqrMagnitude / 1000f;
                Color c = Color.HSVToRGB(h, s, v);

                float expandedCC = ccValue * ccValue * 1.3f;
                float opacity = Mathf.Lerp(0.1f, 1.5f, Mathf.Clamp01(expandedCC));
                
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(c, 0.0f),
                                            new GradientColorKey(c, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(opacity, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                trailRenderer[i].colorGradient = gradient;
            }
        }

        frameCount++;
    }


    // Gravity Fuction
    private Vector3 CalculateGravity(Vector3 distanceVector, float m1, float m2)
    {
        Vector3 gravity = new Vector3(0f, 0f, 0f); // note this is also Vector3
                                                   // **** Fill in the function below.
        float eps = 0.1f;
        gravity = G * m1 * m2 / (distanceVector.magnitude + eps) * distanceVector.normalized;
        return gravity;
    }
}

