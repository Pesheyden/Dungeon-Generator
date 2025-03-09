using NaughtyAttributes;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private int _seed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Button]
    void Print()
    {
        Random.InitState(_seed);
        float[] noiseValues = new float[10];
        for (int i = 0; i < noiseValues.Length; i++)
        {
            noiseValues[i] = Random.value;
            Debug.Log(noiseValues[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
