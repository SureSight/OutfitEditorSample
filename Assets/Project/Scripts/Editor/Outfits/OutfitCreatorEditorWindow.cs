using UnityEngine;
using UnityEditor;
using OutfitEditorSample.Models.Outfits;
using OutfitEditorSample.Outfits;
using OutfitEditorSample.Utilities;

namespace OutfitEditorSample.Editor.Outfits
{
    /// <summary>
    /// Takes an FBX and converts selected renderers to clothing prefabs that are wired up for use by Object Pool
    /// Also creates or modifies an OutfitLibrary with the clothing prefabs
    /// </summary>
    public class OutfitCreatorEditorWindow : EditorWindow
    {
        private const string DataFolder = "Assets/Project/Data";
        private const string PrefabFolder = "Assets/Project/Prefabs/Outfits";

        [SerializeField]
        private GameObject _outfitFbx;        
        [SerializeField]
        private OutfitLibrary _outfitLibrary;
        [SerializeField]
        private bool _isCreateNewOutfit;
        [SerializeField]
        private Material _prefabMaterial;
        [SerializeField]
        private string _prefabFolder;
        [SerializeField]
        private string _outfitName;

        private SerializedObject _serializedObject;
        private SerializedProperty _outfitFbxProperty;
        private bool[] _outfitFbxSelected;        

        [MenuItem("Tools/Outfit Editor Sample/Outfit Creator", false, -1)]
        public static OutfitCreatorEditorWindow OpenOutfitCreationEditor()
        {
            var window = GetWindow<OutfitCreatorEditorWindow>("Outfit Creator");            
            window.minSize = new Vector2(320, 480);
            return window;
        }

        public void SetOutfitFbx(GameObject outfitFbx = null)
        {
            if (outfitFbx)
            {
                _outfitFbx = outfitFbx;
            }

            _serializedObject = new SerializedObject(this);
            _outfitFbxProperty = _serializedObject.FindProperty(nameof(_outfitFbx));
        }

        private void OnEnable()
        {
            if (_serializedObject == null)
            {
                SetOutfitFbx();
            }
        }

        private void OnDisable() 
        {
            //Clear saved values for clean state the next time this editor is opened
            _serializedObject = null;
            _outfitFbx = null;
            _isCreateNewOutfit = false;
            _outfitLibrary = null;
            _prefabFolder = "";
            _prefabFolder = null;
            _outfitName = "";            
            _outfitFbxSelected = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Creates outfit items and an outfit library from an FBX file.", MessageType.Info);
            EditorGUILayout.Space(10);

            //Outfit FBX
            EditorGUILayout.PropertyField(_outfitFbxProperty, new GUIContent("Outfit FBX*", "The model FBX file that contains meshes to convert to outfit clothing items."));

            //Detect when the selected FBX is changed
            if (_outfitFbxProperty.objectReferenceValue != _outfitFbx)
            {
                _outfitFbxSelected = null;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

            //Prefab Folder
            EditorGUILayout.BeginHorizontal();
            var prefabFolderProperty = _serializedObject.FindProperty(nameof(_prefabFolder));
            EditorGUILayout.PropertyField(prefabFolderProperty, new GUIContent("Prefab Folder*", "The folder that the newly created clothing prefabs will be saved to."));

            //Button to open folder dialog to pick Prefab Folder
            if (GUILayout.Button("...", GUILayout.Width(24)))
            {
                //remove focus from prefabFolder field otherwise it will not update with result from dialog
                GUI.FocusControl(null);

                //Open folder dialog to select folder
                string folder = EditorUtility.OpenFolderPanel("Prefab Folder", PrefabFolder, "");
                
                //Get relative path to Assets folder
                int index = folder.LastIndexOf("Assets");                
                string subfolder = folder.Substring(index);
                prefabFolderProperty.stringValue = subfolder;
            }
            EditorGUILayout.EndHorizontal();

            //Prefab Material
            var materialProperty = _serializedObject.FindProperty(nameof(_prefabMaterial));
            EditorGUILayout.PropertyField(materialProperty, new GUIContent("Prefab Material"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Outfit Library", EditorStyles.boldLabel);
            _isCreateNewOutfit = EditorGUILayout.ToggleLeft("Create new outfit", _isCreateNewOutfit);
            
            if (_isCreateNewOutfit)
            {
                //Outfit Name
                //Default name to selected model file
                if (string.IsNullOrEmpty(_outfitName) && _outfitFbx != null)
                {
                    _outfitName = $"{_outfitFbx.name}Library";
                }

                var outfitNameProperty = _serializedObject.FindProperty(nameof(_outfitName));
                EditorGUILayout.PropertyField(outfitNameProperty,new GUIContent("Outfit Name*"));
            }
            else
            {
                //Outfit Library
                var outfitProperty = _serializedObject.FindProperty(nameof(_outfitLibrary));
                EditorGUILayout.PropertyField(outfitProperty, new GUIContent("Outfit Library*", "The existing outfit to update.  Edits the the first outfit level with the imported items."));
            }

            //Save changes to _serializedObject
            _serializedObject.ApplyModifiedProperties();

            if (_outfitFbx != null)
            {
                EditorGUILayout.Space(10);

                //Displayt list of renderers to select from
                var renderers = _outfitFbx.GetComponentsInChildren<Renderer>(true);
                int count = renderers.Length;
                if (_outfitFbxSelected == null)
                {
                    _outfitFbxSelected = new bool[count];
                }

                if (count > 0)
                {
                    EditorGUILayout.LabelField("Renderers", EditorStyles.boldLabel);
                    for (int i = 0; i < count; i++)
                    {
                        var renderer = renderers[i];

                        _outfitFbxSelected[i] = EditorGUILayout.ToggleLeft(renderer.name, _outfitFbxSelected[i]);
                    }

                    EditorGUILayout.Space(10);

                    string buttonText = _isCreateNewOutfit ? "Create Outfit" : "Update Outfit";
                    if (GUILayout.Button(buttonText))
                    {
                        bool isValid = ValidateInput();
                        if (!isValid) 
                        {
                            return;
                        }

                        if (_isCreateNewOutfit)
                        {
                            CreateOutfitLibrary();
                        }
                        
                        CreateOutfitPrefabs(renderers, count);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("There are no meshes in this file", MessageType.Error, true);
                }
            }
        }

        /// <summary>
        /// Create a new OutfitLibrary
        /// </summary>
        private void CreateOutfitLibrary()
        {
            _outfitLibrary = ScriptableObject.CreateInstance<OutfitLibrary>();
            _outfitLibrary._outfitName = _outfitName.Trim();

            string libraryName = _outfitName.EndsWith("Library") ? _outfitName : $"{_outfitName}Library";
            string assetPath = GetAssetPath(DataFolder, "Outfits", libraryName, "asset");

            AssetDatabase.CreateAsset(_outfitLibrary, assetPath);
            AssetDatabase.SaveAssets();
        }

        private void CreateOutfitPrefabs(Renderer[] renderers, int rendererCount)
        {
            //Split prefabFolder into 
            int lastIndex = _prefabFolder.LastIndexOf('/');
            string parentFolder = _prefabFolder.Substring(0, lastIndex);
            string subfolder = _prefabFolder.Substring(lastIndex);

            for (int i = 0; i < rendererCount; i++)
            {
                if (_outfitFbxSelected[i])
                {
                    GameObject tempGameObject = null;
                    try
                    {
                        var renderer = renderers[i];                        
                        string prefabName = $"prefab{renderer.name}".Replace("_", string.Empty);
                        if (renderer is SkinnedMeshRenderer)
                        {
                            tempGameObject = GameObject.Instantiate(renderer.gameObject);

                            //Add SkinnedMeshBoneMapper and automap bones
                            if (!tempGameObject.TryGetComponent<SkinnedMeshBoneMapper>(out SkinnedMeshBoneMapper boneMapper))
                            {
                                boneMapper = tempGameObject.AddComponent<SkinnedMeshBoneMapper>();
                            }
                        }
                        else
                        {
                            //MeshRenderer
                            //These are usually hats and eyewear that need to be created as a child for correct positioning
                            tempGameObject = new GameObject(prefabName);

                            var childObject = GameObject.Instantiate(renderer.gameObject, tempGameObject.transform);
                            childObject.layer = LayerUtility.LayerNpc;
                        }

                        tempGameObject.layer = LayerUtility.LayerNpc;

                        if (_prefabMaterial != null)
                        {
                            renderer.sharedMaterial = _prefabMaterial;
                        }

                        //Save new prefab for clothing item                        
                        string assetPath = GetAssetPath(parentFolder, subfolder, prefabName, "prefab");           
                        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(tempGameObject, assetPath);

                        //Add clothing prefab to default outfit
                        _outfitLibrary.dataItems.Add(newPrefab);
                    }
                    finally
                    {
                        DestroyImmediate(tempGameObject);
                    }
                }
            }

            //Highlight OutfitLibrary in editor window
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = _outfitLibrary;
        }

        private string GetAssetPath(string parentFolderPath, string folderPath, string prefabName, string extension)
        {
            string trimmedFolderPath = folderPath.Trim();
            string folder = $"{parentFolderPath}/{trimmedFolderPath}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parentFolderPath, trimmedFolderPath);
            }

            //Generate combined path that is unique
            string assetPath = $"{folder}/{prefabName}.{extension}";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            return assetPath;
        }

        /// <summary>
        /// Returns false if one or more fields have not been filled out correctly
        /// </summary>
        /// <returns></returns>
        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(_prefabFolder.Trim()))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a Prefab Folder.", "OK");
                return false;
            }

            if (_isCreateNewOutfit) 
            {
                if (string.IsNullOrEmpty(_outfitName.Trim()))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a Prefab Folder.", "OK");
                    return false;
                }
            }
            else
            {
                if (_outfitLibrary == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a Outfit Library to update.", "OK");
                    return false;
                }
            }

            //Validate Renderers
            if (!IsRendererSelected())
            {
                EditorUtility.DisplayDialog("Error", "Please select at least 1 x Renderer.", "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if at least 1 x renderer is selected
        /// </summary>
        /// <returns></returns>
        private bool IsRendererSelected()
        {
            if (_outfitFbxSelected == null)
            {
                return false;
            }

            int count = _outfitFbxSelected.Length;
            for (int i = 0; i < count; i++)
            {
                bool isSelected = _outfitFbxSelected[i];
                if (isSelected)
                {
                    return true;
                }
            }

            //Did not find a selected entry
            return false;
        }
    }
}