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
    private int numberOfSphere = 3;
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
            body[i] = GameObject.CreatePrimitive(PrimitiveType.Cube); // why sphere? try different options.
            // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

            // initial conditions
            float r = 100f;
            // position is (x,y,z). In this case, I want to plot them on the circle with r

            // ******** Fill in this part ********
            // body[i].transform.position = new Vector3( ***, *** , 180);
            // z = 180 to see this happen in front of me. Try something else (randomize) too.

            bp[i].velocity = new Vector3(0,0,0); // Try different initial condition
            bp[i].mass = 1; // Simplified. Try different initial condition


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
        for (int i = 0; i < numberOfSphere; i++)
        {
            // Something
        }

    }

    // Gravity Fuction to finish
    private Vector3 CalculateGravity(Vector3 distanceVector, float m1, float m2)
    {
        Vector3 gravity = new Vector3(0f,0f,0f); // note this is also Vector3
       // **** Fill in the function below. 
        // gravity = ****;
        return gravity;
    }
}

