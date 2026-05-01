using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public partial class ScrollViewPro
    {
        protected float? _childrenBoundsWidth;
        protected float? _childrenBoundsHeight;

        protected class PositionStyleRecord
        {
            public VisualElement Element;
            public StyleEnum<Position> Position;
            public StyleLength Left;
            public StyleLength Top;
            public StyleLength Right;
            public StyleLength Bottom;

            public static PositionStyleRecord Create(VisualElement element)
            {
                var record = new PositionStyleRecord();

                record.Element = element;
                record.Position = element.getComputedStyle<Position>("position", element.resolvedStyle.position);
                record.Left = element.getComputedStyle<Length>("left", element.style.left.value);
                record.Top = element.getComputedStyle<Length>("top", element.style.top.value);
                record.Right = element.getComputedStyle<Length>("right", element.style.right.value);
                record.Bottom = element.getComputedStyle<Length>("bottom", element.style.bottom.value);
                
                return record;
            }

            public void Apply()
            {
                Element.style.position = Position;
                Element.style.left = Left;
                Element.style.top = Top;
                Element.style.right = Right;
                Element.style.bottom = Bottom;
            }
        }

        protected List<PositionStyleRecord> _originalChildPositions = new List<PositionStyleRecord>();

        protected void defragInfiniteChildPositions()
        {
            for (int i = _originalChildPositions.Count()-1; i >= 0; i--)
            {
                if (   _originalChildPositions[i].Element == null
                    || _originalChildPositions[i].Element.panel == null
                    || !this.Contains(_originalChildPositions[i].Element))
                {
                    _originalChildPositions.RemoveAt(i);
                }
            }
        }

        protected bool originalChildPositionsContains(VisualElement element)
        {
            foreach (var record in _originalChildPositions)
            {
                if (record.Element == element)
                    return true;
            }

            return false;
        }

        protected void recordNewInfiniteChildPositions()
        {
            for (int i = 0; i < childCount; i++)
            {
                var child = ElementAt(i);
                if (!originalChildPositionsContains(child))
                {
                    var record = PositionStyleRecord.Create(child);
                    _originalChildPositions.Add(record);
                }
            }
        }
        
        protected void revertInfiniteChildLayouts()
        {
            defragInfiniteChildPositions();
            
            foreach (var record in _originalChildPositions)
            {
                record.Apply();
            }
            
            MarkDirtyRepaint();
        }
        
        protected void clearInfiniteChildInfoCache()
        {
            _originalChildPositions.Clear();
            _childrenBoundsWidth = null;
            _childrenBoundsHeight = null;
        }

        protected bool _tmpInfinite;
        protected Vector2 _tmpInfiniteScrollOffset;

        
        protected bool _waitingForInfiniteAddComplete = false;
        
        /// <summary>
        ///  If the scroll view is infinite then this has to be called BEFORE adding new elements or changing the layout.<br />
        /// HINT: If you use the Add() method then this will be done automatically.
        /// </summary>
        public void StartInfiniteHierarchyChange()
        {
            _waitingForInfiniteAddComplete = true;
                
            // Temporarily disable infinity.
            infinite = false;
            
            // Stop all animations and return to initial position
            StopAnimations();
            _tmpInfiniteScrollOffset = Vector2.zero;
            if (_childrenBoundsWidth.HasValue)
            {
                _tmpInfiniteScrollOffset = new Vector2(
                    (scrollOffset.x % _childrenBoundsWidth.Value) + 1,
                    (scrollOffset.y % _childrenBoundsHeight.Value) + 1
                );
            }

            scrollOffset = Vector2.zero;
            _velocity = Vector2.zero;
            
            // Re-enable the layout system for all children and
            // reset them to their original position.
            revertInfiniteChildLayouts();
        }
        
        /// <summary>
        /// If the scroll view is infinite then this has to be called AFTER adding new elements dynamically.<br />
        /// HINT: If you use the Add() method then this will be done automatically.
        /// </summary>
        public void EndInfiniteHierarchyChange()
        {
            if (!hasFinishedChildLayout())
            {
                // Add a delay to wait for layout.
                schedule.Execute(EndInfiniteHierarchyChange);
            }
            else
            {
                // Update infinite cache.
                clearInfiniteChildInfoCache();
                refreshInfiniteChildInfosIfNeeded(waitForLayout: false); // No need to wait, we already did wait.

                _waitingForInfiniteAddComplete = false;
                    
                // Re-enable infinity.
                infinite = true;

                // Apply the offset as it was before (but clamped).
                scrollOffset = _tmpInfiniteScrollOffset;

                // Wait 2+ frames (does not work if waited only for 1 frame). This is done to trigger the proper
                // infinite teleportation. TODO: Find out why (probably some layout wait/issue).
                setScrollOffsetDelayed(2, Vector2.one);
                setScrollOffsetDelayed(3, -Vector2.one);
            }
        }

        private void setScrollOffsetDelayed(int delay, Vector2 delta)
        {
            delay -= 1;
            if (delay >= 0)
            {
                schedule.Execute(() => { setScrollOffsetDelayed(delay, delta); });
            }
            else
            {
                scrollOffset += delta;
            }
        }

        protected bool hasFinishedChildLayout()
        {
            var children = contentContainer.Children();
            foreach (var child in children)
            {
                if    (float.IsNaN(child.contentRect.width)
                    || float.IsNaN(child.contentRect.height))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Use this to add children to the scroll view dynamically (especially important for infinite scroll views).
        /// </summary>
        /// <param name="child"></param>
        public new void Add(VisualElement child)
        {
            // Avoid miscalculation of infinite scroll pos if scroll view was not layouted yet.
            if (!_isReady || float.IsNaN(layout.height) || float.IsNaN(layout.width))
            {
                AddNative(child);
                RefreshAfterHierarchyChange();
                return;
            }
            
            if (child == null)
                return;

            _tmpInfinite = infinite;
            if (_tmpInfinite)
                StartInfiniteHierarchyChange();

            AddNative(child);
            
            if (_tmpInfinite)
                EndInfiniteHierarchyChange();
            
            RefreshAfterHierarchyChange();
        }
        
        /// <summary>
        /// Use this to remove children to the scroll view dynamically (especially important for infinite scroll views).
        /// </summary>
        /// <param name="child"></param>
        public new void Remove(VisualElement child)
        {
            if (child == null)
                return;

            _tmpInfinite = infinite;
            if (_tmpInfinite)
                StartInfiniteHierarchyChange();

            RemoveNative(child);
            
            if (_tmpInfinite)
                EndInfiniteHierarchyChange();
            
            RefreshAfterHierarchyChange();
        }

        /// <summary>
        /// Calls the native Add() without the extra shenanigans of infinite scrolling.
        /// </summary>
        /// <param name="child"></param>
        public void AddNative(VisualElement child)
        {
            base.Add(child);
        }
        
        /// <summary>
        /// Call the native Remove() without the extra shenanigans of infinite scrolling.
        /// </summary>
        /// <param name="child"></param>
        public void RemoveNative(VisualElement child)
        {
            base.Remove(child);
        }

        protected bool _waitingForInfiniteRefreshComplete; 

        protected void refreshInfiniteChildInfosIfNeeded(bool waitForLayout)
        {
            
#if UNITY_EDITOR
            if (panel != null && panel.contextType == ContextType.Editor)
                return;
#endif
            
            defragInfiniteChildPositions();

            if (waitForLayout)
            {
                _waitingForInfiniteRefreshComplete = true;
                if (hasFinishedChildLayout())
                {
                    refreshInfiniteChildInfosIfNeededInternal();
                    _waitingForInfiniteRefreshComplete = false;
                }
                else
                {
                    schedule.Execute(() => refreshInfiniteChildInfosIfNeeded(true));
                }
            }
            else
            {
                refreshInfiniteChildInfosIfNeededInternal();
                _waitingForInfiniteRefreshComplete = false;
            }

        }

        void refreshInfiniteChildInfosIfNeededInternal()
        {
            recordNewInfiniteChildPositions();

            if (!_childrenBoundsHeight.HasValue || float.IsNaN(_childrenBoundsHeight.Value))
            {
                if (!_childrenBoundsHeight.HasValue)
                    _childrenBoundsHeight = calcChildrenBoundsHeight();

                if (!_childrenBoundsWidth.HasValue)
                    _childrenBoundsWidth = calcChildrenBoundsWidth();

                // We set the values of any absolutely positioned elements. We need this to
                // ensure style.top has a value that takes the absolute top and left into account.
                // Basically this copies any inline left/top position to style.left/top.
                var children = contentContainer.Children();
                foreach (var child in children)
                {
                    if (child.resolvedStyle.position == Position.Absolute)
                    {
                        child.style.top = child.getComputedStyle<Length>("top", child.resolvedStyle.top - child.resolvedStyle.marginTop);
                        child.style.left = child.getComputedStyle<Length>("left", child.resolvedStyle.left - child.resolvedStyle.marginLeft);
                    }
                }
            }
            
            _waitingForInfiniteRefreshComplete = false;
        }

        protected float calcChildrenBoundsWidth()
        {
            // Get child min max.
            float childMin = float.MaxValue;
            float childMax = float.MinValue;
            var children = contentContainer.Children();
            foreach (var child in children)
            {
                if (child.style.display == DisplayStyle.None)
                    continue;

                var childBB = child.worldBound;
                childMin = Mathf.Min(childMin, childBB.xMin - child.resolvedStyle.marginLeft);
                childMax = Mathf.Max(childMax, childBB.xMax + child.resolvedStyle.marginRight);
            }
            return childMax - childMin;
        }

        protected float calcChildrenBoundsHeight()
        { 
            // Get child min max.
            float childMin = float.MaxValue;
            float childMax = float.MinValue;
            var children = contentContainer.Children();
            foreach (var child in children)
            {
                if (child.style.display == DisplayStyle.None)
                    continue;

                var childBB = child.worldBound;
                childMin = Mathf.Min(childMin, childBB.yMin - child.resolvedStyle.marginTop);
                childMax = Mathf.Max(childMax, childBB.yMax + child.resolvedStyle.marginBottom);
            }
            return childMax - childMin;
        }

        // I HATE hidden state but it's the best I could come up with. TODO: investigate.
        protected Dictionary<VisualElement, float> _tmpInfinityPositionChangesX = new Dictionary<VisualElement, float>(10);
        protected int _lastUpdateChildPositionsInInfinityXFrame = 0;

        protected void updateChildPositionsInInfinityX(float scrollDirection)
        {
#if UNITY_EDITOR
            if (panel != null && panel.contextType == ContextType.Editor)
                return;
#endif
            
            // If we are still waiting for new elements to be recorded we delay the execution to avoid recording teleported positions. 
            if (_waitingForInfiniteAddComplete || _waitingForInfiniteRefreshComplete)
            {
                schedule.Execute(() => updateChildPositionsInInfinityX(scrollDirection));
                return;
            }

            // Abort there is not change.
            if (Mathf.Approximately(scrollDirection, 0f))
                return;

            // Execute this only ONCE per frame or else the contents of _tmpInfinityPositionChangesX
            // would be invalid.
            if (_lastUpdateChildPositionsInInfinityXFrame >= Time.frameCount)
                return;

            _lastUpdateChildPositionsInInfinityXFrame = Time.frameCount;


            if (!_childrenBoundsWidth.HasValue || float.IsNaN(_childrenBoundsWidth.Value))
                _childrenBoundsWidth = calcChildrenBoundsWidth();

            var viewBB = contentViewport.worldBound;
            // Use for infinity fix (see below).
            _tmpInfinityPositionChangesX.Clear();

            // Detect all children that are out of bounds and teleport them to the other side.
            // Initial state (move direction of content is ->) _ |_| _ _  -> new state (async, 2 teleported from right to left): _ _ _ |_|
            var children = contentContainer.Children();
            foreach (var child in children)
            {
                if (child.style.display == DisplayStyle.None)
                    continue;

                var childBB = child.worldBound;
                if (scrollDirection < -0.001f)
                {
                    // Is outside
                    if (childBB.xMax < viewBB.xMin)
                    {
                        float pos = child.style.left.value.value;
                        
                        // Support jumping big distances (bigger than _childrenBoundsWidth)
                        float distance = viewBB.xMin - childBB.xMax;
                        float jumps = Mathf.Ceil(distance / _childrenBoundsWidth.Value);

                        child.style.left = pos + _childrenBoundsWidth.Value * jumps;
                        
                        //Debug.Log($"Teleporting {child.name} to right");

                        // Memorize change for infinity fix (see below)
                        _tmpInfinityPositionChangesX.Add(child, _childrenBoundsWidth.Value * jumps);
                    }
                }
                else if (scrollDirection > 0.001f)
                {
                    // Is outside
                    if (childBB.xMin > viewBB.xMax)
                    {
                        float pos = child.style.left.value.value;
                        
                        // Support jumping big distances (bigger than _childrenBoundsWidth)
                        float distance = childBB.xMin - viewBB.xMax;
                        float jumps = Mathf.Ceil(distance / _childrenBoundsWidth.Value);
                        
                        child.style.left = pos - _childrenBoundsWidth.Value * jumps;
                        
                        //Debug.Log($"Teleporting {child.name} to left");

                        // Memorize change for infinity fix (see below)
                        _tmpInfinityPositionChangesX.Add(child, -_childrenBoundsWidth.Value * jumps);
                    }
                }
            }

            // Fix for selecting a child in the inverse direction of the update (primarily a problem in focusSnap).
            // Reason: If all children are on one side then focusing in the inverse direction
            // will not work since there is no child there to focus.
            // To solve this we take one child (if possible) and put it back on the other side.
            // The tricky part, which also trips up all the other code, is that changing the 
            // style.left.value does not immediately update the worldBounds. Instead it has to wait
            // for the layout engine to run. In that sense it is "async" and thus all other calculations
            // that are based on the worldBounds will be incorrect for the rest of the frame (scrollTo, sorting by position, ...).
            // To work around that we store the position change in a dictionary here and hand it over to any method that needs it.
            //
            // Initial state (async): _ _ _ |_|  -> put back state (async): _ _ |_| _
            // 1) Count children that could be put back. 
            // 2) If more than one then put one back, if less then do nothing.
            {
                children = contentContainer.Children();
                VisualElement childToPutBackLeft = null;
                VisualElement childToPutBackRight = null;
                float distanceLeft = 0f;
                float distanceRight = 0f;
                float maxDistanceLeft = 0f;
                float maxDistanceRight = 0f;

                int outLeft = 0;
                int outRight = 0;

                foreach (var child in children)
                {
                    if (child.style.display == DisplayStyle.None)
                        continue;

                    var childBB = child.worldBound;
                    float childxMin = childBB.xMin + getFromDict(_tmpInfinityPositionChangesX, child, 0f);
                    float childxMax = childBB.xMax + getFromDict(_tmpInfinityPositionChangesX, child, 0f);

                    if (childxMin > viewBB.xMax)
                    {
                        outRight++;
                        distanceRight = childxMin - viewBB.xMax;
                        if (distanceRight > maxDistanceRight)
                        {
                            maxDistanceRight = distanceRight;
                            childToPutBackRight = child;
                        }
                    }
                    else if (childxMax < viewBB.xMin)
                    {
                        outLeft++;
                        distanceLeft = viewBB.xMin - childxMax;
                        if (distanceLeft > maxDistanceLeft)
                        {
                            maxDistanceLeft = distanceLeft;
                            childToPutBackLeft = child;
                        }
                    }
                }

                //Debug.Log("Out left: " + outLeft + " out right: " + outRight);

                if (outLeft > 0 && outLeft > outRight)
                {
                    // put one child from left to right
                    float pos = childToPutBackLeft.style.left.value.value;
                    if (_tmpInfinityPositionChangesX.ContainsKey(childToPutBackLeft))
                    {
                        // invert style change
                        childToPutBackLeft.style.left = pos - _tmpInfinityPositionChangesX[childToPutBackLeft];
                    }
                    else
                    {
                        // move to other side
                        childToPutBackLeft.style.left = pos + _childrenBoundsWidth.Value;
                    }

                    //Debug.Log($"Moving {childToPutBackLeft.name} to the right");
                }
                else if (outRight > 0 && outRight > outLeft)
                {
                    // put one child from right to left
                    float pos = childToPutBackRight.style.left.value.value;
                    if (_tmpInfinityPositionChangesX.ContainsKey(childToPutBackRight))
                    {
                        // invert style change
                        childToPutBackRight.style.left = pos - _tmpInfinityPositionChangesX[childToPutBackRight];
                    }
                    else
                    {
                        // move to other side
                        childToPutBackRight.style.left = pos - _childrenBoundsWidth.Value;
                    }

                    //Debug.Log($"Moving {childToPutBackRight.name} to the left");
                }

                sortAllFocusables(_tmpInfinityPositionChangesX, _tmpInfinityPositionChangesY);

                _tmpInfinityPositionChangesX.Clear();
            }
        }

        protected Dictionary<VisualElement, float> _tmpInfinityPositionChangesY = new Dictionary<VisualElement, float>(10);
        protected int _lastUpdateChildPositionsInInfinityYFrame = 0;

        protected void updateChildPositionsInInfinityY(float scrollDirection)
        {
#if UNITY_EDITOR
            if (panel != null && panel.contextType == ContextType.Editor)
                return;
#endif
            
            // If we are still waiting for new elements to be recorded we delay the execution to avoid recording teleported positions. 
            if (_waitingForInfiniteAddComplete || _waitingForInfiniteRefreshComplete)
            {
                schedule.Execute(() => updateChildPositionsInInfinityY(scrollDirection));
                return;
            }
            
            if (!_childrenBoundsHeight.HasValue || float.IsNaN(_childrenBoundsHeight.Value))
                _childrenBoundsHeight = calcChildrenBoundsHeight();

            // bool didTeleportElements = false; // TODO: No longer used, remove?
            var viewBB = contentViewport.worldBound;
            // Use for infinity fix (see below).
            _tmpInfinityPositionChangesY.Clear();

            // Detect all children that are out of bounds and teleport them to the other side.
            var children = contentContainer.Children();
            foreach (var child in children)
            {
                if (child.style.display == DisplayStyle.None)
                    continue;

                var childBB = child.worldBound;
                if (scrollDirection < -0.001f)
                {
                    // Is outside
                    if (childBB.yMax < viewBB.yMin)
                    {
                        float pos = child.style.top.value.value;
                        
                        // Support jumping big distances (bigger than _childrenBoundsWidth)
                        float distance = viewBB.yMin - childBB.yMax;
                        float jumps = Mathf.Ceil(distance / _childrenBoundsHeight.Value);
                        
                        child.style.top = pos + _childrenBoundsHeight.Value * jumps;
                        
                        // Memorize change for infinity fix (see below)
                        _tmpInfinityPositionChangesY.Add(child, _childrenBoundsHeight.Value * jumps);
                    }
                }
                else if (scrollDirection > 0.001f)
                {
                    // Is outside
                    if (childBB.yMin > viewBB.yMax)
                    {
                        float pos = child.style.top.value.value;
                        
                        // Support jumping big distances (bigger than _childrenBoundsWidth)
                        float distance = childBB.yMin - viewBB.yMax;
                        float jumps = Mathf.Ceil(distance / _childrenBoundsHeight.Value);
                        
                        child.style.top = pos - _childrenBoundsHeight.Value * jumps;
                        
                        // Memorize change for infinity fix (see below)
                        _tmpInfinityPositionChangesY.Add(child, -_childrenBoundsHeight.Value * jumps);
                    }
                }
            }

            // Fix for selecting a child in the inverse direction of the update (primarily a problem in focusSnap).
            // See explanation in Y (above)
            {
                children = contentContainer.Children();
                VisualElement childToPutBackTop = null;
                VisualElement childToPutBackBottom = null;
                float distanceTop = 0f;
                float distanceBottom = 0f;
                float maxDistanceTop = 0f;
                float maxDistanceBottom = 0f;

                int outTop = 0;
                int outBottom = 0;

                foreach (var child in children)
                {
                    if (child.style.display == DisplayStyle.None)
                        continue;

                    var childBB = child.worldBound;
                    float childyMin = childBB.yMin + getFromDict(_tmpInfinityPositionChangesY, child, 0f);
                    float childyMax = childBB.yMax + getFromDict(_tmpInfinityPositionChangesY, child, 0f);

                    if (childyMin > viewBB.yMax)
                    {
                        outBottom++;
                        distanceBottom = childyMin - viewBB.yMax;
                        if (distanceBottom > maxDistanceBottom)
                        {
                            maxDistanceBottom = distanceBottom;
                            childToPutBackBottom = child;
                        }
                    }
                    else if (childyMax < viewBB.yMin)
                    {
                        outTop++;
                        distanceTop = viewBB.yMin - childyMax;
                        if (distanceTop > maxDistanceTop)
                        {
                            maxDistanceTop = distanceTop;
                            childToPutBackTop = child;
                        }
                    }
                }

                //Debug.Log("Out top: " + outTop + " out bottom: " + outBottom);

                if (outTop > 0 && outTop > outBottom)
                {
                    // put one child from top to bottom
                    float pos = childToPutBackTop.style.top.value.value;
                    if (_tmpInfinityPositionChangesY.ContainsKey(childToPutBackTop))
                    {
                        // invert style change
                        childToPutBackTop.style.top = pos - _tmpInfinityPositionChangesY[childToPutBackTop];
                    }
                    else
                    {
                        // move to other side
                        childToPutBackTop.style.top = pos + _childrenBoundsHeight.Value;
                    }

                    //Debug.Log($"Moving {childToPutBackTop.name} to the bottom");
                }
                else if (outBottom > 0 && outBottom > outTop)
                {
                    // put one child from bottom to top
                    float pos = childToPutBackBottom.style.top.value.value;
                    if (_tmpInfinityPositionChangesY.ContainsKey(childToPutBackBottom))
                    {
                        // invert style change
                        childToPutBackBottom.style.top = pos - _tmpInfinityPositionChangesY[childToPutBackBottom];
                    }
                    else
                    {
                        // move to other side
                        childToPutBackBottom.style.top = pos - _childrenBoundsHeight.Value;
                    }

                    //Debug.Log($"Moving {childToPutBackBottom.name} to the top");
                }

                sortAllFocusables(_tmpInfinityPositionChangesX, _tmpInfinityPositionChangesY);

                _tmpInfinityPositionChangesY.Clear();
            }
        }
    }
}
