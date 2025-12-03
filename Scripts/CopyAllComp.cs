using UnityEngine;
using UnityEditor;
using System.Linq;

public class ComponentCopier : EditorWindow
{
    private GameObject sourceObject;
    private GameObject[] targetObjects;

    [MenuItem("Tools/Copy Components to Multiple Objects")]
    public static void ShowWindow()
    {
        GetWindow<ComponentCopier>("Component Copier");
    }

    void OnGUI()
    {
        GUILayout.Label("Copy Components", EditorStyles.boldLabel);
        
        sourceObject = (GameObject)EditorGUILayout.ObjectField(
            "Source Object (with components)", 
            sourceObject, 
            typeof(GameObject), 
            true
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Select target objects in Hierarchy, then click button");
        
        if (GUILayout.Button("Copy Components to Selected Objects"))
        {
            CopyComponents();
        }
    }

    void CopyComponents()
    {
        if (sourceObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a source object", "OK");
            return;
        }

        targetObjects = Selection.gameObjects;
        
        if (targetObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select target objects in Hierarchy", "OK");
            return;
        }

        // Begin Undo grouping so we can undo the whole batch operation
        Undo.IncrementCurrentGroup();
        int undoGroupIndex = Undo.GetCurrentGroup();

        int count = 0;

        foreach (GameObject target in targetObjects)
        {
            if (target == sourceObject) continue;

            // Get all components except Transform and MeshFilter/MeshRenderer
            var components = sourceObject.GetComponents<Component>()
                .Where(c => !(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer));

            foreach (var component in components)
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                
                if (UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target))
                {
                    // === NEW LOGIC: Handle Mesh Colliders ===
                    if (component is MeshCollider)
                    {
                        // Since PasteComponentAsNew puts the component at the end, 
                        // we grab the last MeshCollider on the target.
                        MeshCollider[] targetColliders = target.GetComponents<MeshCollider>();
                        MeshCollider newCollider = targetColliders[targetColliders.Length - 1];

                        // Look for a MeshFilter on the target object
                        MeshFilter targetMeshFilter = target.GetComponent<MeshFilter>();

                        if (targetMeshFilter != null && targetMeshFilter.sharedMesh != null)
                        {
                            // Register this specific change for Undo
                            Undo.RecordObject(newCollider, "Assign Mesh to Collider");
                            
                            // Assign the target's own mesh to the collider
                            newCollider.sharedMesh = targetMeshFilter.sharedMesh;
                        }
                    }
                }
            }
            count++;
        }

        Undo.CollapseUndoOperations(undoGroupIndex);
        Debug.Log($"Copied components to {count} objects");
    }
}