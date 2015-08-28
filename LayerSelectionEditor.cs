using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[InitializeOnLoad]
public class LayerSelectionEditor :ScriptableWizard
{

    [System.Serializable]
    public class LayerSelected
    {
        public string name;
        public bool enabled;
    }

    public List<LayerSelected> Layers = new List<LayerSelected>();
    [MenuItem("Tools/Layer Selection")]
    static void CreateWizardFromMenu()
    {
        var helper = DisplayWizard<LayerSelectionEditor>("Layer Selection", "Select", "Cancel");
        

        foreach (var s in UnityEditorInternal.InternalEditorUtility.layers)
        {
            LayerSelected l = new LayerSelected();
            l.enabled = false;
            l.name = s;
            helper.Layers.Add(l);
        }

        helper.minSize = new Vector2(300, 50 + (20 * UnityEditorInternal.InternalEditorUtility.layers.Length));
        helper.maxSize = new Vector2(300, 50 + (20 * UnityEditorInternal.InternalEditorUtility.layers.Length));
    }


    void OnGUI()
    {

        foreach (var l in Layers)
        {
            LayersEditor(l);
        }
        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select"))
        {
            Select();
            Close();
        }
        if (GUILayout.Button("Cancel"))
        {
            Close();
        }

      
        EditorGUILayout.EndHorizontal();

    }
    void Traverse(GameObject obj, ref List<GameObject> all )
    {
        foreach (Transform child in obj.transform)
        {
            Traverse(child.gameObject, ref all);
        }

        all.Add(obj);

    }
    private void Select()
    {
        List<GameObject> allObjectsInScene = new List<GameObject>();

        // var objs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep);
        ////Object.FindObjectsOfType(typeof(GameObject)))
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll(typeof(GameObject)))
        {
            Debug.Log(obj.name);
            if (obj.transform.parent == null)
            {
                Traverse(obj, ref allObjectsInScene);
            }
        }


        var list = new List<Object>(allObjectsInScene.Count);
        foreach (var l in Layers)
        {
            if (l.enabled)
            {
                foreach (var obj in allObjectsInScene)
                {
                    var go = obj as GameObject;

                    if (go == null) continue;
                    if (LayerMask.LayerToName( go.layer) == l.name)
                    {
                        list.Add(go);
                    }
                }
            }

        }

        Selection.objects = list.ToArray();
    }
    private void LayersEditor(LayerSelected layer)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(layer.name);
        layer.enabled = EditorGUILayout.Toggle(layer.enabled);

        EditorGUILayout.EndHorizontal();

    }

   
    // Called when the 'save' button is pressed
    void OnWizardCreate()
    {
        // .Net 2.0 Subset: smcs.rsp
        // .Net 2.0: gmcs.rsp
        // -define:debug;poop
    }


    void OnWizardOtherButton()
    {
        this.Close();
    }
}
