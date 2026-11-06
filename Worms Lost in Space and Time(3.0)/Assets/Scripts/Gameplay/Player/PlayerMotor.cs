using System;
using UnityEngine;
namespace Worms.Gameplay
{
    public sealed class PlayerMotor
    {
        private readonly Rigidbody2D body;
        public PlayerMotor(Rigidbody2D body) { this.body = body ? body : throw new ArgumentNullException(nameof(body)); }
        public void StartFalling() { body.linearVelocity = Vector2.down * 3f; }
        public void Launch(Transform player, bool forced) { body.linearVelocity = player.up * (forced ? 5f : 10f); }
        public void RideConveyor(Transform conveyor, float speed) { body.linearVelocity = conveyor.right * speed; }
    }
}
