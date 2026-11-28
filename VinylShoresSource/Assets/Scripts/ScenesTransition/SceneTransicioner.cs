using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransicioner : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnID;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private GameObject blocker;

    void start()
    {
        if (blocker != null)
            blocker.SetActive(isLocked);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isLocked || !other.CompareTag("Player"))
            return;

        SpawnPoint.NextSpawnId = targetSpawnID;
        SceneManager.LoadScene(targetScene);
    }

    public void Unlock()
    {
        isLocked = false;
        if (blocker != null)
            blocker.SetActive(false);
    }
}
