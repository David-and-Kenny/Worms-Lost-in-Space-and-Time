using System;
using UnityEngine;
namespace Worms.Gameplay
{
    /// <summary>Selected surface and the destructible wheel's actual occupant.</summary>
    public sealed class PlayerAttachment
    {
        public Wheel Wheel { get; private set; }
        public Conveyor Conveyor { get; private set; }
        public Transform Holder { get; private set; }
        private CountdownExplode countdown;
        public void Attach(Player player, Wheel wheel, Vector2 contact)
        {
            if (!wheel || !wheel.holder) throw new ArgumentException("Wheel requires its authored holder reference.", nameof(wheel));
            Leave();
            Wheel = wheel;
            Holder = wheel.holder;
            Holder.position = contact;
            Vector2 offset = contact - (Vector2)wheel.transform.position;
            Holder.rotation = Quaternion.AngleAxis(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg, Vector3.forward);
            countdown = wheel.GetComponent<CountdownExplode>();
            if (countdown) { countdown.RegisterPlayer(player); countdown.count(); }
        }
        public void Attach(Conveyor conveyor) { Leave(); Conveyor = conveyor; }
        public void Leave()
        {
            if (countdown) countdown.playerLeft();
            countdown = null;
            Wheel = null;
            Conveyor = null;
            Holder = null;
        }
        public bool HasSurface => (Wheel && Holder) || Conveyor;
        public void Follow(Transform player)
        {
            if (Wheel && Holder)
            {
                player.position = Holder.position;
                player.rotation = Holder.rotation * Quaternion.Euler(0f, 0f, -90f);
            }
            else if (Conveyor) player.rotation = Conveyor.transform.rotation;
        }
    }
}
