using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 eulerRotation;
    [SerializeField] private float damper;

    private void Start()
    {
        transform.eulerAngles = eulerRotation;
    }

    private void FixedUpdate()
    {
        if(target == null) { return; }

        transform.position = Vector3.Lerp(transform.position, target.position + offset, damper * Time.fixedDeltaTime);
    }
}
