using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UIRangeSliderNamespace {
	[CustomEditor(typeof(UIRangeSlider), true)]
	[CanEditMultipleObjects]
	public class UIRangeSliderEditor : SelectableEditor {
		SerializedProperty m_Direction,
				m_FillRect,
				m_MaxHandleRect,
				m_MinHandleRect,
				m_MinLimit,
				m_MaxLimit,
				m_WholeNumbers,
				m_MaxValue,
				m_MinValue,
				m_OnValuesChanged,
				m_OnMaxValueChanged,
				m_OnMinValueChanged,
				m_moveOnlyByHandles;

		protected override void OnEnable() {
			base.OnEnable();
			m_FillRect = serializedObject.FindProperty("m_FillRect");
			m_MinHandleRect = serializedObject.FindProperty("m_MinHandleRect");
			m_MaxHandleRect = serializedObject.FindProperty("m_MaxHandleRect");
			m_Direction = serializedObject.FindProperty("m_Direction");
			m_MinLimit = serializedObject.FindProperty("m_MinLimit");
			m_MaxLimit = serializedObject.FindProperty("m_MaxLimit");
			m_WholeNumbers = serializedObject.FindProperty("m_WholeNumbers");
			m_MaxValue = serializedObject.FindProperty("m_MaxValue");
			m_MinValue = serializedObject.FindProperty("m_MinValue");
			m_OnValuesChanged = serializedObject.FindProperty("m_OnValuesChanged");
			m_OnMaxValueChanged = serializedObject.FindProperty("m_OnMaxValueChanged");
			m_OnMinValueChanged = serializedObject.FindProperty("m_OnMinValueChanged");
			m_moveOnlyByHandles = serializedObject.FindProperty("m_moveOnlyByHandles");
		}

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			EditorGUILayout.Space();

			serializedObject.Update();

			EditorGUILayout.PropertyField(m_FillRect);
			EditorGUILayout.PropertyField(m_MinHandleRect);
			EditorGUILayout.PropertyField(m_MaxHandleRect);

			if (m_FillRect.objectReferenceValue != null || m_MinHandleRect.objectReferenceValue != null || m_MaxHandleRect.objectReferenceValue != null) {
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(m_Direction);
				if (EditorGUI.EndChangeCheck()) {
					UIRangeSlider.Direction direction = (UIRangeSlider.Direction)m_Direction.enumValueIndex;
					foreach (var obj in serializedObject.targetObjects) {
						UIRangeSlider slider = obj as UIRangeSlider;
						slider.SetDirection(direction, true);
					}
				}

				EditorGUI.BeginChangeCheck();
				float newMin = EditorGUILayout.FloatField("Min Limit", m_MinLimit.floatValue);
				if (EditorGUI.EndChangeCheck()) {
					if (m_WholeNumbers.boolValue ? Mathf.Round(newMin) < m_MaxLimit.floatValue : newMin < m_MaxLimit.floatValue)
						m_MinLimit.floatValue = newMin;
				}

				EditorGUI.BeginChangeCheck();
				float newMax = EditorGUILayout.FloatField("Max Limit", m_MaxLimit.floatValue);
				if (EditorGUI.EndChangeCheck()) {
					if (m_WholeNumbers.boolValue ? Mathf.Round(newMax) > m_MinLimit.floatValue : newMax > m_MinLimit.floatValue)
						m_MaxLimit.floatValue = newMax;
				}

				EditorGUILayout.PropertyField(m_WholeNumbers);
				EditorGUILayout.PropertyField(m_moveOnlyByHandles);

				bool areMinMaxEqual = (m_MinLimit.floatValue == m_MaxLimit.floatValue);

				if (areMinMaxEqual)
					EditorGUILayout.HelpBox("Min Limit and Max Limit cannot be equal.", MessageType.Warning);

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Min Max Value", GUILayout.MinWidth(120));

				EditorGUI.BeginChangeCheck();
				var newMinValue = EditorGUILayout.FloatField(m_MinValue.floatValue, GUILayout.Width(50));
				if (EditorGUI.EndChangeCheck()) {
					if (m_WholeNumbers.boolValue ? Mathf.Round(newMinValue) <= m_MaxValue.floatValue : newMinValue <= m_MaxValue.floatValue)
						m_MinValue.floatValue = newMinValue;
				}

				var minValue = m_MinValue.floatValue;
				var maxValue = m_MaxValue.floatValue;
				EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, m_MinLimit.floatValue, m_MaxLimit.floatValue, GUILayout.MinWidth(50));
				m_MinValue.floatValue = minValue;
				m_MaxValue.floatValue = maxValue;

				EditorGUI.BeginChangeCheck();
				var newMaxValue = EditorGUILayout.FloatField(m_MaxValue.floatValue, GUILayout.Width(50));
				if (EditorGUI.EndChangeCheck()) {
					if (m_WholeNumbers.boolValue ? Mathf.Round(newMaxValue) >= m_MinValue.floatValue : newMaxValue >= m_MinValue.floatValue)
						m_MaxValue.floatValue = newMaxValue;
				}

				EditorGUILayout.EndHorizontal();

				bool warning = false;
				foreach (var obj in serializedObject.targetObjects) {
					UIRangeSlider slider = obj as UIRangeSlider;
					UIRangeSlider.Direction dir = slider.direction;
					if (dir == UIRangeSlider.Direction.LeftToRight || dir == UIRangeSlider.Direction.RightToLeft)
						warning = (slider.navigation.mode != Navigation.Mode.Automatic && (slider.FindSelectableOnLeft() != null || slider.FindSelectableOnRight() != null));
					else
						warning = (slider.navigation.mode != Navigation.Mode.Automatic && (slider.FindSelectableOnDown() != null || slider.FindSelectableOnUp() != null));
				}

				if (warning)
					EditorGUILayout.HelpBox("The selected slider direction conflicts with navigation. Not all navigation options may work.", MessageType.Warning);

				// Draw the event notification options
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(m_OnValuesChanged);
				EditorGUILayout.PropertyField(m_OnMaxValueChanged);
				EditorGUILayout.PropertyField(m_OnMinValueChanged);
			}
			else {
				EditorGUILayout.HelpBox("Specify a RectTransform for the slider fill or the slider handle or both. Each must have a parent RectTransform that it can slide within.",
						MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}