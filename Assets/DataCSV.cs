using UnityEngine;

[System.Serializable]
public struct BodyProperty
{
    public float mass;              
    public float distance;         
    public float initial_velocity; 
}

public class DataCSV : MonoBehaviour
{
    public BodyProperty[] bp;

    void Start()
    {
        LoadIntoArray();
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
                bp[i].mass = mass;
                bp[i].distance = dist;
                bp[i].initial_velocity = vel;
            }
        }
    }
}
