using UnityEngine;
using Worms.Gameplay;
public enum PlayerTraversalKind { Attached, Launched, ForcedDetach }
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform wheelTransform;
    public Transform holdSlot;
    public bool playerAttached = false;
    public Wheel wheel;
    public int direction;
    public Transform back;
    private PlayerMotor motor;
    private readonly PlayerAttachment attachment = new PlayerAttachment();
    public IPlayerInput InputSource { get; set; }
    public PlayerAttachment Attachment => attachment;
    public event System.Action<Player, PlayerTraversalKind, Component> TraversalChanged;
    private void Awake()
    {
        motor = new PlayerMotor(GetComponent<Rigidbody2D>());
        InputSource = new PlayerInput();
    }
    private void Start() { motor.StartFalling(); }
    private void Update()
    {
        // Sample even while falling so a released menu touch is observed before the next attachment.
        bool jumpHeld = InputSource != null && InputSource.JumpHeld;
        if (!playerAttached) return;
        if (!attachment.HasSurface) { detatch(); return; }
        attachment.Follow(transform);
        if (jumpHeld) Detach(false);
    }
    private void OnCollisionEnter2D(Collision2D target)
    {
        var surface = target.gameObject.GetComponent<WheelSurface>();
        var surfaceWheel = surface ? surface.wheel : null;
        if (surfaceWheel && target.transform != wheelTransform && target.contactCount > 0)
            AttachToWheel(surfaceWheel, target.GetContact(0).point);
        else
        {
            var conveyor = target.gameObject.GetComponent<Conveyor>();
            if (conveyor) AttachToConveyor(conveyor);
        }
    }
    public bool AttachToWheel(Wheel surface, Vector2 contact)
    {
        if (!surface || !surface.holder || wheelTransform == surface.transform) return false;
        attachment.Attach(this, surface, contact);
        wheelTransform = surface.transform;
        wheel = surface;
        holdSlot = surface.holder;
        transform.localScale = new Vector3(surface.direction == -1f ? -1f : 1f, 1f, 1f);
        playerAttached = true;
        TraversalChanged?.Invoke(this, PlayerTraversalKind.Attached, surface);
        return true;
    }
    public void AttachToConveyor(Conveyor surface)
    {
        if (!surface) return;
        attachment.Attach(surface);
        holdSlot = null;
        wheelTransform = null;
        wheel = null;
        transform.rotation = surface.transform.rotation;
        motor.RideConveyor(surface.transform, surface.speed);
        playerAttached = true;
        TraversalChanged?.Invoke(this, PlayerTraversalKind.Attached, surface);
    }
    private void OnTriggerEnter2D(Collider2D target)
    {
        var region = target.GetComponent<CameraRegion>();
        if (!region || !cameraTransform) return;
        var follow = cameraTransform.GetComponent<CameraFollow>();
        if (!follow) return;
        follow.Configure(transform, back, region.center, direction != 0, direction != 1);
        direction = direction == 0 ? 1 : 0;
    }
    private void Detach(bool forced)
    {
        Component source = attachment.Wheel ? (Component)attachment.Wheel : attachment.Conveyor;
        attachment.Leave();
        playerAttached = false;
        holdSlot = null;
        motor.Launch(transform, forced);
        TraversalChanged?.Invoke(this, forced ? PlayerTraversalKind.ForcedDetach : PlayerTraversalKind.Launched, source);
    }
    public void detatch() { Detach(true); }
    private void OnDestroy() { attachment.Leave(); }
}
