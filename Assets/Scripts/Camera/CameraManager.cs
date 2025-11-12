using UnityEngine;

[DefaultExecutionOrder(-99)]
public class CameraManager : MonoBehaviour
{
    private Transform followTarget = null;
    [SerializeField] private Vector3 playerOffset;
    private void Awake()
    {
        if(GameServices.Cam != this)
            GameServices.Cam = this;
    }
    private void OnDisable()
    {
        if(GameServices.Cam == this)
            GameServices.Cam = null;
    }

    public void SetFollowTarget(Transform target) 
    {
        followTarget = target;
    }

    private void Update()
    {
        
    }
    private void LateUpdate()
    {
        if (followTarget == null) return;

        transform.position = followTarget.position;
        transform.rotation = followTarget.rotation;
    }
    private void FixedUpdate()
    {
        
    }
}
