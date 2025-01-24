	using System;
	using TMPro;
	using UnityEngine;

	namespace UIMinMaxSliderExamples {
		using UIRangeSliderNamespace;
		public class LabeledSlider : MonoBehaviour {
			[SerializeField] private UIRangeSlider slider;
			[SerializeField] private TMP_Text minValue, maxValue;

			private void SetValues(float min, float max) {
				minValue.text = min.ToString();
				maxValue.text = max.ToString();
			}

			private void OnEnable() {
				slider.onValuesChanged.AddListener(SetValues);
			}

			private void OnDisable() {
				slider.onValuesChanged.RemoveListener(SetValues);
			}

			private void Awake() {
				SetValues(slider.valueMin, slider.valueMax);
			}

		}
	}

