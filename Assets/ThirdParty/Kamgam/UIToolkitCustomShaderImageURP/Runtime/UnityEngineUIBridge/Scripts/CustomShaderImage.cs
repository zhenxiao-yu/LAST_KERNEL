using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;


namespace Kamgam.UIToolkitCustomShaderImageURP
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class CustomShaderImage : ImmediateModeElement
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<CustomShaderImage, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlAssetAttributeDescription<Material> m_SharedMaterial =
                new UxmlAssetAttributeDescription<Material> { name = "shared-material" };
            
            private UxmlAssetAttributeDescription<Sprite> m_Texture =
                new UxmlAssetAttributeDescription<Sprite> { name = "sprite" };
            
            private UxmlEnumAttributeDescription<BlendMode> m_SrcBlend =
                new UxmlEnumAttributeDescription<BlendMode> { name = "src-blend", defaultValue = BlendMode.SrcAlpha };
            
            private UxmlEnumAttributeDescription<BlendMode> m_DstBlend =
                new UxmlEnumAttributeDescription<BlendMode> { name = "dst-blend", defaultValue = BlendMode.OneMinusSrcAlpha};
            
            private UxmlBoolAttributeDescription m_UseSharedMaterial =
                new UxmlBoolAttributeDescription { name = "use-shared-material", defaultValue = false };

            private UxmlIntAttributeDescription m_MeshCornerSegments =
                new UxmlIntAttributeDescription { name = "mesh-corner-segments", defaultValue = 20 };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var element = (CustomShaderImage)ve;
                element.SharedMaterial = m_SharedMaterial.GetValueFromBag(bag, cc);
                element.UseSharedMaterial = m_UseSharedMaterial.GetValueFromBag(bag, cc);
                element.Sprite = m_Texture.GetValueFromBag(bag, cc);
                element.MeshCornerSegments = m_MeshCornerSegments.GetValueFromBag(bag, cc);
                element.SrcBlend = m_SrcBlend.GetValueFromBag(bag, cc);
                element.DstBlend = m_DstBlend.GetValueFromBag(bag, cc);
            }
        }
#endif
        
        public CustomShaderImage()
        {
            RegisterCallback<AttachToPanelEvent>(onAttach);
            RegisterCallback<DetachFromPanelEvent>(onDetach);
        }

        private bool m_isAttached;
        private IVisualElementScheduledItem m_styleCheckScheduledItem;
        
        private void onAttach(AttachToPanelEvent evt)
        {
            m_isAttached = true;
            RegisterCallback<GeometryChangedEvent>(onGeometryChanged);
            
            // Since we do not have a nice way of checking for style changes we do this.
            CheckStyles();
            m_styleCheckScheduledItem = schedule.Execute(CheckStyles).Every(50).Until( () => !m_isAttached || panel == null);
            
            updateOverflowInformation(triggerMeshGeneration:false);
        }
        
        private void onDetach(DetachFromPanelEvent evt)
        {
            m_isAttached = false;
            m_styleCheckScheduledItem.Pause();
            UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);
            
            foreach (var overflowParent in m_overflowParents)
            {
                overflowParent.UnregisterCallback<GeometryChangedEvent>(onOverflowParentGeometryChanged);
            }
            m_overflowParents.Clear();
        }

        private bool m_borderStylesInitialized;
        private float m_styleBorderTopWidth;
        private float m_styleBorderRightWidth;
        private float m_styleBorderBottomWidth;
        private float m_styleBorderLeftWidth;
        private float m_styleBorderTopLeftRadius;
        private float m_styleBorderTopRightRadius;
        private float m_styleBorderBottomLeftRadius;
        private float m_styleBorderBottomRightRadius;

        Material m_sharedMaterial;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("shared-material")]
#endif
        public Material SharedMaterial
        {
            get => m_sharedMaterial;
            set
            {
                if (value == m_sharedMaterial)
                    return;
                
                m_sharedMaterial = value;
                m_cachedMaterial = null;
            }
        }
        
        Sprite m_sprite;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("sprite")]
#endif
        public Sprite Sprite
        {
            get => m_sprite;
            set
            {
                if (value == m_sprite)
                    return;
                
                m_sprite = value;
                m_cachedMaterial = null;
            }
        }

        /// <summary>
        /// Works similar to the "material" property of a MeshRenderer, meaning it will instantiate a new material per instance if needed.<br />
        /// If you want to change the shared base material then use SharedMaterial instead. 
        /// </summary>
        public Material Material
        {
            get
            {
                if (UseSharedMaterial)
                {
#if UNITY_EDITOR
                    if (m_cachedMaterial == null)
                    {
                        Debug.LogWarning("Calling 'Material' on an image that uses a shared material will instantiate a new material and set UseSharedMaterial to FALSE. Try using SharedMaterial instead.");
                    }
#endif
                    UseSharedMaterial = false;
                }
                
                return getMaterial();
            }

            set
            {
                if (m_cachedMaterial != value)
                {
                    m_cachedMaterial = value;
                    initMaterial(m_cachedMaterial);
                }
            }
        }

        bool m_useSharedMaterial = false;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("use-shared-material")]
#endif
        public bool UseSharedMaterial
        {
            get => m_useSharedMaterial;
            set
            {
                if (value == m_useSharedMaterial)
                    return;
                
                m_useSharedMaterial = value;
                m_cachedMaterial = null;
            }
        }
        
        BlendMode m_srcBlend = BlendMode.SrcAlpha;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("src-blend")]
#endif
        public BlendMode SrcBlend
        {
            get => m_srcBlend;
            set
            {
                if (value == m_srcBlend)
                    return;
                
                m_srcBlend = value;
                m_cachedMaterial = null;
            }
        }

        private BlendMode m_dstBlend = BlendMode.OneMinusSrcAlpha;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("dst-blend")]
#endif
        public BlendMode DstBlend
        {
            get => m_dstBlend;
            set
            {
                if (value == m_dstBlend)
                    return;
                
                m_dstBlend = value;
                m_cachedMaterial = null;
            }
        }
        
        int m_meshCornerSegments = 20;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("mesh-corner-segments")]
#endif
        public int MeshCornerSegments
        {
            get => m_meshCornerSegments;
            set
            {
                if (value == m_meshCornerSegments)
                    return;
                
                m_meshCornerSegments = Mathf.Max(1, value);
                GenerateMesh();
            }
        }
        
        protected int getResolvedMeshCornerSegments()
        {
            return CustomShaderImageStyles.ResolveStyle(CustomShaderImageStyles.MeshCornerSegments, this, MeshCornerSegments);
        }

        
        public float MeshCornerOverlap = 0.3f;

        protected float getResolvedMeshCornerOverlap()
        {
            return CustomShaderImageStyles.ResolveStyle(CustomShaderImageStyles.MeshCornerOverlap, this, MeshCornerOverlap);
        }

        protected Rect? m_overflowContentRect = null;
        protected List<VisualElement> m_overflowParents = new List<VisualElement>();
        protected List<VisualElement> m_tmpOverflowParents = new List<VisualElement>();

        protected Material m_cachedMaterial;
        protected bool m_isShaderGraphShader;

        Material getMaterial()
        {
            // Material
            if (m_cachedMaterial == null)
            {
                if (SharedMaterial != null)
                {
                    if (UseSharedMaterial)
                    {
                        m_cachedMaterial = SharedMaterial;
                    }
                    else
                    {
                        m_cachedMaterial = new Material(SharedMaterial);
                        m_cachedMaterial.name = SharedMaterial.name + "CustomShaderImageCopy";
                        m_cachedMaterial.hideFlags = HideFlags.HideAndDontSave;
                    }

                    if (m_cachedMaterial != null)
                    {
                        initMaterial(m_cachedMaterial);
                    }
                }
                else
                {
                    // TODO: Try to fall back on the default material.
                    // See: https://github.com/Unity-Technologies/UnityCsReference/blob/10f8718268a7e34844ba7d59792117c28d75a99b/Modules/UIElements/Core/Renderer/UIRShaders.cs#L24
                    /*
                    string shaderName = null;
                    if (panel.contextType == ContextType.Editor) 
                        shaderName = Shaders.k_Editor;
                    else
                        shaderName = Shaders.k_Runtime;
                    
                    var shader = Shader.Find(shaderName);
                    if (shader != null)
                    {
                        m_cachedMaterial = new Material(shader);
                        m_cachedMaterial.hideFlags = HideFlags.DontSave;
                    }
                    */
                }

                if (m_cachedMaterial == null)
                    return null;
                
                // Texture
                if (Sprite != null)
                {
                    Texture2D texture = Sprite.texture;
                    if (texture != null)
                        m_cachedMaterial.mainTexture = texture;
                }

                // Detect if it is a shadergraph shader.
                m_isShaderGraphShader = IsShaderGraphShader(m_cachedMaterial.shader);
            }

            return m_cachedMaterial;
        }

        private void initMaterial(Material mat)
        {
            mat.SetInt("_SrcBlend", (int)SrcBlend);
            mat.SetInt("_DstBlend", (int)DstBlend);
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.SetInt("_ZWrite", 0);
        }

        bool? m_lastIsDrawnValue; // Nullable to ensure we catch the initial change.
        
        // Called only if the element changed.
        protected virtual void onGeometryChanged(GeometryChangedEvent evt)
        {
            // Update Is Drawn
            bool isDrawn = IsDrawn();
            if (!m_lastIsDrawnValue.HasValue || m_lastIsDrawnValue.Value != isDrawn)
            {
                m_lastIsDrawnValue = isDrawn;
            }

            if (isDrawn)
            {
                updateOverflowInformation(triggerMeshGeneration: false);
                
                // The generated mesh is stored in _vertices and _indices.
                GenerateMesh();
            }
        }
        
        protected void updateOverflowInformation(bool triggerMeshGeneration)
        {
            // We keep track of what parent elements already have the callback registered an only
            // register/unregister on demand at the end.
            m_tmpOverflowParents.Clear();
            m_tmpOverflowParents.AddRange(m_overflowParents);
            m_overflowParents.Clear();
            
            // Recalculate overflow.
            m_overflowContentRect = null;

            if (float.IsNaN(rect.width))
                return;
                
            // Update overflow rect
            // SADLY we have to use some internal types since Unity does NOT expose the overflow
            // property in resolvedStyle.
            // https://discussions.unity.com/t/ui-toolkit-visualelement-resolvedstyle-missing-overflow-style-property/1520499/5
            // *sigh* Thanks Unity.
            VisualElement current = parent;
            while (current != null)
            {
                if (current.computedStyle.overflow == OverflowInternal.Hidden)
                {
                    if (!m_overflowContentRect.HasValue)
                    {
                        m_overflowContentRect = new Rect(0, 0, float.MaxValue, float.MaxValue);
                    }
                    
                    var newRect = convertContentRectTo(current, this);
                    var existingRect = m_overflowContentRect.Value;
                        existingRect.xMin = Mathf.Max(m_overflowContentRect.Value.xMin, newRect.xMin);
                        existingRect.xMax = Mathf.Min(m_overflowContentRect.Value.xMax, newRect.xMax);
                        existingRect.yMin = Mathf.Max(m_overflowContentRect.Value.yMin, newRect.yMin);
                        existingRect.yMax = Mathf.Min(m_overflowContentRect.Value.yMax, newRect.yMax);
                    m_overflowContentRect = existingRect;
                    
                    m_overflowParents.Add(current);
                }

                current = current.parent;
            }

            // Register/Unregister listeners (we do this to avoid unnecessary delegate allocations with every redraw).
            foreach (var p in m_overflowParents)
            {
                if (m_tmpOverflowParents.Contains(p))
                {
                    // Is in both previous and current list -> do nothing.
                    m_tmpOverflowParents.Remove(p);
                    continue;
                }
                else
                {
                    // Is new in current list -> do register callback.
                    p.RegisterCallback<GeometryChangedEvent>(onOverflowParentGeometryChanged);
                }
            }
            foreach (var p in m_tmpOverflowParents)
            {
                // Is only in old list -> unregister callback.
                p.UnregisterCallback<GeometryChangedEvent>(onOverflowParentGeometryChanged);
            }
            m_tmpOverflowParents.Clear();
            
            // If and overflow parent was found then trigger a repaint to force the application of the rect.
            if (triggerMeshGeneration && m_overflowParents.Count > 0)
            {
                GenerateMesh();
            }
        }
        
        private void onOverflowParentGeometryChanged(GeometryChangedEvent evt)
        {
            updateOverflowInformation(triggerMeshGeneration: true);
        }
        
        /// <summary>
        /// Converts the content rect of the source element to the target element's local space.
        /// </summary>
        /// <param name="from">The VisualElement whose content rect will be converted.</param>
        /// <param name="to">The VisualElement to whose local content space the rect will be transformed to.</param>
        /// <returns></returns>
        private static Rect convertContentRectTo(VisualElement from, VisualElement to)
        {
            if (from == null || to == null)
                return Rect.zero;

            Rect sourceContentRect = from.contentRect;
            Vector2 topLeftWorld = from.LocalToWorld(sourceContentRect.min);
            Vector2 bottomRightWorld = from.LocalToWorld(sourceContentRect.max);
            
            Vector2 topLeftInChild = to.WorldToLocal(topLeftWorld);
            Vector2 bottomRightInChild = to.WorldToLocal(bottomRightWorld);

            Vector2 size = bottomRightInChild - topLeftInChild;
            return new Rect(topLeftInChild, size);
        }
        
        /// <summary>
        /// Call to check if the relevant styles have changed. It yes then the mesh with the custom shader will be updated.
        /// Usually this is called automatically every 50 MS but you can also call it manually if needed.
        /// </summary>
        public void CheckStyles()
        {
            // Update mesh if border styles have changed in the UI Builder.
            if(
                   !Mathf.Approximately( m_styleBorderTopWidth , resolvedStyle.borderTopWidth)
                || !Mathf.Approximately( m_styleBorderRightWidth , resolvedStyle.borderRightWidth)
                || !Mathf.Approximately( m_styleBorderBottomWidth , resolvedStyle.borderBottomWidth)
                || !Mathf.Approximately( m_styleBorderLeftWidth , resolvedStyle.borderLeftWidth)
                || !Mathf.Approximately( m_styleBorderTopLeftRadius , resolvedStyle.borderTopLeftRadius)
                || !Mathf.Approximately( m_styleBorderTopRightRadius , resolvedStyle.borderTopRightRadius)
                || !Mathf.Approximately( m_styleBorderBottomLeftRadius , resolvedStyle.borderBottomLeftRadius)
                || !Mathf.Approximately( m_styleBorderBottomRightRadius , resolvedStyle.borderBottomRightRadius)
                )
            {
                if (m_borderStylesInitialized)
                    GenerateMesh();
            }
            
            if (!m_borderStylesInitialized)
            {
                m_borderStylesInitialized = true;
            }
            
            m_styleBorderTopWidth = resolvedStyle.borderTopWidth;
            m_styleBorderRightWidth = resolvedStyle.borderRightWidth;
            m_styleBorderBottomWidth = resolvedStyle.borderBottomWidth;
            m_styleBorderLeftWidth = resolvedStyle.borderLeftWidth;
                
            m_styleBorderTopLeftRadius = resolvedStyle.borderTopLeftRadius;
            m_styleBorderTopRightRadius = resolvedStyle.borderTopRightRadius;
            m_styleBorderBottomLeftRadius = resolvedStyle.borderBottomLeftRadius;
            m_styleBorderBottomRightRadius = resolvedStyle.borderBottomRightRadius;
            
            
            // In the Editor we also check the material (only during edit time)
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && panel != null
                && SharedMaterial != null
                && Selection.objects.Contains(SharedMaterial)
                && !editorAreMaterialsEqual(m_cachedMaterial, SharedMaterial, s_editorExcludedPropertiesForMaterialCompare))
            {
                // n2h: For some reason the UI Builder is not updated immediately (only once it is redrawn).
                m_cachedMaterial = null;
                getMaterial();
                
                EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                foreach (var window in allWindows)
                {
                    var typeName = window.GetType().Name;
                    if (typeName.Contains("Builder") || typeName.Contains("Game"))
                        window.Repaint();
                }
            }
#endif
            
        }
        
#if UNITY_EDITOR
        private static List<string> s_editorExcludedPropertiesForMaterialCompare =
            new List<string>() { "_SrcBlend", "_DstBlend", "_Opacity", "_MainTex", "_UVMinMax" }; 
        
        bool editorAreMaterialsEqual(Material materialA, Material materialB, List<string> excludedProperties = null)
        {
            if (materialA == null || materialB == null)
            {
                return false;
            }

            if (materialA.shader != materialB.shader)
            {
                return false;
            }
            
            // Deep check
            int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount(materialA.shader);
            
            for (int i = 0; i < propertyCount; i++)
            {
                var propertyName = UnityEditor.ShaderUtil.GetPropertyName(materialA.shader, i);
                var type = UnityEditor.ShaderUtil.GetPropertyType(materialA.shader, i);

                if (excludedProperties != null && excludedProperties.Contains(propertyName))
                {
                    continue;
                }

                if (!materialA.HasProperty(propertyName) || !materialB.HasProperty(propertyName))
                    continue;
                
                switch (type)
                {
                    case UnityEditor.ShaderUtil.ShaderPropertyType.Color:
                        if (materialA.GetColor(propertyName) != materialB.GetColor(propertyName))
                        {
                            return false;
                        }

                        break;
                    case UnityEditor.ShaderUtil.ShaderPropertyType.Vector:
                        if (materialA.GetVector(propertyName) != materialB.GetVector(propertyName))
                        {
                            return false;
                        }

                        break;
                    case UnityEditor.ShaderUtil.ShaderPropertyType.Float:
                    case UnityEditor.ShaderUtil.ShaderPropertyType.Range:
                        if (!Mathf.Approximately(materialA.GetFloat(propertyName), materialB.GetFloat(propertyName)))
                        {
                            return false;
                        }

                        break;
                    case UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv:
                        if (materialA.GetTexture(propertyName) != materialB.GetTexture(propertyName))
                        {
                            return false;
                        }

                        break;
                    case UnityEditor.ShaderUtil.ShaderPropertyType.Int:
                        if (materialA.GetInt(propertyName) != materialB.GetInt(propertyName))
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
        }
#endif

        /// <summary>
        /// Returns whether or not the image is drawn based on the contentRect size.
        /// It acts like the activeInHierarchy we are used to from game objects (takes parents into account).
        /// </summary>
        /// <returns></returns>
        public bool IsDrawn()
        {
            var rect = contentRect;
            return !float.IsNaN(rect.width) && !Mathf.Approximately(rect.width, 0f) && !Mathf.Approximately(rect.height, 0f);
        }
        
        // Called every frame / draw cycle.
        protected override void ImmediateRepaint()
        {
            // Abort if not displayed.
            if (!m_lastIsDrawnValue.HasValue || !m_lastIsDrawnValue.Value)
                return;
            
            // Colors will be off in the UI Builder because sadly the UI Builder does not support linear color:
            // https://discussions.unity.com/t/ui-builder-doesnt-support-linear-color-space-and-ui-toolkit-too/870864/26

            var material = getMaterial();

            if (material == null)
                return;
            
            // Tiling support for sprites.
            if (material.HasVector("_UVMinMax"))
            {
                material.SetVector("_UVMinMax", new Vector4(m_minUV.x, m_minUV.y, m_maxUV.x, m_maxUV.y));
            }
            
            // Opacity in shader
            if (material.HasFloat("_Opacity"))
            {
                material.SetFloat("_Opacity", getEffectiveOpacity());
            }
            
            GL.PushMatrix();
            
            if (panel != null && panel.contextType != ContextType.Editor)
            {
                Vector2 worldPosition = this.LocalToWorld(Vector2.zero);

#if UNITY_EDITOR
                // Get the correct size in game view.
                float width = UnityEditor.Handles.GetMainGameViewSize().x;
                float height = UnityEditor.Handles.GetMainGameViewSize().y;
#else
                float width = Screen.width;
                float height = Screen.height;
#endif

                // Init Matrix for Game View and Builds      
                GL.LoadPixelMatrix(
                    -worldPosition.x, 
                    width / scaledPixelsPerPoint - worldPosition.x,
                    height / scaledPixelsPerPoint - worldPosition.y,
                    -worldPosition.y);
                
                Matrix4x4 model = getVisualElementTransformMatrix();
                GL.modelview = model;
            }

            int passCount = material.passCount;
            
            // We noticed that shader graph shaders usually only give valid results in their first pass but
            // they do have multiple, so rendering them all is no good idea. Thus we no limit all rendering
            // of shader graph shaders to the first pass.
            if (m_isShaderGraphShader)
                passCount = Mathf.Min(1, material.passCount);
            
            for (int p = 0; p < passCount; p++)
            {
                // If SetPass returns false, you should not render anything.
                // See: https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Material.SetPass.html
                if (!material.SetPass(p))
                    continue;

                GL.Begin(GL.TRIANGLES);
                GL.Color(Color.white);

                for (int i = 0; i < m_indices.Length; i+=3)
                {
                    var vertex0 = m_vertices[m_indices[i]];
                    var vertex1 = m_vertices[m_indices[i+1]];
                    var vertex2 = m_vertices[m_indices[i+2]];
                    
                    // Ignore nay null area triangles.
                    // n2h: do the ignore at mesh generation and not here while rendering.
                    if (isZeroAreaTriangle(vertex0.position, vertex1.position, vertex2.position))
                    {
                        continue;
                    }
                    
                    GL.TexCoord2(vertex0.uv.x, vertex0.uv.y);
                    GL.Vertex3(vertex0.position.x, vertex0.position.y, vertex0.position.z);
                    
                    GL.TexCoord2(vertex1.uv.x, vertex1.uv.y);
                    GL.Vertex3(vertex1.position.x, vertex1.position.y, vertex1.position.z);
                    
                    GL.TexCoord2(vertex2.uv.x, vertex2.uv.y);
                    GL.Vertex3(vertex2.position.x, vertex2.position.y, vertex2.position.z);
                }
                
                
                // Simple quad for testing
                
                //Vector2 min = Vector2.zero;
                //Vector2 max = new Vector2(contentRect.width + resolvedStyle.borderLeftWidth + resolvedStyle.borderRightWidth, contentRect.height + resolvedStyle.borderTopWidth + resolvedStyle.borderBottomWidth);
                //
                ////GL.TexCoord2(m_minUV.x, m_maxUV.y);
                //GL.Vertex3(min.x, min.y, 0);
                //
                ////GL.TexCoord2(m_maxUV.x, m_maxUV.y);
                //GL.Vertex3(max.x, min.y, 0);
                //
                ////GL.TexCoord2(m_maxUV.x, m_minUV.y);
                //GL.Vertex3(max.x, max.y, 0);
                //
                ////GL.TexCoord2(m_minUV.x, m_maxUV.y);
                //GL.Vertex3(min.x, min.y, 0);
                //
                ////GL.TexCoord2(m_maxUV.x, m_minUV.y);
                //GL.Vertex3(max.x, max.y, 0);
                //
                ////GL.TexCoord2(m_minUV.x, m_minUV.y);
                //GL.Vertex3(min.x, max.y, 0);

                GL.End();
            }
            
            GL.PopMatrix();
        }
        
        float getEffectiveOpacity()
        {
            float effectiveOpacity = 1.0f;
            
            VisualElement current = this;
            while (current != null)
            {
                effectiveOpacity *= current.resolvedStyle.opacity;
                current = current.parent;
            }

            return effectiveOpacity;
        }
        
        public static bool IsShaderGraphShader(Shader shader)
        {
            if (shader == null)
                return false;

            var tagId = new ShaderTagId("ShaderGraphShader");

            for (int i = 0; i < shader.subshaderCount; i++)
            {
                var tagValue = shader.FindSubshaderTagValue(i, tagId);
                if (tagValue != ShaderTagId.none && tagValue.name == "true")
                    return true;
            }

            return false;
        }
        
        Matrix4x4 getVisualElementTransformMatrix(VisualElement element = null)
        {
            // If no element is provided, use 'this' (assuming this is a VisualElement)
            if (element == null)
                element = this;

            // Get transform values
            var rs = element.resolvedStyle;
            var translate = new Vector2(rs.translate.x, rs.translate.y);
            var scale = new Vector2(rs.scale.value.x, rs.scale.value.y);
            var rotateZ = rs.rotate.angle.value;

            // Set up scale, rotation and translation
            Matrix4x4 rotate = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotateZ));
            Matrix4x4 scaling = Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1));
            Matrix4x4 translation = Matrix4x4.Translate(new Vector3(translate.x, translate.y, 0));

            // Combine current element's matrix
            Matrix4x4 localMatrix = translation * rotate * scaling;

            // Recursively get parent's matrix and combine
            if (element.parent != null)
            {
                Matrix4x4 parentMatrix = getVisualElementTransformMatrix(element.parent);
                return parentMatrix * localMatrix;
            }

            return localMatrix;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool isZeroAreaTriangle(Vector3 a, Vector3 b, Vector3 c, float epsilon = 0.00001f)
        {
            float area = 0.5f * Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
            return area < epsilon;
        }

        // Mesh Data
        Vertex[] m_vertices;
        ushort[] m_indices;
        Vector2 m_minUV;
        Vector2 m_maxUV;
        
        public void GenerateMesh()
        {
            Rect contentRectAbs = contentRect;

            if (contentRectAbs.width + resolvedStyle.paddingLeft + resolvedStyle.paddingRight < 0.01f || contentRectAbs.height + resolvedStyle.paddingTop + resolvedStyle.paddingBottom < 0.01f)
                return;

            // Clamp content
            if (resolvedStyle.borderLeftWidth < 0) contentRectAbs.xMin -= resolvedStyle.borderLeftWidth;
            if (resolvedStyle.borderRightWidth < 0) contentRectAbs.xMax += resolvedStyle.borderRightWidth;
            if (resolvedStyle.borderTopWidth < 0) contentRectAbs.yMin -= resolvedStyle.borderTopWidth;
            if (resolvedStyle.borderBottomWidth < 0) contentRectAbs.yMax += resolvedStyle.borderBottomWidth;


            // Mesh generation (we take rounded corners and borders into account).

            // clamp to positive
            float borderLeft = Mathf.Clamp(resolvedStyle.borderLeftWidth, 0, resolvedStyle.width * 0.5f);
            float borderRight = Mathf.Clamp(resolvedStyle.borderRightWidth, 0, resolvedStyle.width * 0.5f);
            float borderTop = Mathf.Clamp(resolvedStyle.borderTopWidth, 0, resolvedStyle.height * 0.5f);
            float borderBottom = Mathf.Clamp(resolvedStyle.borderBottomWidth, 0, resolvedStyle.height * 0.5f);

            float radiusTopLeft = Mathf.Max(0, resolvedStyle.borderTopLeftRadius);
            float radiusTopRight = Mathf.Max(0, resolvedStyle.borderTopRightRadius);
            float radiusBottomLeft = Mathf.Max(0, resolvedStyle.borderBottomLeftRadius);
            float radiusBottomRight = Mathf.Max(0, resolvedStyle.borderBottomRightRadius);

            float paddingLeft = Mathf.Max(0, resolvedStyle.paddingLeft);
            float paddingRight = Mathf.Max(0, resolvedStyle.paddingRight);
            float paddingTop = Mathf.Max(0, resolvedStyle.paddingTop);
            float paddingBottom = Mathf.Max(0, resolvedStyle.paddingBottom);

            contentRectAbs.xMin -= paddingLeft;
            contentRectAbs.xMax += paddingRight;
            contentRectAbs.yMin -= paddingTop;
            contentRectAbs.yMax += paddingBottom;

            // Calc inner rect
            // It only starts to curve on the inside once the radius is > the bigger border width
            Vector2 topLeftCornerSize = new Vector2(
                Mathf.Clamp(radiusTopLeft - borderLeft, 0, resolvedStyle.width * 0.5f - borderLeft),
                Mathf.Clamp(radiusTopLeft - borderTop, 0, resolvedStyle.height * 0.5f - borderTop)
            );

            Vector2 topRightCornerSize = new Vector2(
                Mathf.Clamp(radiusTopRight - borderRight, 0, resolvedStyle.width * 0.5f - borderRight),
                Mathf.Clamp(radiusTopRight - borderTop, 0, resolvedStyle.height * 0.5f - borderTop)
            );

            Vector2 bottomLeftCornerSize = new Vector2(
                Mathf.Clamp(radiusBottomLeft - borderLeft, 0, resolvedStyle.width * 0.5f - borderLeft),
                Mathf.Clamp(radiusBottomLeft - borderBottom, 0, resolvedStyle.height * 0.5f - borderBottom)
            );

            Vector2 bottomRightCornerSize = new Vector2(
                Mathf.Clamp(radiusBottomRight - borderRight, 0, resolvedStyle.width * 0.5f - borderRight),
                Mathf.Clamp(radiusBottomRight - borderBottom, 0, resolvedStyle.height * 0.5f - borderBottom)
            );


            // Calc inner quad with corner radius taken into account
            Vector2 innerTopLeft = new Vector2(contentRectAbs.xMin + topLeftCornerSize.x, contentRectAbs.yMin + topLeftCornerSize.y);
            Vector2 innerTopRight = new Vector2(contentRectAbs.xMax - topRightCornerSize.x, contentRectAbs.yMin + topRightCornerSize.y);
            Vector2 innerBottomLeft = new Vector2(contentRectAbs.xMin + bottomLeftCornerSize.x, contentRectAbs.yMax - bottomLeftCornerSize.y);
            Vector2 innerBottomRight = new Vector2(contentRectAbs.xMax - bottomRightCornerSize.x, contentRectAbs.yMax - bottomRightCornerSize.y);

            int verticesPerCorner = getResolvedMeshCornerSegments();

            // Calc total number of vertices
            // 4 Vertices for the inner rectangle
            // + verticesPerCorner + 2 for each full corner
            // + 1 for a corner with a radius on one side
            // + 0 vertices for a corner without any border radius
            int numVertices = 4; // <- start value

            // Calc total number of indices
            // 6 Vertices for the inner quad (2 tris)
            // + (verticesPerCorner + 1) * 3 for each full corner
            // + 0 for a corner with a radius on one side
            // + 0 vertices for a corner without any border radius
            // Sides
            // + see below
            int numIndices = 6; // <- start value

            // Top Left Corner
            if (topLeftCornerSize.x > 0 && topLeftCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (topLeftCornerSize.x > 0 || topLeftCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Top Right Corner
            if (topRightCornerSize.x > 0 && topRightCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (topRightCornerSize.x > 0 || topRightCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Bottom Left Corner
            if (bottomLeftCornerSize.x > 0 && bottomLeftCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (bottomLeftCornerSize.x > 0 || bottomLeftCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Bottom Right Corner
            if (bottomRightCornerSize.x > 0 && bottomRightCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (bottomRightCornerSize.x > 0 || bottomRightCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Sides (indices)
            // + 6 for a side where the corners form a rectangle
            // + 3 for a side where the corners form a triangle
            // + 0 for a side between two 0 vertex corners
            // Top
            if (topLeftCornerSize.y > 0 && topRightCornerSize.y > 0)
                numIndices += 6;
            else if (topLeftCornerSize.y > 0 || topRightCornerSize.y > 0)
                numIndices += 3;
            // Right
            if (topRightCornerSize.x > 0 && bottomRightCornerSize.x > 0)
                numIndices += 6;
            else if (topRightCornerSize.x > 0 || bottomRightCornerSize.x > 0)
                numIndices += 3;
            // Bottom
            if (bottomRightCornerSize.y > 0 && bottomLeftCornerSize.y > 0)
                numIndices += 6;
            else if (bottomRightCornerSize.y > 0 || bottomLeftCornerSize.y > 0)
                numIndices += 3;
            // Left
            if (bottomLeftCornerSize.x > 0 && topLeftCornerSize.x > 0)
                numIndices += 6;
            else if (bottomLeftCornerSize.x > 0 || topLeftCornerSize.x > 0)
                numIndices += 3;

            if (m_vertices == null || m_vertices.Length != numVertices)
            {
                m_vertices = new Vertex[numVertices];
                m_indices = new ushort[numIndices];
            }

            // keep track of indices
            ushort v = 0;
            ushort i = 0;

            // Center rect
            ushort innerBottomLeftVertex = v;
            m_vertices[v++].position = new Vector3(innerBottomLeft.x, innerBottomLeft.y, Vertex.nearZ);
            ushort innerTopLeftVertex = v;
            m_vertices[v++].position = new Vector3(innerTopLeft.x, innerTopLeft.y, Vertex.nearZ);
            ushort innerTopRightVertex = v;
            m_vertices[v++].position = new Vector3(innerTopRight.x, innerTopRight.y, Vertex.nearZ);
            ushort innerBottomRightVertex = v;
            m_vertices[v++].position = new Vector3(innerBottomRight.x, innerBottomRight.y, Vertex.nearZ);
            m_indices[i++] = 0;
            m_indices[i++] = 1;
            m_indices[i++] = 2;
            m_indices[i++] = 2;
            m_indices[i++] = 3;
            m_indices[i++] = 0;

            ushort bottomLeftLeftVertex, bottomLeftBottomVertex, bottomRightRightVertex, bottomRightBottomVertex,
                   topLeftLeftVertex, topLeftTopVertex, topRightTopVertex, topRightRightVertex;

            // We add an overlap to make the new mesh overlap the borders a little to reduce gaps.
            float overlapWidth = getResolvedMeshCornerOverlap();

            // Sides (indices)
            // + 2 tris for a side where the corners form a rectangle
            // + 1 tri for a side where the corners form a triangle
            // Top
            createSide(topLeftCornerSize, topRightCornerSize, cornerSizeNotZeroY, ref v, ref i, innerTopLeftVertex, innerTopRightVertex,
                new Vector3(innerTopLeft.x, innerTopLeft.y - topLeftCornerSize.y - overlapWidth, Vertex.nearZ),
                new Vector3(innerTopRight.x, innerTopRight.y - topRightCornerSize.y - overlapWidth, Vertex.nearZ),
                out topLeftTopVertex, out topRightTopVertex
                );
            // Right
            createSide(topRightCornerSize, bottomRightCornerSize, cornerSizeNotZeroX, ref v, ref i, innerTopRightVertex, innerBottomRightVertex,
                new Vector3(innerTopRight.x + topRightCornerSize.x + overlapWidth, innerTopRight.y, Vertex.nearZ),
                new Vector3(innerBottomRight.x + bottomRightCornerSize.x + overlapWidth, innerBottomRight.y, Vertex.nearZ),
                out topRightRightVertex, out bottomRightRightVertex
                );
            // Bottom
            createSide(bottomRightCornerSize, bottomLeftCornerSize, cornerSizeNotZeroY, ref v, ref i, innerBottomRightVertex, innerBottomLeftVertex,
                new Vector3(innerBottomRight.x, innerBottomRight.y + bottomRightCornerSize.y + overlapWidth, Vertex.nearZ),
                new Vector3(innerBottomLeft.x, innerBottomLeft.y + bottomLeftCornerSize.y + overlapWidth, Vertex.nearZ),
                out bottomRightBottomVertex, out bottomLeftBottomVertex
                );
            // Left
            createSide(bottomLeftCornerSize, topLeftCornerSize, cornerSizeNotZeroX, ref v, ref i, innerBottomLeftVertex, innerTopLeftVertex,
                new Vector3(innerBottomLeft.x - bottomLeftCornerSize.x - overlapWidth, innerBottomLeft.y, Vertex.nearZ),
                new Vector3(innerTopLeft.x - topLeftCornerSize.x - overlapWidth, innerTopLeft.y, Vertex.nearZ),
                out bottomLeftLeftVertex, out topLeftLeftVertex
                );

            if (verticesPerCorner > 0)
            {
                createCorner(topLeftCornerSize, innerTopLeft, verticesPerCorner, ref v, ref i, innerTopLeftVertex, topLeftLeftVertex, topLeftTopVertex, 2);
                createCorner(topRightCornerSize, innerTopRight, verticesPerCorner, ref v, ref i, innerTopRightVertex, topRightTopVertex, topRightRightVertex, 3);
                createCorner(bottomRightCornerSize, innerBottomRight, verticesPerCorner, ref v, ref i, innerBottomRightVertex, bottomRightRightVertex, bottomRightBottomVertex, 0);
                createCorner(bottomLeftCornerSize, innerBottomLeft, verticesPerCorner, ref v, ref i, innerBottomLeftVertex, bottomLeftBottomVertex, bottomLeftLeftVertex, 1);
            }
            
            // Limit vertices to overflow:hidden rect of the element itself or the first parent.
            // Known issues: Rounded corners change angle a bit due to the squishing. The effect is more visible
            //               If the number of segments is low. It is an acceptable compromise I think.
            //               It only takes the first parent with overflow hidden into account (not all). It will ignore
            //               multiple stacked overflow:hidden parents (solution would be to calculate the min bounding
            //               box of all parents with overflow:hidden.
            // All of this would not be necessary if Unity had proper stencil support in UI Toolkit shading but sadly
            // that is not an option (maybe in Unity 6.3+), see: https://discussions.unity.com/t/ui-toolkit-default-shader-source/1647930/4
            if (m_overflowContentRect.HasValue)
            {
                // Limit the drawn vertices to overflow bounds.
                // We can just quash them because the shape is guaranteed to be convex
                // though some tris may have zero area after squashing. We take care
                // of that during GL drawing (we ignore empty tris).
                for (int vi = 0; vi < m_vertices.Length; vi++)
                {
                    var pos = m_vertices[vi].position;
                    pos.x = Mathf.Clamp(pos.x, m_overflowContentRect.Value.xMin, m_overflowContentRect.Value.xMax);
                    pos.y = Mathf.Clamp(pos.y, m_overflowContentRect.Value.yMin, m_overflowContentRect.Value.yMax);
                    m_vertices[vi].position = pos;
                }
            }

            // UVs
            {
                Vector2 minPos = new Vector2(0, 0);
                Vector2 maxPos = new Vector2(contentRect.width + resolvedStyle.borderLeftWidth + resolvedStyle.borderRightWidth,
                                             contentRect.height + resolvedStyle.borderTopWidth + resolvedStyle.borderBottomWidth);
                m_minUV = Vector2.zero;
                m_maxUV = Vector2.one;
                
                // UV Sprite support
                if (Sprite != null)
                {
                    m_minUV.x = Sprite.rect.xMin / Sprite.texture.width;
                    m_minUV.y = Sprite.rect.yMin / Sprite.texture.height;
                    m_maxUV.x = Sprite.rect.xMax / Sprite.texture.width;
                    m_maxUV.y = Sprite.rect.yMax / Sprite.texture.height;
                }

                float posDeltaX = maxPos.x - minPos.x;
                float posDeltaY = maxPos.y - minPos.y;
                float uvDeltaX = m_maxUV.x - m_minUV.x;
                float uvDeltaY = m_maxUV.y - m_minUV.y;
                for (int vi = 0; vi < m_vertices.Length; vi++)
                {
                    var pos = m_vertices[vi].position;
                    m_vertices[vi].uv = new Vector2(
                        m_minUV.x + (pos.x - minPos.x) / posDeltaX * uvDeltaX,
                        m_minUV.y + (maxPos.y - pos.y) / posDeltaY * uvDeltaY // flip y
                    );
                }
            }
        }

        private void createCorner(Vector2 cornerSize, Vector2 innerPos, int verticesPerCorner, ref ushort v, ref ushort i, ushort innerVertex, ushort startVertex, ushort endVertex, int quadrantOffset)
        {
            if (cornerSize.x > 0 && cornerSize.y > 0)
            {
                ushort center = innerVertex;
                ushort last = startVertex;

                float offset = Mathf.PI * 0.5f * quadrantOffset;
                float stepSizeInQuadrant = 1f / (verticesPerCorner + 1) * Mathf.PI * 0.5f;

                for (int c = 1; c < verticesPerCorner + 1; c++)
                {
                    float x = Mathf.Cos(offset + stepSizeInQuadrant * c);
                    float y = Mathf.Sin(offset + stepSizeInQuadrant * c);
                    // We also add an overlap to make the new mesh overlap the borders a little to reduce gaps.
                    float overlapWidth = getResolvedMeshCornerOverlap();
                    m_vertices[v++].position = new Vector3(innerPos.x + x * (cornerSize.x + overlapWidth), innerPos.y + y * (cornerSize.y + overlapWidth), Vertex.nearZ);

                    m_indices[i++] = center;
                    m_indices[i++] = last;
                    m_indices[i++] = (ushort)(v - 1);
                    last = m_indices[i - 1];
                }

                // End at the existing vertex
                m_indices[i++] = center;
                m_indices[i++] = last;
                m_indices[i++] = endVertex;
            }
        }

        void createSide(
            Vector2 firstCornerSize, Vector2 secondCornerSize,
            System.Func<Vector2, bool> cornerSizeNotZeroFunc,
            ref ushort v, ref ushort i,
            ushort firstOuterVertex, ushort secondOuterVertex,
            Vector3 newVertexAPos, Vector3 newVertexBPos,
            out ushort newVertexA, out ushort newVertexB)
        {
            newVertexA = 0;
            newVertexB = 0;

            if (cornerSizeNotZeroFunc(firstCornerSize) && cornerSizeNotZeroFunc(secondCornerSize))
            {
                newVertexA = v;
                m_vertices[v++].position = newVertexAPos;
                newVertexB = v;
                m_vertices[v++].position = newVertexBPos;
                m_indices[i++] = newVertexA;
                m_indices[i++] = newVertexB;
                m_indices[i++] = firstOuterVertex;
                m_indices[i++] = newVertexB;
                m_indices[i++] = secondOuterVertex;
                m_indices[i++] = firstOuterVertex;
            }
            else if (cornerSizeNotZeroFunc(firstCornerSize) || cornerSizeNotZeroFunc(secondCornerSize))
            {
                if (cornerSizeNotZeroFunc(firstCornerSize))
                {
                    newVertexA = v;
                    m_vertices[v++].position = newVertexAPos;
                    m_indices[i++] = newVertexA;
                    m_indices[i++] = secondOuterVertex;
                    m_indices[i++] = firstOuterVertex;
                }
                else
                {
                    newVertexB = v;
                    m_vertices[v++].position = newVertexBPos;
                    m_indices[i++] = newVertexB;
                    m_indices[i++] = secondOuterVertex;
                    m_indices[i++] = firstOuterVertex;
                }
            }
        }

        bool cornerSizeNotZeroX(Vector2 cornerSize)
        {
            return cornerSize.x > 0;
        }

        bool cornerSizeNotZeroY(Vector2 cornerSize)
        {
            return cornerSize.y > 0;
        }
    }
}