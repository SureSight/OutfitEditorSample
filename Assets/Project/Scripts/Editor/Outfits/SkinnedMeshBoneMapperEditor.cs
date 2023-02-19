using UnityEngine;
using UnityEditor;
using OutfitEditorSample.Outfits;

namespace OutfitEditorSample.Editor.Outfits
{
	[CustomEditor(typeof(SkinnedMeshBoneMapper))]
	public class SkinnedMeshBoneMapperEditor : UnityEditor.Editor
	{
		private bool _isExpanded;

		public override void OnInspectorGUI()
		{
			SkinnedMeshBoneMapper data = (SkinnedMeshBoneMapper)target;

			GUIContent content = new GUIContent("Root Bone Name", "Override the root bone for this item");
			data.RootBoneName = EditorGUILayout.TextField(content, data.RootBoneName);

			_isExpanded = EditorGUILayout.Foldout(_isExpanded, "Bones");

			if (_isExpanded)
			{
				EditorGUI.indentLevel = 1;
				for (int i = 0; i < data.boneNames.Length; i++)
				{
					EditorGUILayout.LabelField(data.boneNames[i]);
				}
				EditorGUI.indentLevel = 0;
			}

			if (GUILayout.Button("Build Bones"))
			{
				if (data.TryMapBones())
				{
                    //expand list of bones if mapping was successful
					_isExpanded = true;
                }
			}
		}
	}
}