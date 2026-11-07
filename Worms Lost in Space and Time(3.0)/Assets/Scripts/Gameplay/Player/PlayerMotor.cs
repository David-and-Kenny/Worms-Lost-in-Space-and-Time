using System;
using UnityEngine;
namespace Worms.Gameplay
{
    public sealed class PlayerMotor
    {
        private readonly Rigidbody2D body;
        private readonly RigidbodyType2D flightBodyType;
        private readonly bool flightContacts;
        public PlayerMotor(Rigidbody2D body)
        {
            this.body = body ? body : throw new ArgumentNullException(nameof(body));
            flightBodyType = body.bodyType;
            flightContacts = body.useFullKinematicContacts;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        public void StartFalling() { body.linearVelocity = Vector2.down * 3f; }
        public void AttachToWheel()
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
        }
        public void FollowWheel(Transform holder)
        {
            body.position = holder.position;
            body.rotation = holder.eulerAngles.z - 90f;
        }
        public void RestoreFlight()
        {
            body.bodyType = flightBodyType;
            body.useFullKinematicContacts = flightContacts;
        }
        public void FaceConveyor(Transform conveyor) { body.rotation = conveyor.eulerAngles.z; }
        public void Launch(bool forced)
        {
            RestoreFlight();
            Vector2 heading = Quaternion.Euler(0, 0, body.rotation) * Vector2.up;
            body.linearVelocity = heading * (forced ? 5f : 10f);
        }
        public void RideConveyor(Transform conveyor, float speed) { body.linearVelocity = conveyor.right * speed; }
    }
}
