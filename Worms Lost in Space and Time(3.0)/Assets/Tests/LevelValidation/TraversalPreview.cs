using System.Collections.Generic;
using UnityEngine;

namespace Worms.LevelValidation
{
    /// <summary>Scene-view breadcrumbs for an interactive replay; excluded from shipping builds.</summary>
    public sealed class TraversalPreview : MonoBehaviour
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> launches = new List<Vector3>();
        private Player player;
        private void Awake()
        {
            player = GetComponent<Player>();
            player.TraversalChanged += Traversal;
        }
        private void Traversal(Player source, PlayerTraversalKind kind, Component surface)
        {
            if (kind != PlayerTraversalKind.Attached) launches.Add(transform.position);
        }
        private void LateUpdate() { if (positions.Count < 30000) positions.Add(transform.position); }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            for (int index = 1; index < positions.Count; index++) Gizmos.DrawLine(positions[index - 1], positions[index]);
            Gizmos.color = Color.yellow;
            foreach (var launch in launches) Gizmos.DrawSphere(launch, .08f);
        }
        private void OnDestroy() { if (player) player.TraversalChanged -= Traversal; }
    }
}
