using UnityEngine;
using UnityEngine.Events;

public class MouseClickController : MonoBehaviour
{
    public Vector3 clickPosition;
    
    public UnityEvent<Vector3> OnClick;
    // Update is called once per frame
    void Update()
    {
        // Get the mouse click position in world space
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay( Input.mousePosition );
            if (Physics.Raycast( mouseRay, out RaycastHit hitInfo ))
            {
                Vector3 clickWorldPosition = hitInfo.point;
                clickPosition = clickWorldPosition;

                OnClick.Invoke(clickPosition);
            }
        }
        
        DebugExtension.DebugWireSphere(clickPosition, Color.yellow, .1f);
        Debug.DrawLine(Camera.main.transform.position, clickPosition, Color.yellow);
    }
}
