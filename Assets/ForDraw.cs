using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForDraw : MonoBehaviour
{
    GameObject[] body;
    public Material[] material;
    private int numberOfSphere = 300;
    private float timeflow = 0;
    float radius = 0.1f;

    void Start()
    {
        body = new GameObject[numberOfSphere];
        // Loop generating the gameobject and assign initial conditions 
        for (int i = 0; i < numberOfSphere; i++)
        {
            // Our gameobjects are created here:
            body[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere); // why sphere? try different options.
            // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

            // initial position
            body[i].transform.position = new Vector3(radius * Mathf.Sin(timeflow),
                                                    radius * Mathf.Cos(timeflow),
                                                    180);
            // initial color
            var meshRenderer = body[i].GetComponent<Renderer>();
            meshRenderer.material.SetColor("_Color", new Color(i/255f, (255-i)/255f, 255/255f));

        }
    }

    void Update()
    {
        timeflow += Time.deltaTime;
        // How to make them move over the time
        for (int i = 0; i < numberOfSphere; i++)
        {
            body[i].transform.position = new Vector3((radius+i) * Mathf.Sin(timeflow),
                                                    (radius + i) * Mathf.Cos(timeflow), 
                                                    10+i);
        }

    }
}


