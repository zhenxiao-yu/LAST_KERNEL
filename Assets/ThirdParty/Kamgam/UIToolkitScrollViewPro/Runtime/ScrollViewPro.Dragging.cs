using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public partial class ScrollViewPro
    {
        const string DragIgnoreClassName = "svp-drag-ignore";
        const string DragAllowClassName = "svp-drag-allow";

        public enum NestedInteractionKind
        {
            Default, // Currently equal to ForwardScrolling. TODO: Make equal to Unity scroll view behaviour.
            StopScrolling,
            ForwardScrolling,
        }

        /// <summary>
        /// Whether the scroll view is currently being dragged or not.
        /// </summary>
        public bool isDragging { get => _dragState == DragState.Dragging; }

        public const int FallbackAnimationFps = 60;
        const int UndefinedAnimationFps = -1;

        protected int _animationFps = UndefinedAnimationFps;

        /// <summary>
        /// Defines how quickly the animations will be updated.<br />
        /// If set to -1 then Application.targetFrameRate will be used.<br />
        /// If Application.targetFrameRate is -1 too then FallbackAnimationFps (60) will be used.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("animation-fps")]
#endif
        public int animationFps
        {
            get => _animationFps;
            set
            {
                if (_animationFps != value)
                {
                    _animationFps = value;
                    int fps = value;
                    if (fps <= 0)
                        fps = Application.targetFrameRate;
                    if (fps <= 0)
                        fps = FallbackAnimationFps;

                    _animationFrameDurationInMS = 1000 / fps;
                }
            }
        }

        protected int _animationFrameDurationInMS = 16;


        public const bool DefaultDragEnabled = true;
        /// <summary>
        /// Specifies whether the scrollbars should have buttons or not.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("drag-enabled")]
#endif
        public bool dragEnabled { get; set; } = DefaultDragEnabled;

        protected bool isDragOrDragEventBubblingEnabled => dragEnabled || dragEventBubbling;
        
        public const bool DefaultDragEventBubbling = false;
        /// <summary>
        /// Specifies whether the drag events will bubble to a parent scroll view if dragging is disabled.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("drag-event-bubbling")]
#endif
        public bool dragEventBubbling { get; set; } = DefaultDragEventBubbling;
        
        
        public const NestedInteractionKind DefaultNestedInteractionKind = NestedInteractionKind.Default;
        protected NestedInteractionKind _nestedInteractionKind = DefaultNestedInteractionKind;
        /// <summary>
        /// Options for controlling how nested ScrollView handles scrolling when reaching the limits of the scrollable area. 
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("nested-interaction-kind")]
#endif
        public NestedInteractionKind nestedInteractionKind
        {
            get
            {
                return _nestedInteractionKind;
            }
            set
            {
                _nestedInteractionKind = value;
            }
        }
        
        public const string DefaultDraggableChildTypes = "Slider,SliderInt,MinMaxSlider,Scroller";
        protected string _draggableChildTypes = DefaultDraggableChildTypes;
        
        protected string[] _draggableChildTypesList = new string[]{"Slider", "SliderInt", "MinMaxSlider", "Scroller"};
        /// <summary>
        /// Convenience field for accessing draggableChildTypes as an array.
        /// </summary>
        public string[] draggableChildTypesList => _draggableChildTypesList;
        
        /// <summary>
        /// Options for controlling how nested ScrollView handles scrolling when reaching the limits of the scrollable area. 
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("draggable-child-types")]
#endif
        public string draggableChildTypes
        {
            get
            {
                return _draggableChildTypes;
            }
            set
            {
                var trimmed = value.Trim().Replace(" ","");
                _draggableChildTypes = trimmed;
                _draggableChildTypesList = _draggableChildTypes.Split(',');
            }
        }

        public const bool DefaultCancelAnimationsOnDrag = true;
        /// <summary>
        /// Specifies whether the existing animations are cancelled on drag.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("cancel-animations-on-drag")]
#endif
        public bool cancelAnimationsOnDrag { get; set; } = DefaultCancelAnimationsOnDrag;
        
        public const float DefaultDragThreshold = 20f;
        /// <summary>
        /// If the mouse moves more than the sqrt of this distance then it is treated as a drag/scroll event.
        /// Otherwise it is treated as a click. 
        /// This threshold is also used for snap activation. Only after a drag has been started the onSnap will be triggered.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("drag-threshold")]
#endif
        [Tooltip("If the mouse moves more than the sqrt of this distance then it is treated as a drag/scroll event. Otherwise it is treated as a click.\n" +
            "This threshold is also used for snap activation. Only after a drag has been started the onSnap will be triggered.")]
        public float dragThreshold { get; set; } = DefaultDragThreshold;

        public const float DefaultVelocityMultiplier = 1f;

        protected float _velocityMultiplier = DefaultVelocityMultiplier;

        /// <summary>
        /// Multiplier applied to the tracked velocity when the pointer is released.
        /// Values above 1 make the scroll feel faster / more responsive to flick gestures.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("velocity-multiplier")]
#endif
        public float velocityMultiplier
        {
            get => _velocityMultiplier;
            set { _velocityMultiplier = Mathf.Max(0f, value); }
        }
        
        float _lastVelocityLerpTime;
        protected IVisualElementScheduledItem _inertiaAndElasticityAnimation;
        
        protected Vector2 _velocity;
        
        /// <summary>
        /// The current internal scroll velocity in pixels per second.<br />
        /// Read this before calling StopAnimations() to capture the velocity for custom animations.
        /// </summary>
        public Vector2 velocity => _velocity;

        // Mouse pos an offset are used for dragging AND child threshold detection.
        public enum DragState
        {
            Idle,
            CheckingThreshold,
            Dragging
        }

        // The starting drag scroll view per pointer id.
        // We use this to forward pointer move events from parent scroll views back to the child scroll view
        // that started the scroll.
        private static Dictionary<int,ScrollViewPro> _dragStartScrollView = new Dictionary<int, ScrollViewPro>();
        
        [System.NonSerialized]
        protected DragState _dragState = DragState.Idle;
        protected Vector2 _dragPointerDownPos;
        protected Vector2 _dragPointerDownScrollOffset;
        protected VisualElement _dragPropagationTarget;
        protected IEventHandler _dragCaptureTarget;
        protected ScrollViewMode _dragDirection;
        protected bool _dragBoundariesReached; 

        // Drag pointer id is only set if the contentContainer has captured the pointer for dragging.
        protected int _capturedDragPointerId = PointerId.invalidPointerId;
        protected IEventHandler _capturedDragHandler = null;

        protected IVisualElementScheduledItem _startDraggingTask;

        protected void captureDragPointer(IEventHandler handler, int pointerId)
        {
            if (pointerId == PointerId.invalidPointerId)
                return;

            if (_capturedDragHandler != null)
                releaseDragPointer();

            if (!PointerCaptureHelper.HasPointerCapture(handler, pointerId))
            {
                PointerCaptureHelper.CapturePointer(handler, pointerId);
            }
            _capturedDragHandler = handler;
            _capturedDragPointerId = pointerId;
        }

        protected bool isCapturedDragPointerId(int pointerId)
        {
            return pointerId != PointerId.invalidPointerId && pointerId == _capturedDragPointerId;
        }

        protected bool hasCapturedDragPointer()
        {
            return _capturedDragPointerId != PointerId.invalidPointerId
                && _capturedDragHandler != null
                && PointerCaptureHelper.HasPointerCapture(_capturedDragHandler, _capturedDragPointerId);
        }

        protected void releaseDragPointer()
        {
            if (_capturedDragPointerId == PointerId.invalidPointerId)
                return;

            if (_capturedDragHandler == null)
                return;

            if (PointerCaptureHelper.HasPointerCapture(_capturedDragHandler, _capturedDragPointerId))
            {
                PointerCaptureHelper.ReleasePointer(_capturedDragHandler, _capturedDragPointerId);
            }

            _capturedDragPointerId = PointerId.invalidPointerId;
            _capturedDragHandler = null;
        }

        protected void startDragThresholdTracking(PointerDownEvent evt, IEventHandler captureTarget)
        {
            if (!isDragOrDragEventBubblingEnabled)
                return;
            
            if (_dragState != DragState.Idle)
                return;

			// Track the scroll view that started the drag check.
            _dragStartScrollView[evt.pointerId] = this;
            
            _dragState = DragState.CheckingThreshold;
            _dragPointerDownPos = evt.position;
            _dragPropagationTarget = evt.target as VisualElement;
            _dragCaptureTarget = captureTarget;
            _dragDirection = ScrollViewMode.VerticalAndHorizontal;
            _dragBoundariesReached = false;
        }

        protected bool hasSurpassedDragThreshold(Vector3 mousePosition)
        {
            if (!isDragOrDragEventBubblingEnabled)
                return false;

            var movementSinceDown = (Vector2)mousePosition - _dragPointerDownPos;
            return movementSinceDown.sqrMagnitude > dragThreshold * dragThreshold;
        }

        protected void startDragging(Vector3 pointerPosition, int pointerId, bool capturePointer, bool bubbleDragEvents, IEventHandler captureTarget, VisualElement propagationTarget, ScrollViewMode? dragDirection = null)
        {
            _dragStartScrollView.Remove(pointerId);
                
            if (!isDragOrDragEventBubblingEnabled || isDragging || (capturePointer && hasCapturedDragPointer()))
                return;

            calculateBounds();

            if (capturePointer)
                captureDragPointer(captureTarget, pointerId);

            if (!dragDirection.HasValue) // Null able check to support bubbling.
            {
                // Fix/Feature: Do it like the Unity scroll view and check what major direction
                // the drag was started in and then use that to constraint the drag. This is useful
                // for nested scroll views with a perpendicular direction to the parent scroll view.
                // See: https://discussions.unity.com/t/ui-toolkit-scroll-view-pro/929016/54
                _dragDirection = ScrollViewMode.VerticalAndHorizontal;
                // Though we only do that if the child scroll view has a direction constraint. 
                if (scrollableWidth > 0f | scrollableHeight > 0f)
                {
                    var movementSinceDown = (Vector2)pointerPosition - _dragPointerDownPos;
                    if (Mathf.Abs(movementSinceDown.x) > Mathf.Abs(movementSinceDown.y))
                        _dragDirection = ScrollViewMode.Horizontal;
                    else
                        _dragDirection = ScrollViewMode.Vertical;
                }
            }
            else
            {
                _dragDirection = dragDirection.Value;
            }

            _dragPointerDownPos = pointerPosition;
            _dragPointerDownScrollOffset = scrollOffset;

            _dragState = DragState.Dragging;

            
            if (bubbleDragEvents && (nestedInteractionKind == NestedInteractionKind.Default || nestedInteractionKind == NestedInteractionKind.ForwardScrolling))
                DragStartEvent.Dispatch(propagationTarget, this, pointerPosition, pointerId, _dragDirection);
        }

        // This is called if a child scroll view has started dragging and it was configured to forward (bubble) the drag events to its parents.
        protected void onBubbledDragStartReceived(DragStartEvent evt)
        {
            startDragging(evt.pointerPosition, evt.pointerId, capturePointer: false, bubbleDragEvents: false, captureTarget: null, propagationTarget: null, dragDirection: evt.direction);
        }

        protected void stopDragging(bool startAnimation, bool releasePointer, bool bubbleDragEvents)
        {
            if (!isDragOrDragEventBubblingEnabled)
                return;

            if (!isDragging)
                return;

            if (releasePointer)
                releaseDragPointer();

            if (hasInertia || touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic)
            {
                if (startAnimation)
                {
                    startInertiaAndElasticityAnimation();
                }
            }

            _dragState = DragState.Idle;

            if (bubbleDragEvents && (nestedInteractionKind == NestedInteractionKind.Default || nestedInteractionKind == NestedInteractionKind.ForwardScrolling))
                DragStopEvent.Dispatch(this, this);
        }

        // This is called if a child scroll view has stopped dragging and it was configured to forward (bubble) the drag events to its parents.
        protected void onBubbledDragStopReceived(DragStopEvent evt)
        {
            stopDragging(startAnimation: true, releasePointer: false, bubbleDragEvents: false);
        }

        /// <summary>
        /// Returns true if the event was cancelled.
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        protected bool ignoreDragAndCancelEvent(EventBase evt, bool cancelEvent, bool ignoreIfInOtherScrollView = true)
        {
            if (evt.target == null)
                return false;

            if (ignoreIfInOtherScrollView && (evt.target as VisualElement).GetFirstAncestorOfType<ScrollViewPro>() != this)
                return true;

            // If the element or a parent has the DragIgnoreClassName class then it is considered ignored UNLESS
            // the element itself also has the DragAllowClassName name.
            var ve = evt.target as VisualElement;
            if (ve != null && ve.IsChildOfClass(DragIgnoreClassName, DragAllowClassName, includeSelf: true, preferNegative: true))
            {
                if (cancelEvent)
                    evt.StopImmediatePropagation();

                return true;
            }

            return false;
        }

        // Used to remember whether or not a scroll-to animation was cancelled by pointer down.
        // We do remember this because we want to use that info in pointer up to.
        protected bool _pointerDownWhileScrollToAnimation;

        protected void onPointerDown(PointerDownEvent evt)
        {
            revertCaptureFix();
            
            if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
            {
                return;
            }

            _pointerDownWhileScrollToAnimation = hasActiveScrollToAnimation;

            if (cancelAnimationsOnDrag)
                StopAnimations();
            
            startDragThresholdTracking(evt, contentContainer);
        }

        // Necessary because in infinite scroll views the contentContainer (which we
        // normally subscribe our events to) will be outside the viewport rather quickly.
        protected void onPointerDownOnViewport(PointerDownEvent evt)
        {
            if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                return;

            if (infinite)
                startDragThresholdTracking(evt, contentContainer);
        }

        public void StopAnimations()
        {
            _inertiaAndElasticityAnimation?.Pause();
            _scrollWheelScheduledAnimation?.Pause();

            StopScrollToAnimation(); // See "ScrollToAnimated".
        }

        protected void onPointerMove(PointerMoveEvent evt)
        {
            onPointerMove(evt, checkPointerCapture: true, bubbleDragEvents: true, movementConstraint: ScrollViewMode.VerticalAndHorizontal, ignoreIfInOtherScrollView: false);
        }

        protected void onPointerMove(PointerMoveEvent evt, bool checkPointerCapture, bool bubbleDragEvents, ScrollViewMode movementConstraint = ScrollViewMode.VerticalAndHorizontal, bool ignoreIfInOtherScrollView = true)
        {
            // While dragging (or drag threshold checking) forward move events from outside scroll views to the scroll view that the drag was started in.
            // Fix for Unity 6+ which made dragging stop once the pointer was moved outside the starting scroll view before the threshold was reached.
            // The reason is that we do not capture the pointer while waiting for the drag threshold because capturing it does prevent click/mouse-up events
            // from being fired in some Unity versions.
            if (_dragStartScrollView.TryGetValue(evt.pointerId, out var dragScrollView) && dragScrollView != this)
            {
                if (!(evt.target as VisualElement).IsChildOf(dragScrollView))
                {
                    dragScrollView.onPointerMove(evt, checkPointerCapture, bubbleDragEvents, movementConstraint, ignoreIfInOtherScrollView);
                    return;
                }
            }

            if (!isDragOrDragEventBubblingEnabled)
                return;

            if (_dragState == DragState.Idle)
                return;

            if (ignoreDragAndCancelEvent(evt, cancelEvent: false, ignoreIfInOtherScrollView))
                return;
            if (_dragState == DragState.CheckingThreshold)
            {
                // If nothing is pressed but _dragState is still checking then we have a case of missing PointerUp event (it happens, TODO: investigate, report bug).
                if (evt.isPrimary && evt.pressedButtons == 0)
                {
                    _pointerDownOnChild = false;
                    _dragState = DragState.Idle;
                }
                else
                {
                    if (hasSurpassedDragThreshold(evt.position))
                    {
                        _pointerDownOnChild = false;

                        // We could use _dragPointerDownPos here but that would make the drag a bit jumpy at the start.
                        startDragging(evt.position, evt.pointerId, checkPointerCapture, bubbleDragEvents, _dragCaptureTarget, _dragPropagationTarget);
                    }
                }
            }
            else if (_dragState == DragState.Dragging)
            {
                // If nothing is pressed but _dragState is still checking then we have a case of missing PointerUp event (it happens, TODO: investigate, report bug).
                if (evt.isPrimary && evt.pressedButtons == 0)
                {
                    // We hav to delay because if there will be an UP event it may come AFTER (and we
                    // don't want to double stop dragging, especially with startAnimation=false).
                    schedule.Execute(() =>
                    {
                        if (isDragging) // Still dragging? Okay, then no UP event was fired and we force stop dragging.
                        {
                            stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);
                            _pointerDownOnChild = false;
                        }
                    });
                }
                else
                {
                    if (!checkPointerCapture || hasCapturedDragPointer())
                    {
                        handleDrag(evt, movementConstraint);

                        if (bubbleDragEvents && (nestedInteractionKind == NestedInteractionKind.Default || nestedInteractionKind == NestedInteractionKind.ForwardScrolling))
                            DragMoveEvent.Dispatch(evt.target as VisualElement, this, _dragDirection, evt);
                    }
                }
            }
        }

        // This is called if the pointer was dragged in a child view.
        private void onBubbledMoveReceived(DragMoveEvent evt)
        {
            // If the child scroll view can be scrolled then ignore the scroll in the parent.
            var inHorizontalBounds = evt.scrollView.scrollOffset.x - evt.scrollView._lowBounds.x > 0.05f && evt.scrollView.scrollOffset.x - evt.scrollView._highBounds.x < -0.05f;
            var inVerticalBounds = evt.scrollView.scrollOffset.y - evt.scrollView._lowBounds.y > 0.05f && evt.scrollView.scrollOffset.y - evt.scrollView._highBounds.y < -0.05f;

            if (
                   (evt.scrollView.mode == ScrollViewMode.Vertical && inVerticalBounds && evt.direction == ScrollViewMode.Vertical)
                || (evt.scrollView.mode == ScrollViewMode.Horizontal && inHorizontalBounds && evt.direction == ScrollViewMode.Horizontal)
               )
            {
                _dragBoundariesReached = false;
                return;
            }
            else
            {
                // Once the child view has reached the boundaries we allow dragging the parent BUT
                // we have to make sure the drag distance (velocity, elasticity) are calculated from
                // the point when the drag boundaries were reached, thus we update the drag start
                // positions of the parent here.
                if (!_dragBoundariesReached)
                {
                    _dragPointerDownPos = evt.pointerMoveEvent.position;
                    _dragPointerDownScrollOffset = scrollOffset;
                }
                
                _dragBoundariesReached = true;
            }
            
            ScrollViewMode constraint = ScrollViewMode.VerticalAndHorizontal;
            onPointerMove(evt.pointerMoveEvent, checkPointerCapture: false, bubbleDragEvents: false, constraint, ignoreIfInOtherScrollView: false);
        }

        protected void handleDrag(PointerMoveEvent evt, ScrollViewMode movementConstraint = ScrollViewMode.VerticalAndHorizontal)
        {
            // If dragging is disabled and we land here then it's only because dragEventBubbling
            // is enabled and we have to prevent the scroll view from moving.
            if (!dragEnabled && dragEventBubbling)
                return;
            
            // If a drag direction was set then that overrides the movementConstraint.
            if (_dragDirection != ScrollViewMode.VerticalAndHorizontal)
                movementConstraint = _dragDirection;

            // Elastic and inertia effects need knowledge about the total dragged length.
            // Side effect of this: bubbled drag to parent will make the parent
            // jump on boundary.
            Vector2 deltaPos = (Vector2)evt.position - _dragPointerDownPos;
            var newScrollOffset = _dragPointerDownScrollOffset - deltaPos;

            // Clamp based on scroll behaviour
            if (touchScrollBehavior == ScrollView.TouchScrollBehavior.Clamped)
            {
                newScrollOffset = Vector2.Max(newScrollOffset, _lowBounds);
                newScrollOffset = Vector2.Min(newScrollOffset, _highBounds);
            }
            else if (touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic)
            {
                newScrollOffset.x = computeElasticOffset(
                    scrollOffset.x,
                    deltaPos.x, _dragPointerDownScrollOffset.x,
                    _lowBounds.x, _lowBounds.x - contentViewport.resolvedStyle.width,
                    _highBounds.x, _highBounds.x + contentViewport.resolvedStyle.width);

                newScrollOffset.y = computeElasticOffset(
                    scrollOffset.y,
                    deltaPos.y, _dragPointerDownScrollOffset.y,
                    _lowBounds.y, _lowBounds.y - contentViewport.resolvedStyle.height,
                    _highBounds.y, _highBounds.y + contentViewport.resolvedStyle.height);
            }

            // Reset x or y based on scroll mode.
            switch (mode)
            {
                case ScrollViewMode.Vertical:
                    newScrollOffset.x = scrollOffset.x;
                    break;
                case ScrollViewMode.Horizontal:
                    newScrollOffset.y = scrollOffset.y;
                    break;
                default:
                    break;
            }

            // Reset x or y based on movement Constraint
            switch (movementConstraint)
            {
                case ScrollViewMode.Vertical:
                    newScrollOffset.x = scrollOffset.x;
                    break;
                case ScrollViewMode.Horizontal:
                    newScrollOffset.y = scrollOffset.y;
                    break;
                default:
                    break;
            }

            // Calculate velocity
            // Velocity is updated just like in Unity's own ScrollView.
            if (hasInertia)
            {
                if (scrollOffset == _lowBounds || scrollOffset == _highBounds)
                {
                    _velocity = Vector2.zero;
                }
                else
                {
                    if (_lastVelocityLerpTime > 0f)
                    {
                        float dT = Time.unscaledTime - _lastVelocityLerpTime;
                        _velocity = Vector2.Lerp(_velocity, Vector2.zero, dT * 10f);
                    }

                    _lastVelocityLerpTime = Time.unscaledTime;
                    float unscaledDeltaTime = Time.unscaledDeltaTime;
                    Vector2 b = (newScrollOffset - scrollOffset) / unscaledDeltaTime;
                    _velocity = Vector2.Lerp(_velocity, b, unscaledDeltaTime * 10f);
                }
            }

            // Finally set new scroll offset.
            scrollOffset = newScrollOffset;
        }

        protected void onPointerCancel(PointerCancelEvent evt)
        {
            revertCaptureFix();
            _dragStartScrollView.Remove(evt.pointerId);
            
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (!isCapturedDragPointerId(evt.pointerId))
                    return;

                if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    return;

                stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);
            }
        }

        protected VisualElement _childCaptureFixedTo;
        protected int _childCaptureFixPointerId;
        
        protected void onPointerCapture(PointerCaptureEvent evt)
        {
            _dragStartScrollView.Remove(evt.pointerId);
            
            // Fix for the issue that dragging from the contentContainer in child views did
            // not work. The reason was that the pointer was captured but the childScrollView
            // and not the contentContainer and thus no move events were triggered. The solution
            // is to move the capture from the scroll view to the contentContainer if the capture
            // target is a childScrollView.
            var childScrollView = evt.target as ScrollViewPro;
            if (childScrollView != null && childScrollView != this && childScrollView.IsChildOf(this))
            {
                PointerCaptureHelper.ReleasePointer(evt.target, evt.pointerId);
                PointerCaptureHelper.CapturePointer(childScrollView.contentContainer, evt.pointerId);
                _childCaptureFixedTo = childScrollView.contentContainer;
                _childCaptureFixPointerId = evt.pointerId;
            }
        }

        protected void revertCaptureFix()
        {
            if (_childCaptureFixedTo != null)
            {
                PointerCaptureHelper.ReleasePointer(_childCaptureFixedTo, _childCaptureFixPointerId);
                _childCaptureFixedTo = null;
                _childCaptureFixPointerId = -1;
            }
        }


        protected void onPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            // Cancel drag threshold checking if the capture changes
            // (i.e. if a button or some other interactive element is clicked).
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
        }

        protected void onPointerUp(PointerUpEvent evt)
        {
            _dragStartScrollView.Remove(evt.pointerId);
            revertCaptureFix();
            
            bool didCancelScrollTo = _pointerDownWhileScrollToAnimation;
            _pointerDownWhileScrollToAnimation = false;
            
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;

                if (snap && didCancelScrollTo)
                    Snap();
            }
            else if (_dragState == DragState.Dragging)
            {
                if (!isCapturedDragPointerId(evt.pointerId))
                    return;

                if (ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    return;

                stopDragging(startAnimation: true, releasePointer: true, bubbleDragEvents: true);

                if (snap && (!hasInertia || didCancelScrollTo))
                    Snap();
            }
        }

        protected bool _pointerDownOnChild = false;

        protected void onPointerDownOnChild(PointerDownEvent evt)
        {
            revertCaptureFix();

            // Manually make a copy and pool it because we need to retain it for later use.
            var pointerDownEvent = PointerDownEvent.GetPooled(evt);
            pointerDownEvent.target = evt.target;
            
            // This fixes the (for users) unexpected behaviour of interactable elements (like sliders) not being
            // usable unless svp-drag-ignore is added. People's expectation is that interactable elements remain functional
            // even if dragEnable is activated (which is reasonable and I now consider this an error to be fixed.)
            //
            // Sadly we have to delay the execution because interactive elements capture the cursor late (after this event).
            // Therefore we wait until the capturing is done and then we use the infos whether or not the pointer was captured
            // to determine if the events should be forwarded.
            schedule.Execute(() =>
            {
                if (shouldReactToChildPointerEvent(pointerDownEvent))
                {
                    if (!isDragOrDragEventBubblingEnabled)
                        return;

                    if (ignoreDragAndCancelEvent(pointerDownEvent, cancelEvent: false))
                        return;

                    if (isDragging)
                        stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);

                    _pointerDownOnChild = true;

                    startDragThresholdTracking(pointerDownEvent, pointerDownEvent.target);
                }
                
                pointerDownEvent.Dispose();
            });
        }

        protected void onPointerMoveOnChild(PointerMoveEvent evt)
        {
            // Check if something other that the scroll view drag target has captured the pointer. If yes, then abort.
            if (!shouldReactToChildPointerEvent(evt))
                return;
            
            onPointerMove(evt);
        }

        protected bool shouldReactToChildPointerEvent<T>(PointerEventBase<T> evt) where T : PointerEventBase<T>, new()
        {
            var captureTarget = panel.GetCapturingElement(evt.pointerId);

            if (   captureTarget == null
                || captureTarget == _capturedDragHandler
                || captureTarget == _dragCaptureTarget)
                return true;

            foreach (var typeName in draggableChildTypesList)
            {
                if (!string.IsNullOrEmpty(typeName) && captureTarget.IsChildOfType(typeName))
                {
                    return false;
                }
            }

            return true;
        }

        protected void onPointerCancelOnChild(PointerCancelEvent evt)
        {
            // Check if something other that the scroll view drag target has captured the pointer. If yes, then abort.
            if (!shouldReactToChildPointerEvent(evt))
                return;
            
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (isCapturedDragPointerId(evt.pointerId) && !ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    stopDragging(startAnimation: false, releasePointer: true, bubbleDragEvents: true);
            }

            _pointerDownOnChild = false;
        }

        protected void onPointerUpOnChild(PointerUpEvent evt)
        {
            // Check if something other that the scroll view drag target has captured the pointer. If yes, then abort.
            if (!shouldReactToChildPointerEvent(evt))
                return;
            
            if (_dragState == DragState.CheckingThreshold)
            {
                _dragState = DragState.Idle;
            }
            else if (_dragState == DragState.Dragging)
            {
                if (isCapturedDragPointerId(evt.pointerId) && !ignoreDragAndCancelEvent(evt, cancelEvent: false))
                    stopDragging(startAnimation: true, releasePointer: true, bubbleDragEvents: true);
            }

            _pointerDownOnChild = false;
        }

        protected int _lastClickEventFrame = -1;
        protected HashSet<IEventHandler> _frameClickEvents = new HashSet<IEventHandler>(5);

        protected void onPointerClickOnChild(ClickEvent evt)
        {
            // Check if something other that the scroll view drag target has captured the pointer. If yes, then abort.
            if (!shouldReactToChildPointerEvent(evt))
                return;
            
            // Tracks all clicked elements within a single frame and if a click event is triggered twice it will be cancelled.
            // This fixes some double event triggering issues.

            if (Time.frameCount != _lastClickEventFrame)
            {
                _frameClickEvents.Clear();
                _lastClickEventFrame = Time.frameCount;
            }

            if (_frameClickEvents.Contains(evt.currentTarget))
            {
                evt.StopImmediatePropagation();
            }
            _frameClickEvents.Add(evt.currentTarget);
        }

        /// <summary>
        /// Compute the new scroll view offset from a pointer delta, taking elasticity into account.
        /// Low and high limits are the values beyond which the scrollview starts to show resistance to scrolling (elasticity).
        /// Low and high hard limits are the values beyond which it is infinitely hard to scroll.
        /// </summary>
        /// <param name="currentOffset"></param>
        /// <param name="deltaPointer"></param>
        /// <param name="initialScrollOffset"></param>
        /// <param name="lowLimit"></param>
        /// <param name="hardLowLimit"></param>
        /// <param name="highLimit"></param>
        /// <param name="hardHighLimit"></param>
        /// <returns></returns>
        protected static float computeElasticOffset(
            float currentOffset,
            float deltaPointer, float initialScrollOffset,
            float lowLimit, float hardLowLimit,
            float highLimit, float hardHighLimit)
        {
            // Short circuit if inside all limits.
            float targetOffset = initialScrollOffset - deltaPointer;
            // Extra margins for equal state (with if the initial state of the scroll view).
            if (targetOffset > lowLimit - 0.001f && targetOffset < highLimit + 0.001f)
            {
                return targetOffset;
            }

            // Here it is between the limit and the hard limit.
            float limit = targetOffset < lowLimit ? lowLimit : highLimit;
            float hardLimit = targetOffset < lowLimit ? hardLowLimit : hardHighLimit;
            float span = hardLimit - limit;
            float delta = targetOffset - limit;
            float normalizedDelta = delta / span;
            // 0.3f = the content will stop at 30% of the scroll view size.
            float ratio = (1f - (normalizedDelta - 1) * (normalizedDelta - 1)) * 0.3f;
            if (normalizedDelta < 1f)
            {
                return limit + span * ratio;
            }
            else
            {
                return currentOffset;
            }
        }


        protected void startInertiaAndElasticityAnimation()
        {
            calcInitialSpringBackVelocity();

            // Reset if not moved for a while. Done to avoid inertia
            // animation in case the pointer was not moved for some time.
            if (Time.unscaledTime - _lastVelocityLerpTime > 0.2f)
                _velocity = Vector2.zero;
            
            // Apply multiplier
            _velocity *= _velocityMultiplier;

            if (_inertiaAndElasticityAnimation == null)
            {
                _inertiaAndElasticityAnimation = base.schedule.Execute(inertiaAndElasticityAnimationStep).Every(_animationFrameDurationInMS);
            }
            else
            {
                _inertiaAndElasticityAnimation.Resume();
            }
        }

        protected Vector2 _springBackVelocity;

        protected void calcInitialSpringBackVelocity()
        {
            if (touchScrollBehavior != ScrollView.TouchScrollBehavior.Elastic)
            {
                _springBackVelocity = Vector2.zero;
                return;
            }

            if (scrollOffset.x < _lowBounds.x)
            {
                _springBackVelocity.x = _lowBounds.x - scrollOffset.x;
            }
            else if (scrollOffset.x > _highBounds.x)
            {
                _springBackVelocity.x = _highBounds.x - scrollOffset.x;
            }
            else
            {
                _springBackVelocity.x = 0f;
            }

            if (scrollOffset.y < _lowBounds.y)
            {
                _springBackVelocity.y = _lowBounds.y - scrollOffset.y;
            }
            else if (scrollOffset.y > _highBounds.y)
            {
                _springBackVelocity.y = _highBounds.y - scrollOffset.y;
            }
            else
            {
                _springBackVelocity.y = 0f;
            }
        }

        protected void inertiaAndElasticityAnimationStep()
        {
            inertiaAnimationStep();
            elasticityAnimationStep();

            // If none of the animations needs updating then pause.
            if (_springBackVelocity == Vector2.zero && _velocity == Vector2.zero)
            {
                _inertiaAndElasticityAnimation.Pause();
            }
        }

        protected void elasticityAnimationStep()
        {
            if (touchScrollBehavior != ScrollView.TouchScrollBehavior.Elastic)
            {
                _springBackVelocity = Vector2.zero;
                return;
            }

            // Unity ScrollView uses Time.unscaledDeltaTime internally which makes the spring back
            // animation very slow on high fps (like in the Editor). To avoid that we use the delay
            // of the animation as delta time.
            float deltaTime = _animationFrameDurationInMS / 1000f;

            Vector2 vector = scrollOffset;
            if (vector.x < _lowBounds.x)
            {
                vector.x = Mathf.SmoothDamp(vector.x, _lowBounds.x, ref _springBackVelocity.x, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.x) < 1f)
                {
                    _springBackVelocity.x = 0f;
                }
            }
            else if (vector.x > _highBounds.x)
            {
                vector.x = Mathf.SmoothDamp(vector.x, _highBounds.x, ref _springBackVelocity.x, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.x) < 1f)
                {
                    _springBackVelocity.x = 0f;
                }
            }
            else
            {
                _springBackVelocity.x = 0f;
            }

            if (vector.y < _lowBounds.y)
            {
                vector.y = Mathf.SmoothDamp(vector.y, _lowBounds.y, ref _springBackVelocity.y, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.y) < 1f)
                {
                    _springBackVelocity.y = 0f;
                }
            }
            else if (vector.y > _highBounds.y)
            {
                vector.y = Mathf.SmoothDamp(vector.y, _highBounds.y, ref _springBackVelocity.y, elasticity, float.PositiveInfinity, deltaTime);
                if (Mathf.Abs(_springBackVelocity.y) < 1f)
                {
                    _springBackVelocity.y = 0f;
                }
            }
            else
            {
                _springBackVelocity.y = 0f;
            }

            scrollOffset = vector;
        }

        protected void inertiaAnimationStep()
        {
            // Unity ScrollView uses Time.unscaledDeltaTime internally which makes the spring back
            // animation very slow on high fps (like in the Editor). To avoid that we use the delay
            // of the animation as delta time.
            float deltaTime = _animationFrameDurationInMS / 1000f;

            if (hasInertia && _velocity != Vector2.zero)
            {
                _velocity *= Mathf.Pow(scrollDecelerationRate, deltaTime);

                // Set to 0 if close to zero or if out of bounds and behaviour is elastic.
                if (Mathf.Abs(_velocity.x) < 1f || (touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic && (scrollOffset.x < _lowBounds.x || scrollOffset.x > _highBounds.x)))
                {
                    _velocity.x = 0f;
                }

                if (Mathf.Abs(_velocity.y) < 1f || (touchScrollBehavior == ScrollView.TouchScrollBehavior.Elastic && (scrollOffset.y < _lowBounds.y || scrollOffset.y > _highBounds.y)))
                {
                    _velocity.y = 0f;
                }

                scrollOffset += _velocity * deltaTime;
            }
            else
            {
                _velocity = Vector2.zero;
            }

            handleSnappingWhileInertiaAnimation();
        }
    }
}
