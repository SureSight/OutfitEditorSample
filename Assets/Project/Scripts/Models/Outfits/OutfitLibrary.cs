using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace OutfitEditorSample.Models.Outfits
{
	[CreateAssetMenu(fileName = "NewOutfit", menuName = "Outfit Editor Sample/Outfit Library")]
	public class OutfitLibrary : ScriptableObject
	{
		public int Count => dataItems.Count;

		//TODO: Make these private SerializeField
        public string _outfitName;        
		public List<GameObject> dataItems = new List<GameObject>();

        private void OnValidate()
        {
			Assert.IsFalse(string.IsNullOrEmpty(_outfitName), $"{nameof(_outfitName)} must be assigned");
        }
    }
}