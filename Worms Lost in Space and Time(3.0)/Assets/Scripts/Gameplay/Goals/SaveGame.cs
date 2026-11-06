using UnityEngine;
using Worms;
public class SaveGame : MonoBehaviour
{
    public float starTime = 0f;
    public int currentLevel;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) FinishRun.TryComplete(other.gameObject);
    }
}
