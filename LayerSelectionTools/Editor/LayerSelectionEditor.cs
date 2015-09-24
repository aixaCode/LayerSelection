using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using System.IO;
using Random = UnityEngine.Random;
using System.Runtime.Serialization.Formatters.Binary;

namespace gbFactory.Tools
{
    [InitializeOnLoad]
    public class LayerSelectionEditor : EditorWindow
    {
        #region Used classes
        [System.Serializable]
        public class LayerSelected : ISerializable
        {
            public string name;
            public bool enabled;
            public Color color;
            public bool activeIcon;

            public LayerSelected()
            { }


            protected LayerSelected(SerializationInfo info, StreamingContext context)
            {
                name = info.GetString("name");
                enabled = info.GetBoolean("enabled");
                activeIcon = info.GetBoolean("activeIcon");
                color.r = (float)info.GetDecimal("colorR");
                color.g = (float)info.GetDecimal("colorG");
                color.b = (float)info.GetDecimal("colorB");
                color.a = (float)info.GetDecimal("colorA");
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("name", name);
                info.AddValue("enabled", enabled);
                info.AddValue("activeIcon", activeIcon);
                info.AddValue("colorR", color.r);
                info.AddValue("colorG", color.g);
                info.AddValue("colorB", color.b);
                info.AddValue("colorA", color.a);
            }
        }
        #endregion

        #region Public variables
        public static Dictionary<string, LayerSelected> Layers = new Dictionary<string, LayerSelected>();
        #endregion
        #region private variables
        private const string PrefsKey = "LayerPref";
        private static bool iconsVisible = false;
        private static bool foldOut = false;
        private static bool selectAll = false;
        private static bool selectAllPrev = false;
        private static bool iconsVisiblePrev = false;
        #endregion
        [MenuItem("Tools/Layer Selection Tools")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(LayerSelectionEditor), false, "Layer Selection Tools");
        }

        #region EditorWindow Build-in functions
        void OnEnable()
        {
            if (EditorPrefs.HasKey(PrefsKey))
            {
                Load();
            }
            else
            {
                CreateFirstTime();
            }

        }

        void OnGUI()
        {
            GUILayout.Label("Layers", EditorStyles.boldLabel);

            GUILayout.Space(5);
            GroupSettingsEditor();
            GUILayout.Space(5);

            GUILayout.Label("Individual settings");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(115));
            GUILayout.Label("Select", EditorStyles.boldLabel, GUILayout.Width(55));
            GUILayout.Label("Color", EditorStyles.boldLabel, GUILayout.Width(65));
            GUILayout.Label("Icon", EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 1; 
            foreach (var l in Layers.Values)
            {
                LayersEditor(l);
            }

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select", GUILayout.Width(150)))
            {
                Select();
            }
            if (GUILayout.Button("Reset",GUILayout.Width(150)))
            {
                SetLayersSelection(false);
            }
            EditorGUILayout.EndHorizontal();

        }
        private void GroupSettingsEditor()
        {
            if (foldOut = EditorGUILayout.Foldout(foldOut, "Group settings"))
            {
                EditorGUI.indentLevel = 2;

                if (selectAll = EditorGUILayout.Toggle("Select all layers", selectAll)) { Save(); }
                if (selectAll != selectAllPrev)
                {
                    selectAllPrev = selectAll;
                    SetLayersSelection(selectAll);
                }

                if (iconsVisible = EditorGUILayout.Toggle("Show all icons", iconsVisible)) { Save(); }
                if (iconsVisible != iconsVisiblePrev)
                { 
                    iconsVisiblePrev = iconsVisible;
                    SetLayersIconVisibility(iconsVisible);
                }
            }
        }

        private void LayersEditor(LayerSelected layer)
        {
            EditorGUILayout.BeginHorizontal();

            var style = new GUIStyle();
            style.fontStyle = layer.enabled ? FontStyle.Bold : FontStyle.Normal;
            
            EditorGUILayout.LabelField(layer.name, style, GUILayout.Width(110));
            if (layer.enabled = EditorGUILayout.Toggle(layer.enabled, GUILayout.Width(50))) { Save(); }
            layer.color = EditorGUILayout.ColorField(layer.color, GUILayout.Width(60));

            if (layer.activeIcon = EditorGUILayout.Toggle(layer.activeIcon,GUILayout.Width(50)))
            {
                EditorApplication.RepaintHierarchyWindow();
                Save();
            }

            EditorGUILayout.EndHorizontal();

        }


        void OnFocus()
        {
            EditorApplication.RepaintHierarchyWindow();
            Save();
            if (Layers.Count != UnityEditorInternal.InternalEditorUtility.layers.Length)
            {
                Load();
            }
            Save();
        }

        void OnDestroy()
        {
            Save();
        }
        #endregion

        #region Functionality

        private void SetLayersSelection(bool select)
        {
            foreach (var s in Layers.Values)
            {
                s.enabled = select;
            }
        }
        private void SetLayersIconVisibility(bool select)
        {
            foreach (var s in Layers.Values)
            {
                s.activeIcon = select;
            }
        }
        //Looks for gameobjects in hierarchy, traversing through every node down
        void Traverse(GameObject obj, ref List<GameObject> all)
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

            //Other possible ways of searching available gameObjects.
            //- need to select first in hierarchy, then it's selecting from those
            // var objs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep); 

            // - will not select inactive objects
            //Object.FindObjectsOfType(typeof(GameObject)))

            // - looks also in asset folder
            //Resources.FindObjectsOfTypeAll(typeof(GameObject)))

            foreach (GameObject obj in GameObject.FindObjectsOfType(typeof(GameObject))) 
            {
                if (obj.transform.parent == null)
                {
                    Traverse(obj, ref allObjectsInScene);
                }
            }

            var list = new List<GameObject>(allObjectsInScene.Count);
            foreach (var l in Layers.Values)
            {
                if (l.enabled)
                {
                    foreach (var obj in allObjectsInScene)
                    {
                        var go = obj as GameObject;

                        if (go == null) continue;
                        if (LayerMask.LayerToName(go.layer) == l.name)
                        {
                            list.Add(go);
                        }
                    }
                }
            }

            Selection.objects = list.ToArray();
        }
    
        private void Save()
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, Layers);
                var data = System.Convert.ToBase64String(stream.ToArray());
                stream.Close();

                EditorPrefs.SetString(PrefsKey, data);
            }
        }

        private void Load()
        {
            //copy of Layers, to remove unused form previous save
            Dictionary<string, LayerSelected> tempLayers = new Dictionary<string, LayerSelected>();
            try
            {
                var data = EditorPrefs.GetString(PrefsKey);
                var bytes = System.Convert.FromBase64String(data);
                var stream = new MemoryStream(bytes);

                var formatter = new BinaryFormatter();
                tempLayers = (Dictionary<string, LayerSelected>)formatter.Deserialize(stream);

                Layers.Clear();

                if (tempLayers.Count == 0)
                {
                    CreateFirstTime();
                    return;
                }
                //Remove rows from loaded data (Layers), that are not specified in Editor's layers
                foreach (var s in UnityEditorInternal.InternalEditorUtility.layers)
                {
                    if (tempLayers.ContainsKey(s))
                    {
                        Layers.Add(s, tempLayers[s]);
                        Layers[s].enabled = false;
                    }
                    else
                    {
                        LayerSelected l = CreateNewLayer(s);
                        Layers.Add(l.name, l);
                    }
                }

                
            }
            catch(Exception ex)
            {
                CreateFirstTime();
            }

           
        }
        //Initialized the Layers dictionary
        private void CreateFirstTime()
        {
            foreach (var s in UnityEditorInternal.InternalEditorUtility.layers)
            {
                LayerSelected l = CreateNewLayer(s);
                Layers.Add(l.name, l);
            }
        }

        private LayerSelected CreateNewLayer(string name)
        {
            LayerSelected l = new LayerSelected();
            l.enabled = false;
            l.name = name;
            l.color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
               Random.Range(0.0f, 1.0f));
            return l;
        }
        #endregion
    }


    [InitializeOnLoad]
    class HierarchyIcon
    {
        private static Texture2D texture;
        private static List<int> objectWithLayers;
        private static List<GameObject> selected ;

        static HierarchyIcon()
        {
            // Init
            texture = AssetDatabase.LoadAssetAtPath("Assets/LayerSelectionTools/Testicon.png", typeof(Texture2D)) as Texture2D;
            EditorApplication.update += UpdateLayers;
            EditorApplication.hierarchyWindowItemOnGUI += AddIcon;
        }

        static void UpdateLayers()
        {
            // Check here
            GameObject[] go = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
            objectWithLayers = new List<int>();
            foreach (GameObject g in go)
            {
                objectWithLayers.Add(g.GetInstanceID());
            }
            selected = new List<GameObject>();

        }

        static void AddIcon(int instanceID, Rect selectionRect)
        {
            var instaceIDGameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (instaceIDGameObject == null) return;

            var layer = instaceIDGameObject.layer;
            var layerName = LayerMask.LayerToName(instaceIDGameObject.layer);
            if (LayerSelectionEditor.Layers.ContainsKey(layerName))
            {
                if (!LayerSelectionEditor.Layers[layerName].activeIcon) return;
                //Change color of button
                GUI.color = LayerSelectionEditor.Layers[layerName].color;
                // place the icon to the right of the list:
                Rect r = new Rect(selectionRect);
                r.x = r.width - 20;
                r.width = 40;

                if (GUI.Button(r, texture, GUIStyle.none))
                {
                    selected.Add(instaceIDGameObject);

                    for (int i = 0; i < objectWithLayers.Count; i++)
                    {
                        var obj = (EditorUtility.InstanceIDToObject(objectWithLayers[i]) as GameObject);
                        if (obj.layer == layer)
                        {
                            selected.Add(obj);
                        }
                    }

                    Selection.objects = selected.ToArray();
                }
                GUI.color = Color.white;
            }

        }

    }

}