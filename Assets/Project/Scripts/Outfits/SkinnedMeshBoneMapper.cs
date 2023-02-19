using System.Linq;
using UnityEngine;

namespace OutfitEditorSample.Outfits
{
    public class SkinnedMeshBoneMapper : MonoBehaviour
    {
        public string RootBoneName;
        public string[] boneNames = new string[0];

        private void Reset()
        {
            //Autoapply the bone mapping when script is added to gameobject or editor Reset
            TryMapBones();
        }

        /// <summary>
        /// Maps all bones for a SkinnedMeshRenderer to an array of strings and retains RootBoneName
        /// </summary>
        /// <returns></returns>
        public bool TryMapBones()
        {
            //Scan bones for Skinned MeshRendere and add names
            if (TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer renderer))
            {
                RootBoneName = renderer.rootBone.name;

                boneNames = renderer.bones
                    .Where(t => t != null)
                    .Select(t => t.name)
                    .ToArray();

                return true;
            }
            else
            {
                Debug.Log("There are no SkinnedMeshRenderers attached to " + name);
            }

            return false;
        }
    }
}