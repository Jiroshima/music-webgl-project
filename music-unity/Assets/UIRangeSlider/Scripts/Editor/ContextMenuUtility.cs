using UnityEditor;
namespace UIRangeSliderNamespace  {
	public static class ContextMenuUtility {
		private const string ELEMENT_NAME_IN_RESOURCES = "UIRangeSlider"; 
		
		[MenuItem("GameObject/UIComponents/RangeSlider")]
		public static void CreateSwitcher(MenuCommand menuCommand) {
			CreateUtility.CreateUIElement(ELEMENT_NAME_IN_RESOURCES);
		}
	}
}