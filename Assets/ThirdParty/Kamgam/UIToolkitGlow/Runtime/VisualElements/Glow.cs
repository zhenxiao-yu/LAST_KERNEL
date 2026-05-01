using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitGlow
{
    /// <summary>
    /// Each Glow creates a new manipulator that is not part of the config / UIDocument workflow.
    /// This uses a completely separate local modifier for each glow element.<br />
    /// The glow actually is a glow manipulator with some special settings (fill center). 
    /// </summary>
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class Glow : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "kamgam-glow";

        // Width
        const float WidthDefault = 30f;
        protected float _width = WidthDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("width")]
#endif
        public float width
        {
            get => _width;
            set
            {
                if (_width == value) 
                    return;

                _width = value;

                getConfig().Width = _width;
            }
        }

        // Overlap Width
        const float OverlapWidthDefault = 0.2f;
        protected float _overlapWidth = OverlapWidthDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("overlap-width")]
#endif
        public float overlapWidth
        {
            get => _overlapWidth;
            set
            {
                if (_overlapWidth == value)
                    return;

                _overlapWidth = value;

                getConfig().OverlapWidth = _overlapWidth;
            }
        }

        // Split Width
        const bool SplitWidthDefault = false;
        protected bool _splitWidth = SplitWidthDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("split-width")]
#endif
        public bool splitWidth
        {
            get => _splitWidth;
            set
            {
                if (_splitWidth == value)
                    return;

                _splitWidth = value;

                getConfig().SplitWidth = _splitWidth;
            }
        }

        // Width Left
        const float WidthLeftDefault = WidthDefault;
        protected float _widthLeft = WidthLeftDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("width-left")]
#endif
        public float widthLeft
        {
            get => _widthLeft;
            set
            {
                if (_widthLeft == value)
                    return;

                _widthLeft = value;

                var widths = getConfig().Widths;
                widths.Left = _widthLeft;
                getConfig().Widths = widths;
            }
        }

        // Width Top
        const float WidthTopDefault = WidthDefault;
        protected float _widthTop = WidthTopDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("width-top")]
#endif
        public float widthTop
        {
            get => _widthTop;
            set
            {
                if (_widthTop == value)
                    return;

                _widthTop = value;

                var widths = getConfig().Widths;
                widths.Top = _widthTop;
                getConfig().Widths = widths;
            }
        }

        // Width Right
        const float WidthRightDefault = WidthDefault;
        protected float _widthRight = WidthRightDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("width-right")]
#endif
        public float widthRight
        {
            get => _widthRight;
            set
            {
                if (_widthRight == value)
                    return;

                _widthRight = value;

                var widths = getConfig().Widths;
                widths.Right = _widthRight;
                getConfig().Widths = widths;
            }
        }

        // Width Bottom
        const float WidthBottomDefault = WidthDefault;
        protected float _widthBottom = WidthBottomDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("width-bottom")]
#endif
        public float widthBottom
        {
            get => _widthBottom;
            set
            {
                if (_widthBottom == value)
                    return;

                _widthBottom = value;

                var widths = getConfig().Widths;
                widths.Bottom = _widthBottom;
                getConfig().Widths = widths;
            }
        }

        // Inner Color
        static readonly Color InnerColorDefault = new Color(0f, 1f, 1f, 0.5f);
        protected Color _innerColor = InnerColorDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("inner-color")]
#endif
        public Color innerColor
        {
            get => _innerColor;
            set
            {
                if (_innerColor == value)
                    return;

                _innerColor = value;
                getConfig().InnerColor = _innerColor;
            }
        }

        // Outer Color
        static readonly Color OuterColorDefault = new Color(0f, 1f, 1f, 0f);
        protected Color _outerColor = OuterColorDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("outer-color")]
#endif
        public Color outerColor
        {
            get => _outerColor;
            set
            {
                if (_outerColor == value)
                    return;

                _outerColor = value;

                getConfig().OuterColor = _outerColor;
            }
        }

        // Offset X
        const float OffsetXDefault = 0f;
        protected float _offsetX = OffsetXDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("offset-x")]
#endif
        public float offsetX
        {
            get => _offsetX;
            set
            {
                if (_offsetX == value)
                    return;

                _offsetX = value;

                var offset = getConfig().Offset;
                offset.x = _offsetX;
                getConfig().Offset = offset;
            }
        }

        // Offset Y
        const float OffsetYDefault = 0f;
        protected float _offsetY = OffsetYDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("offset-y")]
#endif
        public float offsetY
        {
            get => _offsetY;
            set
            {
                if (_offsetY == value)
                    return;

                _offsetY = value;

                var offset = getConfig().Offset;
                offset.y = _offsetY;
                getConfig().Offset = offset;
            }
        }

        // Offset Everything
        const bool OffsetEverythingDefault = false;
        protected bool _offsetEverything = OffsetEverythingDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("offset-everything")]
#endif
        public bool offsetEverything
        {
            get => _offsetEverything;
            set
            {
                if (_offsetEverything == value)
                    return;

                _offsetEverything = value;

                getConfig().OffsetEverything = _offsetEverything;
            }
        }

        // Scale X
        const float ScaleXDefault = 1f;
        protected float _scaleX = ScaleXDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("scale-x")]
#endif
        public float scaleX
        {
            get => _scaleX;
            set
            {
                if (_scaleX == value)
                    return;

                _scaleX = value;

                var scale = getConfig().Scale;
                scale.x = _scaleX;
                getConfig().Scale = scale;
            }
        }

        // Scale Y
        const float ScaleYDefault = 1f;
        protected float _scaleY = ScaleYDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("scale-y")]
#endif
        public float scaleY
        {
            get => _scaleY;
            set
            {
                if (_scaleY == value)
                    return;

                _scaleY = value;

                var scale = getConfig().Scale;
                scale.y = _scaleY;
                getConfig().Scale = scale;
            }
        }

        // Inherit Border Colors
        const bool InheritBorderColorsDefault = false;
        protected bool _inheritBorderColors = InheritBorderColorsDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("inherit-border-colors")]
#endif
        public bool inheritBorderColors
        {
            get => _inheritBorderColors;
            set
            {
                if (_inheritBorderColors == value)
                    return;

                _inheritBorderColors = value;

                getConfig().InheritBorderColors = _inheritBorderColors;
            }
        }

        // Force Subdivision
        const bool ForceSubdivisionDefault = false;
        protected bool _forceSubdivision = ForceSubdivisionDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("force-subdivision")]
#endif
        public bool forceSubdivision
        {
            get => _forceSubdivision;
            set
            {
                if (_forceSubdivision == value)
                    return;

                _forceSubdivision = value;

                getConfig().ForceSubdivision = _forceSubdivision;
            }
        }

        // Preserve Hard Edges
        const bool PreserveHardCornersDefault = false;
        protected bool _preserveHardCorners = PreserveHardCornersDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("preserve-hard-corners")]
#endif
        public bool preserveHardCorners
        {
            get => _preserveHardCorners;
            set
            {
                if (_preserveHardCorners == value)
                    return;

                _preserveHardCorners = value;

                getConfig().PreserveHardCorners = _preserveHardCorners;
            }
        }

        // Fill Center
        const bool FillCenterDefault = false;
        protected bool _fillCenter = FillCenterDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("fill-center")]
#endif
        public bool fillCenter
        {
            get => _fillCenter;
            set
            {
                if (_fillCenter == value)
                    return;

                _fillCenter = value;

                getConfig().FillCenter = _fillCenter;
            }
        }

        // Vertices per corner
        const float VertexDistanceDefault = 15;
        protected float _vertexDistance = VertexDistanceDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("vertex-distance")]
#endif
        public float vertexDistance
        {
            get => _vertexDistance;
            set
            {
                if (_vertexDistance == value)
                    return;

                value = Mathf.Max(value, 1);

                _vertexDistance = value;

                getConfig().VertexDistance = _vertexDistance;
            }
        }

        // Layout First Child
        const bool LayoutFirstChildDefault = false;
        protected bool _layoutFirstChild = LayoutFirstChildDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("layout-first-child")]
#endif
        public bool layoutFirstChild
        {
            get => _layoutFirstChild;
            set
            {
                if (_layoutFirstChild == value)
                    return;

                _layoutFirstChild = value;
                UpdateGlowManipulator();
                
                if (layoutFirstChild)
                    DoLayoutFirstChild();
                else
                    ClearFirstChildLayoutStyles();
            }
        }

        // Animation
        const string AnimationNameDefault = null;
        protected string _animationName = AnimationNameDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("animation-name")]
#endif
        public string animationName
        {
            get => _animationName;
            set
            {
                if (_animationName == value)
                    return;

                if (value != null)
                    value = value.Trim();

                if (string.IsNullOrEmpty(value))
                    value = null;

                _animationName = value;
                updateAnimation();
            }
        }

        [System.NonSerialized]
        protected GlowConfig _config;

        [System.NonSerialized]
        protected GlowManipulator _glowManipulator;
        public GlowManipulator manipulator => _glowManipulator;

        [System.NonSerialized]
        protected IGlowAnimation _glowAnimation;
        public IGlowAnimation animation => _glowAnimation;

        public T GetAnimation<T>() where T : class, IGlowAnimation
        {
            return (T) _glowAnimation;
        }

        /// <summary>
        /// Shortcut for the first child element. 
        /// </summary>
        public VisualElement content
        {
            get
            {
                if (childCount == 0)
                    return null;

                return ElementAt(0);
            }
        }

#if !UNITY_6000_0_OR_NEWER
        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<Glow, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription _width =
                new UxmlFloatAttributeDescription { name = "width", defaultValue = WidthDefault };

            UxmlFloatAttributeDescription _overlapWidth =
                new UxmlFloatAttributeDescription { name = "overlap-width", defaultValue = OverlapWidthDefault };

            UxmlBoolAttributeDescription _splitWidth =
                new UxmlBoolAttributeDescription { name = "split-width", defaultValue = SplitWidthDefault };

            UxmlFloatAttributeDescription _widthLeft =
                new UxmlFloatAttributeDescription { name = "width-left", defaultValue = WidthLeftDefault };

            UxmlFloatAttributeDescription _widthTop =
                new UxmlFloatAttributeDescription { name = "width-top", defaultValue = WidthTopDefault };

            UxmlFloatAttributeDescription _widthRight =
                new UxmlFloatAttributeDescription { name = "width-right", defaultValue = WidthRightDefault };

            UxmlFloatAttributeDescription _widthBottom =
                new UxmlFloatAttributeDescription { name = "width-bottom", defaultValue = WidthBottomDefault };

            UxmlFloatAttributeDescription _offsetX =
                new UxmlFloatAttributeDescription { name = "offset-x", defaultValue = OffsetXDefault };

            UxmlFloatAttributeDescription _offsetY =
                new UxmlFloatAttributeDescription { name = "offset-y", defaultValue = OffsetYDefault };

            UxmlBoolAttributeDescription _offsetEverything =
                new UxmlBoolAttributeDescription { name = "offset-everything", defaultValue = OffsetEverythingDefault };

            UxmlFloatAttributeDescription _scaleX =
                new UxmlFloatAttributeDescription { name = "scale-x", defaultValue = ScaleXDefault };

            UxmlFloatAttributeDescription _scaleY =
                new UxmlFloatAttributeDescription { name = "scale-y", defaultValue = ScaleYDefault };

            UxmlColorAttributeDescription _innerColor =
                new UxmlColorAttributeDescription { name = "inner-color", defaultValue = InnerColorDefault };

            UxmlColorAttributeDescription _outerColor =
                new UxmlColorAttributeDescription { name = "outer-color", defaultValue = OuterColorDefault };

            UxmlBoolAttributeDescription _inheritBorderColors =
                new UxmlBoolAttributeDescription { name = "inherit-border-colors", defaultValue = InheritBorderColorsDefault };

            UxmlBoolAttributeDescription _forceSubdivision =
                new UxmlBoolAttributeDescription { name = "force-subdivision", defaultValue = ForceSubdivisionDefault };

            UxmlBoolAttributeDescription _preserveHardCorners =
                new UxmlBoolAttributeDescription { name = "preserve-hard-corners", defaultValue = PreserveHardCornersDefault };

            UxmlBoolAttributeDescription _fillCenter =
                new UxmlBoolAttributeDescription { name = "fill-center", defaultValue = FillCenterDefault };

            UxmlFloatAttributeDescription _verticexDistance =
                new UxmlFloatAttributeDescription { name = "vertex-distance", defaultValue = VertexDistanceDefault };

            UxmlBoolAttributeDescription _layoutFirstChild =
                new UxmlBoolAttributeDescription { name = "layout-first-child", defaultValue = LayoutFirstChildDefault };

            UxmlStringAttributeDescription _animationName =
                new UxmlStringAttributeDescription { name = "animation-name", defaultValue = AnimationNameDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var glow = ve as Glow;

                glow.width = _width.GetValueFromBag(bag, cc);
                glow.overlapWidth = _overlapWidth.GetValueFromBag(bag, cc);
                glow.splitWidth = _splitWidth.GetValueFromBag(bag, cc);
                glow.widthLeft = _widthLeft.GetValueFromBag(bag, cc);
                glow.widthTop = _widthTop.GetValueFromBag(bag, cc);
                glow.widthRight = _widthRight.GetValueFromBag(bag, cc);
                glow.widthBottom = _widthBottom.GetValueFromBag(bag, cc);
                glow.offsetX = _offsetX.GetValueFromBag(bag, cc);
                glow.offsetY = _offsetY.GetValueFromBag(bag, cc);
                glow.offsetEverything = _offsetEverything.GetValueFromBag(bag, cc);
                glow.scaleX = _scaleX.GetValueFromBag(bag, cc);
                glow.scaleY = _scaleY.GetValueFromBag(bag, cc);
                glow.innerColor = _innerColor.GetValueFromBag(bag, cc);
                glow.outerColor = _outerColor.GetValueFromBag(bag, cc);
                glow.inheritBorderColors = _inheritBorderColors.GetValueFromBag(bag, cc);
                glow.forceSubdivision = _forceSubdivision.GetValueFromBag(bag, cc);
                glow.preserveHardCorners = _preserveHardCorners.GetValueFromBag(bag, cc);
                glow.fillCenter = _fillCenter.GetValueFromBag(bag, cc);
                glow.vertexDistance = _verticexDistance.GetValueFromBag(bag, cc);
                glow.layoutFirstChild = _layoutFirstChild.GetValueFromBag(bag, cc);
                glow.animationName = _animationName.GetValueFromBag(bag, cc);
            }
        }
#endif

        public Glow()
        {
            AddToClassList(ussClassName);
            this.style.overflow = Overflow.Visible;

            // Init config
            _config = getConfig();

            // Register callbacks
            generateVisualContent += onGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(onAttachToPanel);
            RegisterCallback<GeometryChangedEvent>(onGeometryChanged);
        }

        protected void onAttachToPanel(AttachToPanelEvent evt)
        {
            UpdateGlowManipulator();
            if (layoutFirstChild)
                DoLayoutFirstChild();
            MarkDirtyRepaint();
        }

        protected void onGeometryChanged(GeometryChangedEvent evt)
        {
            if (layoutFirstChild)
            {
                UpdateGlowManipulator();
                DoLayoutFirstChild();
            }
        }

        protected void onGenerateVisualContent(MeshGenerationContext context)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying && layoutFirstChild)
            {
                //if(layoutFirstChild)
                    // schedule.Execute(DoLayoutFirstChild).ExecuteLater(1);
            }
#endif
        }

        protected GlowConfig getConfig()
        {
            if (_config == null)
                _config = createGlowConfig();

            return _config;
        }

        protected GlowConfig createGlowConfig()
        {
            var config = new GlowConfig();

            // Don't remove glow configs based on class names.
            config.RemoveIfClassIsNoLongerPresentOnTarget = false;

            config.Width = width;
            config.SplitWidth = splitWidth;
            config.Widths = new GlowConfig.DirectionValues(
                widthTop, widthRight, widthBottom, widthLeft);
            config.Scale = new Vector2(scaleX, scaleY);

            config.Offset = new Vector2(offsetX, offsetY);
            config.OffsetEverything = offsetEverything;

            config.InnerColor = innerColor;
            config.OuterColor = outerColor;
            config.InheritBorderColors = inheritBorderColors;
            config.UseRadialGradients = false;
            //config.InnerColors = default;
            //config.OuterColors = default;

            config.OverlapWidth = overlapWidth;
            config.ForceSubdivision = forceSubdivision;
            config.PreserveHardCorners = preserveHardCorners;
            config.FillCenter = fillCenter;
            config.VertexDistance = vertexDistance;

            return config;
        }

        public void UpdateGlowManipulator()
        {
            if (this.panel == null)
                return;

            // There is no manipulator yet -> find or create one.
            if (_glowManipulator == null)
            {
                _glowManipulator = new GlowManipulator(getConfig());
                _glowManipulator.RemoveOnPlayModeStateChange = false;
                
                this.AddManipulator(_glowManipulator);
            }
            
            // TODO: Find out why the manipulator gets detached.
            if (_glowManipulator.target == null)
            {
                _glowManipulator.Config = getConfig();
                this.AddManipulator(_glowManipulator);
            }

            updateAnimation();
        }

        private bool _loggedMissingConfigRoot = false;

        protected void updateAnimation()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (animationName == null && _glowAnimation == null)
                return;

            // animation
            if (_glowManipulator != null)
            {
                // Remove animation if animation name changed or removed.
                if (_glowAnimation != null && _glowAnimation.Name != animationName)
                {
                    _glowAnimation.RemoveFromManipulator(_glowManipulator);
                    _glowAnimation = null;
                }

                if (animationName == null)
                    return;

                // Add new animation.
                if (_glowAnimation == null
                    && this.panel != null 
                    && this.panel.contextType == ContextType.Player)
                {
                    // Check and warn about missing config root or animation.
                    var configRoot = GlowConfigRoot.FindConfigRoot();
                    if (configRoot == null && !_loggedMissingConfigRoot)
                    {
                        _loggedMissingConfigRoot = true;
                        Logger.LogWarning("You are trying to add the animation '" + animationName + "' to '" + this + " but there was no config root found. Please make sure you have a GlowDocument with a root added to your UI Document.");
                    }
                    else
                    {
                        _glowAnimation = GlowAnimation.AddAnimationCopyTo(animationName, _glowManipulator, configRoot, linkToTemplate: true);
                        if (_glowAnimation != null)
                        {
                            _glowAnimation.Play();
                        }
                    }
                }
            }
        }

        public void DoLayoutFirstChild()
        {
            if (childCount == 0)
                return;
            
            var firstChild = ElementAt(0);
            firstChild.style.borderTopLeftRadius = resolvedStyle.borderTopLeftRadius;
            firstChild.style.borderTopRightRadius = resolvedStyle.borderTopRightRadius;
            firstChild.style.borderBottomLeftRadius = resolvedStyle.borderBottomLeftRadius;
            firstChild.style.borderBottomRightRadius = resolvedStyle.borderBottomRightRadius;

            firstChild.style.flexGrow = 1f;
            firstChild.style.flexShrink = 1f;
            firstChild.style.minWidth = new Length(100f, LengthUnit.Percent);
            firstChild.style.minHeight = new Length(100f, LengthUnit.Percent);

            firstChild.style.marginTop = 0f;
            firstChild.style.marginRight = 0f;
            firstChild.style.marginBottom = 0f;
            firstChild.style.marginLeft = 0f;
        }

        public void ClearFirstChildLayoutStyles()
        {
            if (childCount == 0)
                return;
            
            var firstChild = ElementAt(0);
            firstChild.style.borderTopLeftRadius = StyleKeyword.Null;
            firstChild.style.borderTopRightRadius = StyleKeyword.Null;
            firstChild.style.borderBottomLeftRadius = StyleKeyword.Null;
            firstChild.style.borderBottomRightRadius = StyleKeyword.Null;

            firstChild.style.flexGrow = StyleKeyword.Null;
            firstChild.style.flexShrink = StyleKeyword.Null;
            firstChild.style.minWidth = StyleKeyword.Null;
            firstChild.style.minHeight = StyleKeyword.Null;

            firstChild.style.marginTop = StyleKeyword.Null;
            firstChild.style.marginRight = StyleKeyword.Null;
            firstChild.style.marginBottom = StyleKeyword.Null;
            firstChild.style.marginLeft = StyleKeyword.Null;
        }
    }
}