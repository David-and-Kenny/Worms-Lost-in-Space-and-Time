using UnityEngine;
using UnityEngine.SceneManagement;
using Worms;
public class Finish : MonoBehaviour
{
    protected string currentLevel;
    public string nextLevel;
    private void Start() { currentLevel = SceneManager.GetActiveScene().name; }
    private void OnTriggerEnter2D(Collider2D target)
    {
        if (target.CompareTag("Player")) FinishRun.TryComplete(target.gameObject);
    }
}
