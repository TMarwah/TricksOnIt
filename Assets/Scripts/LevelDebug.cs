using UnityEngine;

/// <summary>
/// LevelDebug allows teleporting the player to different spawn points using keyboard shortcuts.
/// Attach this script to a GameObject in your scene for debugging purposes.
/// </summary>
public class LevelDebug : MonoBehaviour
{
  [Tooltip("Drag your Player GameObject here.")]
  public GameObject player;

  [Tooltip("Drag all your Spawn Point Transforms here. Assign them in order (0, 1, 2, 3...).")]
  public Transform[] spawnPoints;

  private int currentSpawnPointIndex = 0;

  void Start()
  {
    // Basic validation: Check if player and spawn points are assigned.
    if (player == null)
    {
      Debug.LogError("LevelDebug: Player GameObject is not assigned! Please assign the Player in the Inspector.", this);
      enabled = false;
      return;
    }

    if (spawnPoints == null || spawnPoints.Length == 0)
    {
      Debug.LogError("LevelDebug: No Spawn Points assigned! Please assign Transform objects to the 'Spawn Points' array in the Inspector.", this);
      enabled = false;
      return;
    }

    // Teleport player to the first spawn point on game start
    TeleportPlayer(0);
  }

  void Update()
  {
    HandleInput();
  }

  /// <summary>
  /// Handles keyboard input for teleporting the player to different spawn points.
  /// Keys '1', '2', '3', '4' correspond to spawn points at index 0, 1, 2, and 3 respectively.
  /// </summary>
  private void HandleInput()
  {
    if (spawnPoints == null || spawnPoints.Length == 0)
      return;

    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      TeleportPlayer(0);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      TeleportPlayer(1);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha3))
    {
      TeleportPlayer(2);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha4))
    {
      TeleportPlayer(3);
    }
    // Add more else if blocks for more spawn points if needed
    // Example for cycling:
    // else if (Input.GetKeyDown(KeyCode.Space))
    // {
    //     currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Length;
    //     TeleportPlayer(currentSpawnPointIndex);
    // }
  }

  /// <summary>
  /// Teleports the player to the specified spawn point index.
  /// </summary>
  /// <param name="index">The index of the spawn point in the 'spawnPoints' array.</param>
  private void TeleportPlayer(int index)
  {
    if (player == null)
    {
      Debug.LogWarning("LevelDebug: Cannot teleport player. Player GameObject is not assigned.");
      return;
    }

    if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
    {
      Debug.LogWarning($"LevelDebug: Invalid spawn point index {index}. Please ensure the index is within the bounds of the 'Spawn Points' array.", this);
      return;
    }

    player.transform.position = spawnPoints[index].position;
    Debug.Log($"Player teleported to Spawn Point {index + 1} at position: {spawnPoints[index].position}");

    Rigidbody rb = player.GetComponent<Rigidbody>();
    if (rb != null)
    {
#if UNITY_2022_2_OR_NEWER
      rb.linearVelocity = Vector3.zero;
#else
      rb.velocity = Vector3.zero;
#endif
      rb.angularVelocity = Vector3.zero;
    }

    // If your player has a CharacterController, you might need to handle it differently
    // CharacterController cc = player.GetComponent<CharacterController>();
    // if (cc != null)
    // {
    //     // For CharacterController, directly setting position might not work as expected
    //     // Consider disabling/enabling the CharacterController around the teleport if issues arise
    // }
  }
}