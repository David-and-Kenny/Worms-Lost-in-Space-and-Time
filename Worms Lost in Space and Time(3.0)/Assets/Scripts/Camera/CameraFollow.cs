using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Transform background;
    public float smoothTime = 0.3f;
    public bool followingX;
    public bool followingY;
    public Transform centerFloat;
    private Vector3 velocity;
    public void Configure(Transform player, Transform backdrop, Transform center, bool followX, bool followY)
    {
        target = player;
        background = backdrop;
        centerFloat = center;
        followingX = followX;
        followingY = followY;
        velocity = Vector3.zero;
    }
    private void Start()
    {
        var camera = GetComponent<Camera>();
        if (!camera) return;
        float aspect = (float)Screen.height / Screen.width;
        if (aspect > 1.7f) camera.orthographicSize = 9.9f;
        else if (aspect >= 1.5f) camera.orthographicSize = 9f;
        else if (aspect >= 1.3f) camera.orthographicSize = 7.5f;
    }
    private void Update()
    {
        if (!target) return;
        Vector3 position = transform.position;
        if (followingX)
        {
            position.x = Mathf.SmoothDamp(position.x, target.position.x, ref velocity.x, smoothTime);
            if (centerFloat) position.y = Mathf.SmoothDamp(position.y, centerFloat.position.y, ref velocity.y, smoothTime);
        }
        if (followingY)
        {
            position.y = Mathf.SmoothDamp(position.y, target.position.y, ref velocity.y, smoothTime);
            if (centerFloat) position.x = Mathf.SmoothDamp(position.x, centerFloat.position.x, ref velocity.x, smoothTime);
        }
        transform.position = position;
        if (background) background.position = new Vector3(position.x, position.y, background.position.z);
    }
}
