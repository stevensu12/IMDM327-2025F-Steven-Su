// 3-body Starter Code
// Fall 2025. IMDM 327
// Instructor. Myungin Lee
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class ThreeBody : MonoBehaviour
{
    private const float G = 500f; // Gravity constant https://en.wikipedia.org/wiki/Gravitational_constant
    GameObject[] body;
    BodyProperty[] bp;
    private int numberOfSphere = 100;
    TrailRenderer trailRenderer;
    struct BodyProperty // why struct?
    {                   // https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct
        public float mass;
        public Vector3 velocity;
        public Vector3 acceleration;
    }

 
    void Start()
    {
        // Just like GO, computer should know how many room for struct is required:
        bp = new BodyProperty[numberOfSphere];
        body = new GameObject[numberOfSphere];

        // Loop generating the gameobject and assign initial conditions (type, position, (mass/velocity/acceleration)
        for (int i = 0; i < numberOfSphere; i++)
        {          
            // Our gameobjects are created here:
            body[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere); // why sphere? try different options.
            // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

            // initial conditions
            float r = 40f;

            // ******** Fill in this part ********
            // body[i].transform.position = new Vector3( ***, *** , 180);
            // z = 180 to see this happen in front of me. Try something else (randomize) too.

            // place bodies evenly in a circle shape
            float angle = i * Mathf.PI * 2f / numberOfSphere;
            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r;
            body[i].transform.position = new Vector3(x, y, 0f);

            // randomizing mass and velocity
            bp[i].mass = Random.Range(0.01f, 0.2f);
            bp[i].velocity = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0);

            //--------------------

            // + This is just pretty trails
            trailRenderer = body[i].AddComponent<TrailRenderer>();
            // Configure the TrailRenderer's properties
            trailRenderer.time = 100.0f;  // Duration of the trail
            trailRenderer.startWidth = 0.5f;  // Width of the trail at the start
            trailRenderer.endWidth = 0.1f;    // Width of the trail at the end
            // a material to the trail
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // Set the trail color over time
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color (Mathf.Cos(Mathf.PI * 2 / numberOfSphere * i), Mathf.Sin(Mathf.PI * 2 / numberOfSphere * i), Mathf.Tan(Mathf.PI * 2 / numberOfSphere * i)), 0.80f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer.colorGradient = gradient;

        }
    }

    void Update()
    {
        // Loop for N-body gravity
        // How should we design the loop?
        for (int i = 0; i < numberOfSphere; i++) bp[i].acceleration = Vector3.zero;

        for (int i = 0; i < numberOfSphere; i++)
        {
            // Something
            for (int j = 0; j < numberOfSphere; j++)
            {
                if (i != j)
                {
                    Vector3 temp = body[j].transform.position - body[i].transform.position;
                    Vector3 force = CalculateGravity(temp, bp[i].mass, bp[j].mass);

                    bp[i].acceleration += force / bp[i].mass;
                }
            }
        }

        for (int i = 0; i < numberOfSphere; i++)
        {
            bp[i].velocity += bp[i].acceleration * Time.deltaTime;
            body[i].transform.position += bp[i].velocity * Time.deltaTime;
        }
    }

    // Gravity Fuction to finish
    private Vector3 CalculateGravity(Vector3 distanceVector, float m1, float m2)
    {
        float temp = distanceVector.magnitude;
        if (temp <= 0f) temp = 0.1f;
        float magnitude = G * m1 * m2 / temp;
        Vector3 gravity = distanceVector * magnitude;
        return gravity;
    }
}

