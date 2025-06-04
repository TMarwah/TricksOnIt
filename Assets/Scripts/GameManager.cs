using UnityEngine;
using System.Collections.Generic; // Required for List if you prefer that over array

/// <summary>
/// GameManager script to handle player teleportation for debugging purposes.
/// Assign this script to an empty GameObject in your scene (e.g., "GameManager").
/// </summary>
public class GameManager : MonoBehaviour
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
            Debug.LogError("GameManager: Player GameObject is not assigned! Please assign the Player in the Inspector.", this);
            enabled = false; // Disable the script if player is not assigned
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("GameManager: No Spawn Points assigned! Please assign Transform objects to the 'Spawn Points' array in the Inspector.", this);
            enabled = false; // Disable the script if no spawn points are assigned
            return;
        }

        // Teleport player to the first spawn point on game start (optional, but good for debugging)
        TeleportPlayer(0);
    }

    void Update()
    {
        // Check for key presses to teleport the player
        HandleInput();
    }

    /// <summary>
    /// Handles keyboard input for teleporting the player to different spawn points.
    /// Keys '1', '2', '3', '4' correspond to spawn points at index 0, 1, 2, and 3 respectively.
    /// </summary>
    private void HandleInput()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return; // Do nothing if no spawn points are set
        }

        // Teleport to spawn point 1 (index 0)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TeleportPlayer(0);
        }
        // Teleport to spawn point 2 (index 1)
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TeleportPlayer(1);
        }
        // Teleport to spawn point 3 (index 2)
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TeleportPlayer(2);
        }
        // Teleport to spawn point 4 (index 3)
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TeleportPlayer(3);
        }
        // Add more else if blocks for more spawn points if needed (e.g., Alpha5, Alpha6, etc.)
        // You could also implement cycling through spawn points:
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
            Debug.LogWarning("GameManager: Cannot teleport player. Player GameObject is not assigned.");
            return;
        }

        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
        {
            Debug.LogWarning($"GameManager: Invalid spawn point index {index}. Please ensure the index is within the bounds of the 'Spawn Points' array.", this);
            return;
        }

        // Set the player's position to the spawn point's position
        player.transform.position = spawnPoints[index].position;
        Debug.Log($"Player teleported to Spawn Point {index + 1} at position: {spawnPoints[index].position}");

        // If your player has a Rigidbody, you might want to reset its velocity
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // If your player has a CharacterController, you might need to handle it differently
        // CharacterController cc = player.GetComponent<CharacterController>();
        // if (cc != null)
        // {
        //     // For CharacterController, directly setting position might not work as expected
        //     // You might need to disable/enable it, or use cc.Move() with a large vector to force position
        //     // For simple teleportation, directly setting transform.position usually suffices if no movement is happening
        //     // If issues arise, consider disabling/enabling the CharacterController around the teleport
        // }
    }
}
