using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public static string NextSpawnId;  
    [SerializeField] private string spawnId;

    void Start()
    {
        if (NextSpawnId != spawnId) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        player.transform.position = transform.position;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = transform.position;

        NextSpawnId = null;
    }
}