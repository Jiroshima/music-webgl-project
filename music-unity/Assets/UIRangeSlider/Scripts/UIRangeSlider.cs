using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIRangeSliderNamespace
{
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	public class UIRangeSlider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
	{
		[Serializable] public class ValuesChangedEvent : UnityEvent<float, float> {}
		[Serializable] public class ValueChangedEvent : UnityEvent<float> {}
		
		public ValuesChangedEvent onValuesChanged { 
			get => m_OnValuesChanged;
			set => m_OnValuesChanged = value;
		}
		public ValueChangedEvent onMaxValueChanged { 
			get => m_OnMaxValueChanged;
			set => m_OnMaxValueChanged = value;
		}
		public ValueChangedEvent onMinValueChanged {
			get => m_OnMinValueChanged;
			set => m_OnMinValueChanged = value;
		}
		/*[Serializable]
		public class MinMaxSliderEvent : UnityEvent<float, float> { }

		public MinMaxSliderEvent onValueChanged {
			get => m_OnValueChanged;
			set => m_OnValueChanged = value;
		}*/
		[SerializeField] private ValuesChangedEvent m_OnValuesChanged = new();
		[SerializeField] private ValueChangedEvent m_OnMaxValueChanged = new();
		[SerializeField] private ValueChangedEvent m_OnMinValueChanged = new();

		[SerializeField] protected float m_MinValue;
		[SerializeField] protected float m_MaxValue;
		[SerializeField] private Direction m_Direction = Direction.LeftToRight;
		[SerializeField] private RectTransform m_FillRect;
		[SerializeField] private RectTransform m_MaxHandleRect;
		[SerializeField] private RectTransform m_MinHandleRect;
		[SerializeField] private bool m_WholeNumbers;
		[SerializeField] private float m_MaxLimit = 1;
		[SerializeField] private float m_MinLimit;
		[SerializeField] private bool m_moveOnlyByHandles;

		public enum Direction
		{
			LeftToRight,
			RightToLeft,
			BottomToTop,
			TopToBottom,
		}

		public bool moveOnlyByHandles {
			get => m_moveOnlyByHandles;
			set => m_moveOnlyByHandles = value;
		}

		public RectTransform fillRect {
			get { return m_FillRect; }
			set
			{
				if (m_FillRect == value) return;
				m_FillRect = value;
				UpdateCachedReferences();
				UpdateVisuals();
			}
		}

		public RectTransform maxHandleRect {
			get => m_MaxHandleRect;
			set
			{
				if (m_MaxHandleRect == value) return;
				m_MaxHandleRect = value;
				UpdateCachedReferences();
				UpdateVisuals();
			}
		}

		public RectTransform minHandleRect {
			get => m_MinHandleRect;
			set
			{
				if (m_MinHandleRect == value) return;
				m_MinHandleRect = value;
				UpdateCachedReferences();
				UpdateVisuals();
			}
		}

		public Direction direction {
			get => m_Direction;
			set
			{
				if (m_Direction == value) return;
				m_Direction = value;
				UpdateVisuals();
			}
		}

		public float minLimit {
			get => m_MinLimit;
			set
			{
				m_MinLimit = value;
				Set(m_MinValue, m_MaxValue);
				UpdateVisuals();
			}
		}

		public float maxLimit {
			get => m_MaxLimit;
			set
			{
				m_MaxLimit = value;
				Set(m_MinValue, m_MaxValue);
				UpdateVisuals();
			}
		}

		public virtual float valueMax {
			get => wholeNumbers ? Mathf.Round(m_MaxValue) : m_MaxValue;
			set => Set(m_MinValue, value);
		}

		public virtual float valueMin {
			get => wholeNumbers ? Mathf.Round(m_MinValue) : m_MinValue;
			set => Set(value, m_MaxValue);
		}

		public bool wholeNumbers {
			get => m_WholeNumbers;
			set
			{
				if (m_WholeNumbers == value) return;
				Set(m_MinValue, m_MaxValue);
				UpdateVisuals();
			}
		}

		public float minNormalizedValue {
			get
			{
				if (Mathf.Approximately(minLimit, maxLimit))
					return 0;
				return Mathf.InverseLerp(minLimit, maxLimit, valueMin);
			}
			set { this.valueMin = Mathf.Lerp(minLimit, maxLimit, value); }
		}

		public float maxNormalizedValue {
			get
			{
				if (Mathf.Approximately(minLimit, maxLimit))
					return 0;
				return Mathf.InverseLerp(minLimit, maxLimit, valueMax);
			}
			set { this.valueMax = Mathf.Lerp(minLimit, maxLimit, value); }
		}

		private enum Axis
		{
			Horizontal = 0,
			Vertical = 1
		}

		private Axis axis {
			get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; }
		}

		private bool reverseValue {
			get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; }
		}

		private Image _fillImage;
		private Transform _fillTransform;
		private RectTransform _fillContainerRect;
		private Transform _maxHandleTransform;
		private Transform _minHandleTransform;
		private RectTransform _maxHandleContainerRect;
		private RectTransform _minHandleContainerRect;
		private Vector2 _offset = Vector2.zero;
		private bool _clickedOnMax, _clickedOnMin, _clickedOnFill;
		private float _lengthToMin;
		private float _lengthToMax;
		private float _intervalLength;
		// field is never assigned warning
#pragma warning disable 649
		private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649
		private bool m_DelayedUpdateVisuals;

		private float stepSize {
			get { return wholeNumbers ? 1 : (maxLimit - minLimit) * 0.1f; }
		}

		public virtual void SetValueWithoutNotify(float minValue, float maxValue) =>
			Set(minValue, maxValue, false);

		public virtual void Rebuild(CanvasUpdate executing) {
#if UNITY_EDITOR
			if (executing == CanvasUpdate.Prelayout) {
				onValuesChanged.Invoke(valueMin, valueMax);
				onMaxValueChanged.Invoke(valueMax);
				onMinValueChanged.Invoke(valueMin);
			}
#endif
		}

		public virtual void LayoutComplete() {
		}

		public virtual void GraphicUpdateComplete() {
		}

		protected override void OnEnable() {
			base.OnEnable();
			UpdateCachedReferences();
			Set(m_MinValue, m_MaxValue, false);
			// Update rects since they need to be initialized correctly.
			UpdateVisuals();
		}

		protected override void OnDisable() {
			m_Tracker.Clear();
			base.OnDisable();
		}

		protected virtual void Update() {
			if (m_DelayedUpdateVisuals) {
				m_DelayedUpdateVisuals = false;
				Set(m_MinValue, m_MaxValue, false);
				UpdateVisuals();
			}
		}

		private void UpdateCachedReferences() {
			if (m_FillRect && m_FillRect != (RectTransform)transform) {
				_fillTransform = m_FillRect.transform;
				_fillImage = m_FillRect.GetComponent<Image>();
				if (_fillTransform.parent != null)
					_fillContainerRect = _fillTransform.parent.GetComponent<RectTransform>();
			}
			else {
				m_FillRect = null;
				_fillContainerRect = null;
				_fillImage = null;
			}

			if (m_MaxHandleRect && m_MaxHandleRect != (RectTransform)transform) {
				_maxHandleTransform = m_MaxHandleRect.transform;
				if (_maxHandleTransform.parent != null)
					_maxHandleContainerRect = _maxHandleTransform.parent.GetComponent<RectTransform>();
			}
			else {
				m_MaxHandleRect = null;
				_maxHandleContainerRect = null;
			}

			if (m_MinHandleRect && m_MinHandleRect != (RectTransform)transform) {
				_minHandleTransform = m_MinHandleRect.transform;
				if (_minHandleTransform.parent != null)
					_minHandleContainerRect = _minHandleTransform.parent.GetComponent<RectTransform>();
			}
			else {
				m_MinHandleRect = null;
				_minHandleContainerRect = null;
			}
		}

		private float ClampValueByLimit(float value) {
			float newValue = Mathf.Clamp(value, minLimit, maxLimit);
			if (wholeNumbers)
				newValue = Mathf.Round(newValue);
			return newValue;
		}

		protected virtual void Set(float minValue, float maxValue, bool sendCallback = true) {
			float newMinValue = ClampValueByLimit(minValue);
			float newMaxValue = ClampValueByLimit(maxValue);

			if (m_MaxValue == newMaxValue && m_MinValue == newMinValue)
				return;

			if (m_MaxValue != newMaxValue) {
				m_MaxValue = newMaxValue;
				m_OnMaxValueChanged.Invoke(newMaxValue);
			}

			if (m_MinValue != newMinValue) {
				m_MinValue = newMinValue;
				m_OnMinValueChanged.Invoke(newMinValue);
			}
			
			UpdateVisuals();
			if (sendCallback) {
				UISystemProfilerApi.AddMarker("Slider.value", this);
				m_OnValuesChanged.Invoke(newMinValue, newMaxValue);
			}
		}

		protected override void OnRectTransformDimensionsChange() {
			base.OnRectTransformDimensionsChange();

			if (!IsActive())
				return;

			UpdateVisuals();
		}

		private void UpdateVisuals() {
#if UNITY_EDITOR
			if (!Application.isPlaying)
				UpdateCachedReferences();
#endif

			m_Tracker.Clear();

			if (_fillContainerRect != null) {
				m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
				var anchorMin = Vector2.zero;
				var anchorMax = Vector2.one;

				if (_fillImage != null && _fillImage.type != Image.Type.Sliced) {
					_fillImage.type = Image.Type.Sliced;
				}
				else {
					if (reverseValue) {
						anchorMin[(int)axis] = 1 - maxNormalizedValue;
						anchorMax[(int)axis] = 1 - minNormalizedValue;
					}
					else {
						anchorMin[(int)axis] = minNormalizedValue;
						anchorMax[(int)axis] = maxNormalizedValue;
					}
				}

				m_FillRect.anchorMin = anchorMin;
				m_FillRect.anchorMax = anchorMax;
			}

			if (_maxHandleContainerRect != null) {
				m_Tracker.Add(this, m_MaxHandleRect, DrivenTransformProperties.Anchors);
				var anchorMin = Vector2.zero;
				var anchorMax = Vector2.one;
				anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - maxNormalizedValue) : maxNormalizedValue);
				m_MaxHandleRect.anchorMin = anchorMin;
				m_MaxHandleRect.anchorMax = anchorMax;
			}

			if (_minHandleContainerRect != null) {
				m_Tracker.Add(this, m_MinHandleRect, DrivenTransformProperties.Anchors);
				var anchorMin = Vector2.zero;
				var anchorMax = Vector2.one;
				anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - minNormalizedValue) : minNormalizedValue);
				m_MinHandleRect.anchorMin = anchorMin;
				m_MinHandleRect.anchorMax = anchorMax;
			}
		}

		private void UpdateDragMinHandle(PointerEventData eventData) {
			if (!TryGetCursorNormalizedValue(_minHandleContainerRect, eventData, out var normalizedValue))
				return;

			minNormalizedValue = Mathf.Clamp(normalizedValue, 0, maxNormalizedValue);
		}

		private void UpdateDragMaxHandle(PointerEventData eventData) {
			if (!TryGetCursorNormalizedValue(_maxHandleContainerRect, eventData, out var normalizedValue))
				return;

			maxNormalizedValue = Mathf.Clamp(normalizedValue, minNormalizedValue, 1);
		}

		private void UpdateDragFillContainerRect(PointerEventData eventData) {
			if (!TryGetCursorNormalizedValue(_fillContainerRect, eventData, out var normalizedValue))
				return;

			if (normalizedValue < minNormalizedValue) {
				minNormalizedValue = normalizedValue;
				maxNormalizedValue = minNormalizedValue + _intervalLength;
			}
			else if (normalizedValue > maxNormalizedValue) {
				maxNormalizedValue = Mathf.Clamp(normalizedValue, minNormalizedValue, 1);
				minNormalizedValue = maxNormalizedValue - _intervalLength;
			}
		}

		private void UpdateDragFillRect(PointerEventData eventData) {
			if (!TryGetCursorNormalizedValue(_fillContainerRect, eventData, out var normalizedValue))
				return;
			minNormalizedValue = Mathf.Clamp(normalizedValue - _lengthToMin, 0, 1 - _intervalLength);
			maxNormalizedValue = Mathf.Clamp(normalizedValue + _lengthToMax, _intervalLength, 1);
		}

		private bool TryGetCursorNormalizedValue(RectTransform clickRect, PointerEventData eventData, out float value) {
			value = 0;
			if (clickRect != null && clickRect.rect.size[(int)axis] > 0) {
				var position = Vector2.zero;
				if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
					return false;

				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, eventData.pressEventCamera, out var localCursor))
					return false;

				localCursor -= clickRect.rect.position;

				float val = Mathf.Clamp01((localCursor - _offset)[(int)axis] / clickRect.rect.size[(int)axis]);
				value = (reverseValue ? 1f - val : val);
				return true;
			}

			return false;
		}

		public override void OnPointerDown(PointerEventData eventData) {
			if (!MayDrag(eventData))
				return;

			base.OnPointerDown(eventData);
			_intervalLength = maxNormalizedValue - minNormalizedValue;
			
			_clickedOnMax = _maxHandleContainerRect != null &&
			                RectTransformUtility.RectangleContainsScreenPoint(m_MaxHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
			_clickedOnMin = _minHandleContainerRect != null &&
			                RectTransformUtility.RectangleContainsScreenPoint(m_MinHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
			_clickedOnFill = m_FillRect != null &&
			                 RectTransformUtility.RectangleContainsScreenPoint(m_FillRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);

			_offset = Vector2.zero;
			if (_clickedOnMax) {
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MaxHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera,
					    out var localMousePos))
					return;
				_offset = localMousePos;
				UpdateDragMaxHandle(eventData);
			}
			else if (_clickedOnMin) {
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MinHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera,
					    out var localMousePos))
					return;
				_offset = localMousePos;
				UpdateDragMinHandle(eventData);
			}
			else if (moveOnlyByHandles) {
				return;
			}
			else if (_clickedOnFill) {
				if (!TryGetCursorNormalizedValue(_fillContainerRect, eventData, out var normalizedValue))
					return;

				_lengthToMin = normalizedValue - minNormalizedValue;
				_lengthToMax = maxNormalizedValue - normalizedValue;
				UpdateDragFillRect(eventData);
			}
			else {
				UpdateDragFillContainerRect(eventData);
			}
		}

		public virtual void OnDrag(PointerEventData eventData) {
			if (!MayDrag(eventData))
				return;
			if (_clickedOnMax)
				UpdateDragMaxHandle(eventData);
			else if (_clickedOnMin)
				UpdateDragMinHandle(eventData);
			else if (moveOnlyByHandles)
				return;
			else if (_clickedOnFill)
				UpdateDragFillRect(eventData);
			else
				UpdateDragFillContainerRect(eventData);
		}

		public override void OnMove(AxisEventData eventData) {
			if (!IsActive() || !IsInteractable()) {
				base.OnMove(eventData);
				return;
			}

			/*switch (eventData.moveDir) {
				case MoveDirection.Left:
					if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
						Set(reverseValue ? valueMax + stepSize : valueMax - stepSize);
					else
						base.OnMove(eventData);
					break;
				case MoveDirection.Right:
					if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
						Set(reverseValue ? valueMax - stepSize : valueMax + stepSize);
					else
						base.OnMove(eventData);
					break;
				case MoveDirection.Up:
					if (axis == Axis.Vertical && FindSelectableOnUp() == null)
						Set(reverseValue ? valueMax - stepSize : valueMax + stepSize);
					else
						base.OnMove(eventData);
					break;
				case MoveDirection.Down:
					if (axis == Axis.Vertical && FindSelectableOnDown() == null)
						Set(reverseValue ? valueMax + stepSize : valueMax - stepSize);
					else
						base.OnMove(eventData);
					break;
			}*/
		}

		private bool MayDrag(PointerEventData eventData) =>
			base.IsActive() && base.IsInteractable() && eventData.button == PointerEventData.InputButton.Left;

		public override Selectable FindSelectableOnLeft() {
			if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
				return null;
			return base.FindSelectableOnLeft();
		}

		public override Selectable FindSelectableOnRight() {
			if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
				return null;
			return base.FindSelectableOnRight();
		}

		public override Selectable FindSelectableOnUp() {
			if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
				return null;
			return base.FindSelectableOnUp();
		}

		public override Selectable FindSelectableOnDown() {
			if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
				return null;
			return base.FindSelectableOnDown();
		}

		public virtual void OnInitializePotentialDrag(PointerEventData eventData) {
			eventData.useDragThreshold = false;
		}

		public void SetDirection(Direction newDirection, bool includeRectLayouts) {
			Axis oldAxis = axis;
			bool oldReverse = reverseValue;
			this.direction = newDirection;

			if (!includeRectLayouts)
				return;

			if (axis != oldAxis)
				RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

			if (reverseValue != oldReverse)
				RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
		}
		
		protected override void OnDidApplyAnimationProperties() {
			m_MinValue = ClampValueByLimit(m_MinValue);
			m_MaxValue = ClampValueByLimit(m_MaxValue);
			float oldNormalizedValue = maxNormalizedValue;
			if (_fillContainerRect != null) {
				if (_fillImage != null && _fillImage.type == Image.Type.Filled)
					oldNormalizedValue = _fillImage.fillAmount;
				else
					oldNormalizedValue = (reverseValue ? 1 - m_FillRect.anchorMin[(int)axis] : m_FillRect.anchorMax[(int)axis]);
			}
			else if (_maxHandleContainerRect != null)
				oldNormalizedValue = (reverseValue ? 1 - m_MaxHandleRect.anchorMin[(int)axis] : m_MaxHandleRect.anchorMin[(int)axis]);

			UpdateVisuals();

			if (oldNormalizedValue != maxNormalizedValue) {
				UISystemProfilerApi.AddMarker("Slider.value", this);
				onValuesChanged.Invoke(m_MinValue, m_MaxValue);
			}
		}

#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();

			if (wholeNumbers) {
				m_MinLimit = Mathf.Round(m_MinLimit);
				m_MaxLimit = Mathf.Round(m_MaxLimit);
			}

			if (IsActive()) {
				UpdateCachedReferences();
				m_DelayedUpdateVisuals = true;
			}

			if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
				CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
		}
#endif 
	}
}