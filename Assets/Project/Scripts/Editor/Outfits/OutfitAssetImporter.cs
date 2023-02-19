using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace OutfitEditorSample.Editor.Outfits
{
    public class OutfitAssetImporter : AssetPostprocessor
    {
        /// <summary>
        /// This assumes that any FBX files dropped in this folder are to be processed
        /// </summary>
        private const string OutfitModelPath = "Assets/Project/Models/Outfits";
        /// <summary>
        /// This is where to find the base mesh with the same skeleton as in the outfit FBX
        /// </summary>
        private const string AvatarPath = "Assets/Models/CharacterBaseMesh.FBX";

        private void OnPreprocessModel()
        {
            if (assetPath.StartsWith(OutfitModelPath))
            {
                var importer = (ModelImporter)assetImporter;
                if (!importer.importSettingsMissing)
                {
                    //Abort as this model has already been imported
                    return;
                }

                //Set scale factor
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;

                //Set rig to Mecanim rig with base mesh Avatar
                importer.animationType = ModelImporterAnimationType.Human;
                importer.skinWeights = ModelImporterSkinWeights.Standard;

                Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
                Assert.IsNotNull(avatar, $"Could not find base mesh avatar at {AvatarPath}");
                
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            int count = importedAssets.Length;
            for (int i = 0; i < count; i++)
            {
                //Automatically open the editor for this outfit FBX
                string assetPath = importedAssets[i];
                if (assetPath.Contains(OutfitModelPath))
                {
                    //Load the FBX model's asset now that it has completed importing
                    GameObject importedFbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    Assert.IsNotNull(importedFbx, $"Could not find FBX at {assetPath}");

                    //Open editor window and preselect the imported FBX model
                    var window = OutfitCreatorEditorWindow.OpenOutfitCreationEditor();
                    window.Show();
                    window.SetOutfitFbx(importedFbx);

                    //Abort because the editor can only handle one FBX at a time
                    return;
                }
            }
        }
    }
}