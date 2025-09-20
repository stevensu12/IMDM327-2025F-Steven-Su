using UnityEngine;

[System.Serializable]
public struct BodyProperty
{
    public float mass;
    public float distance;
    public float initial_velocity;

    public Vector3 velocity;
    
    public Vector3 acceleration;
}

public class DataCSV : MonoBehaviour
{
    [SerializeField] GameObject sceneCamera;
    private const float G = 500f;
    public BodyProperty[] bp;

    GameObject[] planets;

    float scalingFloat = 1e10f;

    void Start()
    {
        sceneCamera.transform.position = new Vector3(0, 0, -700);
        LoadIntoArray();

        planets = new GameObject[bp.Length];

        for (int i = 0; i < bp.Length; i++)
        {
            planets[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planets[i].transform.localScale = new Vector3(5, 5, 5);

            // place planets along circle
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float x = bp[i].distance * Mathf.Cos(angle);
            float y = bp[i].distance * Mathf.Sin(angle);
            planets[i].transform.position = new Vector3(x, y, 0);

            //setting velocity in relation to sun : bp[0]
            Vector3 pos = new Vector3(x, y, 0);
            float r = pos.magnitude;
            if (r <= 0) r = 0.1f;
            float vel = Mathf.Sqrt(G * bp[0].mass / r);
            bp[i].velocity = new Vector3(-y, x, 0).normalized * vel;

            TrailRenderer trailRenderer = planets[i].AddComponent<TrailRenderer>();
            // Configure the TrailRenderer's properties
            trailRenderer.time = 100.0f;  // Duration of the trail
            trailRenderer.startWidth = 1f;  // Width of the trail at the start
            trailRenderer.endWidth = 0.1f;    // Width of the trail at the end
            // a material to the trail
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // Set the trail color over time
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color (Mathf.Cos(Mathf.PI * 2 / bp.Length * i), Mathf.Sin(Mathf.PI * 2 / bp.Length * i), Mathf.Tan(Mathf.PI * 2 / bp.Length * i)), 0.80f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer.colorGradient = gradient;
        }
    }

    void Update()
    {
        for (int i = 0; i < bp.Length; i++) bp[i].acceleration = Vector3.zero;

        for (int i = 0; i < bp.Length; i++)
        {
            for (int j = 0; j < bp.Length; j++)
            {
                if (i != j)
                {
                    Vector3 temp = planets[j].transform.position - planets[i].transform.position;
                    Vector3 gravity = CalculateGravity(temp, bp[i].mass, bp[j].mass);
                    bp[i].acceleration += gravity / bp[i].mass;
                }
            }
        }

        for (int i = 0; i < bp.Length; i++)
        {
            bp[i].velocity += bp[i].acceleration * Time.deltaTime;
            planets[i].transform.position += bp[i].velocity * Time.deltaTime;   
        }
    }

    private Vector3 CalculateGravity(Vector3 distanceVector, float m1, float m2)
    {
        float temp = distanceVector.magnitude;
        if (temp <= 0f) temp = 0.1f;
        float magnitude = G * m1 * m2 / (temp * temp);
        Vector3 gravity = distanceVector.normalized * magnitude;
        return gravity;
    }

    void LoadIntoArray()
    {
        // Load Assets/Resources/solar.csv (omit extension)
        // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Resources.Load.html
        TextAsset csv = Resources.Load<TextAsset>("solar"); 
        if (csv == null) // - Safer
        {
            Debug.LogError("Resources/solar.csv not found.");
            bp = new BodyProperty[0];
            return;
        }
        string[] lines = csv.text.Split('\n'); // \n = line feed

        // Allocate array with read values
        bp = new BodyProperty[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim(); 
            // - Safer: Trim() is used to remove whitespace or specific characters
            string[] cols = line.Split(',');

            if (float.TryParse(cols[2].Trim(), out float mass) &&
                float.TryParse(cols[4].Trim(), out float dist) &&
                float.TryParse(cols[5].Trim(), out float vel))
            {
                // assignment into array
                bp[i].mass = mass / scalingFloat;
                bp[i].distance = dist / scalingFloat;
                bp[i].initial_velocity = vel / scalingFloat;
            }
        }
    }
}
