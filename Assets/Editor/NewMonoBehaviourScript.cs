using UnityEditor;
using UnityEngine;

public class AddMeshColliders : MonoBehaviour
{
    [MenuItem("Tools/Add MeshColliders To All")]
    static void AddColliders()
    {
        int count = 0;
        foreach (MeshRenderer mr in FindObjectsOfType<MeshRenderer>())
        {
            GameObject go = mr.gameObject;
            if (go.GetComponent<MeshCollider>() == null)
            {
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    count++;
                }
            }
        }
        Debug.Log($"Added MeshCollider to {count} objects.");
    }
}
