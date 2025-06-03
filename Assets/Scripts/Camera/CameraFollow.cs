using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _followPoint;
    [SerializeField] private Vector3 _offset;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = _followPoint.transform.position + _offset;
    }
}
