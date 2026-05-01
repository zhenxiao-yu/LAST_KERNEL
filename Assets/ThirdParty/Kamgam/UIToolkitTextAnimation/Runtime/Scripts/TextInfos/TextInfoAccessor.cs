using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using TextElement = UnityEngine.UIElements.TextElement;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
#if !UNITY_2023_1_OR_NEWER
using System.Collections;
using Unity.Collections;
using UnityEngine.UIElements.UIR.Implementation;
#endif

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// A wrapper for TextElement that allows access vertex information by using lots of reflections and internal APIs.
    /// See: https://discussions.unity.com/t/how-to-access-and-modify-the-mesh-data-of-visualelement/1599415/7
    /// 
    /// It uses the UnityEngine.UI Assembly to gain access to the internals of TextCore, see:
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/AssemblyInfo.cs
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreFontEngine/Managed/AssemblyInfo.cs
    /// and: https://discussions.unity.com/t/how-to-access-useful-unity-editor-engine-internal-methods/251479/3
    /// This works because the text core is visible to UnityEngine.UI and thus is we reference UnityEngine.UI we also have
    /// access to the core.
    /// 
    /// In Unity 2022.3 and below it uses the painter (UIRStylePainter) and assumes it is executed AFTER the generateVisualContent callback.
    /// In Unity 2023.0 and higher it uses the vertexData and assumes it is executed BEFORE the generateVisualContent callback.
    ///
    /// It makes use of the following internal types:
    /// UITKTextHandle https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextHandle.cs
    /// TextInfo https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextInfo.cs
    /// TextElementInfo https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextElementInfo.cs
    /// LinkInfo https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/LinkInfo.cs
    /// MeshInfo https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/MeshInfo.cs
    /// TextCoreVertex https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextCoreVertex.bindings.cs
    /// UIRStylePainter https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/ModuleOverrides/com.unity.ui/Core/Renderer/UIRStylePainter.cs
    ///
    /// The AssemblyReference workflow was introduced late in development. Thus there still is some reflection code (also some parts are using private fields).
    /// TODO: Clean up all unnecessary reflection code.
    /// </summary>
    public class TextInfoAccessor
    {
        // This class exists only to pass around the vertex data of one quad in a memory allocation free manner.
        // Why? "ref" turned out to cause memory allocations on old Unity versions.
        public class QuadVertexData
        {
            static Stack<QuadVertexData> _pool = new Stack<QuadVertexData>();

            public static void ReturnToPool(QuadVertexData data)
            {
                _pool.Push(data);
            }

            public static void ReturnToPool(IList<QuadVertexData> list)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ReturnToPool(list[i]);
                }

                list.Clear();
            }

            public static QuadVertexData GetFromPool()
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }

                return new QuadVertexData();
            }

            public Vector3 BottomLeftPosition;
            public Vector3 TopLeftPosition;
            public Vector3 TopRightPosition;
            public Vector3 BottomRightPosition;
            
            public Color32 BottomLeftColor;
            public Color32 TopLeftColor;
            public Color32 TopRightColor;
            public Color32 BottomRightColor;
            
            public void SetAlpha(byte alpha)
            {
                BottomLeftColor.a = alpha;
                TopLeftColor.a = alpha;
                TopRightColor.a = alpha;
                BottomRightColor.a = alpha;
            }

            public byte BottomLeftAlpha
            {
                set => BottomLeftColor.a = value;
                get => BottomLeftColor.a;
            }

            public byte TopLeftAlpha
            {
                set => TopLeftColor.a = value;
                get => TopLeftColor.a;
            }
            
            public byte TopRightAlpha
            {
                set => TopRightColor.a = value;
                get => TopRightColor.a;
            }
            
            public byte BottomRightAlpha
            {
                set => BottomRightColor.a = value;
                get => BottomRightColor.a;
            }


            public static QuadVertexData CreateFromVertices(
                Vertex vertexBottomLeft,
                Vertex vertexTopLeft,
                Vertex vertexTopRight,
                Vertex vertexBottomRight
                )
            {
                var data = GetFromPool();
                
                data.BottomLeftPosition = vertexBottomLeft.position;
                data.TopLeftPosition = vertexTopLeft.position;
                data.TopRightPosition = vertexTopRight.position;
                data.BottomRightPosition = vertexBottomRight.position;
                
                data.BottomLeftColor = vertexBottomLeft.tint;
                data.TopLeftColor = vertexTopLeft.tint;
                data.TopRightColor = vertexTopRight.tint;
                data.BottomRightColor = vertexBottomRight.tint;
                
                return data;
            }
        }
        
        // A partial copy of https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextElementInfo.cs
        // If Unity makes the API public (which they should) then this will no longer be needed.
        public class CharacterInfo
        {
            static Stack<CharacterInfo> _pool = new Stack<CharacterInfo>();

            public static void ReturnToPool(CharacterInfo info)
            {
                info.MeshIndex = -1;
                info.VertexIndex = -1;
                info.IsVisible = false;
                
                _pool.Push(info);
            }

            public static void ReturnToPool(IList<CharacterInfo> tagInfos)
            {
                for (int i = tagInfos.Count - 1; i >= 0; i--)
                {
                    ReturnToPool(tagInfos[i]);
                }

                tagInfos.Clear();
            }

            public static CharacterInfo GetFromPool()
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }

                return new CharacterInfo();
            }

            public int MeshIndex;
            public int VertexIndex;
            public bool IsVisible;
        }
        
        // A copy of https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextElementInfo.cs
        // If Unity makes the API public (which they should) then this will no longer be needed.
        public class AnimationTagInfo
        {
            static Stack<AnimationTagInfo> _pool = new Stack<AnimationTagInfo>();

            public static void ReturnToPool(AnimationTagInfo tagInfo)
            {
                tagInfo.Ids = null;
                tagInfo.LinkTextFirstCharacterIndex = 0;
                tagInfo.LinkTextLength = 0;
                tagInfo.VertexIndices.Clear();
                tagInfo.MaterialReferenceIndices.Clear();

                _pool.Push(tagInfo);
            }

            public static void ReturnToPool(IList<AnimationTagInfo> tagInfos)
            {
                for (int i = tagInfos.Count - 1; i >= 0; i--)
                {
                    ReturnToPool(tagInfos[i]);
                }

                tagInfos.Clear();
            }

            public static AnimationTagInfo GetFromPool()
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }

                return new AnimationTagInfo();
            }

            public string[] Ids;
            public int LinkTextFirstCharacterIndex;
            public int LinkTextLength;

            /// <summary>
            /// First vertex indices of each character. Each character has 4 vertices which means the indices here look like this: 8,12,16, ...
            /// </summary>
            public List<int> VertexIndices;

            /// <summary>
            /// Material indices matching the VertexIndices. NOTICE: Even within one link tag there can be multiple materials.
            /// </summary>
            public List<int> MaterialReferenceIndices;

            private AnimationTagInfo()
            {
            }
        }


        public class VertexData
        {
            static Stack<VertexData> s_pool = new Stack<VertexData>();

            public static int PoolSize => s_pool.Count;

            public static void ReturnToPool(VertexData vertex)
            {
                s_pool.Push(vertex);
            }

            public static void ReturnToPool(IList<VertexData> vertices)
            {
                for (int i = vertices.Count - 1; i >= 0; i--)
                {
                    ReturnToPool(vertices[i]);
                }

                vertices.Clear();
            }

            public static void ReturnToPool(IDictionary<int, VertexData> vertices)
            {
                foreach (var kv in vertices)
                {
                    ReturnToPool(kv.Value);
                }

                vertices.Clear();
            }

            public static VertexData GetFromPool(Vector3 position, Color32 color)
            {
                var v = GetFromPool();
                v.Position = position;
                v.Color = color;
                return v;
            }

            public static VertexData GetFromPool()
            {
                if (s_pool.Count > 0)
                {
                    return s_pool.Pop();
                }
                return new VertexData();
            }

            public Vector3 Position;
            public Color32 Color;

            private VertexData()
            {
            }
        }


        private static int s_instanceIdCounter = 0;

        public static bool CacheIsBuilt;

        public static Type UITKTextHandleType;
        public static Type TextInfoType;
        public static Type TextElementInfoType;
        public static Type LinkInfoType;
        public static Type MeshInfoType;
        public static int MeshInfoTypeSize;
        public static Type TextCoreVertexType;
        public static int TextCoreVertexTypeSize;
        public static Type UIRStylePainterType;
        public static Type UIRStylePainterEntryType;
        public static Type TextureIdType;

        public static MemberInfo s_uitkTextHandleMemberInfo;
        public static MemberInfo s_textInfoMemberInfo;
        public static MemberInfo s_textElementInfoFieldMemberInfo;
        public static MemberInfo s_textInfoCharacterCountMemberInfo;
        public static MemberInfo s_linkInfoMemberInfo;
        public static MemberInfo s_linkCountMemberInfo;
        public static MemberInfo s_linkTextFirstCharacterIndexMemberInfo;
        public static MemberInfo s_linkTextLengthMemberInfo;
        public static MemberInfo s_vertexIndexMemberInfo;
        public static MemberInfo s_materialReferenceIndexMemberInfo;
        public static MemberInfo s_isVisibleMemberInfo;
        public static MemberInfo s_indexMemberInfo;
        public static MemberInfo s_textGenerationSettingsMemberInfo;
        public static MemberInfo s_isDirtyMemberInfo;
        public static MemberInfo s_lastHashMemberInfo;
        public static MethodInfo s_updateMeshMethodInfo;
        public static MethodInfo s_updateMethodInfo;
        public static MemberInfo s_meshInfoMemberInfo;
        public static MemberInfo s_vertexDataMemberInfo;
        public static MemberInfo s_textCoreVertexColorMemberInfo;
        public static MemberInfo s_textCoreVertexPositionMemberInfo;

        public static MemberInfo s_textCoreVertexUV2MemberInfo;

        // Unity 2022
        public static MemberInfo s_painterMemberInfo;
        public static MemberInfo s_entriesMemberInfo;
        public static MemberInfo s_entryVerticesMemberInfo;
        public static MemberInfo s_TextureIdMemberInfo;
        public static MethodInfo s_TextureIdIsValidMethodInfo;

        public TextElement Element;
        public bool IsValid;

        internal Array m_textElementInfoArray;
        protected int m_characterCount;
        protected List<CharacterInfo> m_characterInfos = new List<CharacterInfo>();
        public int CharInfoCount => m_characterInfos.Count;
        protected Array m_linkInfoArray;
        protected int m_linkInfoCount;
        public int LinkInfoCount => m_linkInfoCount;
        protected List<string> m_animationIdTexts = new List<string>();
        protected List<DelayTagInfo> m_delayTagInfos = new List<DelayTagInfo>();
        public List<DelayTagInfo> DelayTagInfos => m_delayTagInfos;
         
        public int AnimationIdCount => m_animationIdTexts.Count;
        
#if UNITY_2023_1_OR_NEWER
        protected Dictionary<int, VertexData> m_originalVertices = new Dictionary<int, VertexData>(20);
#endif

        // For debugging
        private int m_instanceId;
        public int InstanceId => m_instanceId;

        public TextInfoAccessor(TextElement element)
        {
            m_instanceId = s_instanceIdCounter;
            s_instanceIdCounter++;

            Reuse(element);
        }

        public void Reuse(TextElement element)
        {
            CacheTypeInfoIfNeeded();
            IsValid = CacheIsValid();

            Reset();
            
            Element = element;
        }

        public void Reset()
        {
            Element = null;
#if UNITY_2023_1_OR_NEWER    
            ClearOriginalVertexCache();
#endif
            CharacterInfo.ReturnToPool(m_characterInfos);
        }

        public static void CacheTypeInfoIfNeeded()
        {
            if (!CacheIsBuilt)
            {
                CacheIsBuilt = true;

                // Types

                UITKTextHandleType =
                    Type.GetType("UnityEngine.UIElements.UITKTextHandle, UnityEngine.UIElementsModule");
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextHandle.cs

                TextInfoType = Type.GetType("UnityEngine.TextCore.Text.TextInfo, UnityEngine.TextCoreTextEngineModule");
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextInfo.cs

                TextElementInfoType =
                    Type.GetType("UnityEngine.TextCore.Text.TextElementInfo, UnityEngine.TextCoreTextEngineModule");
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextElementInfo.cs

                LinkInfoType = Type.GetType("UnityEngine.TextCore.Text.LinkInfo, UnityEngine.TextCoreTextEngineModule");
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/LinkInfo.cs

                MeshInfoType = Type.GetType("UnityEngine.TextCore.Text.MeshInfo, UnityEngine.TextCoreTextEngineModule");
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/MeshInfo.cs

                TextCoreVertexType = Type.GetType("UnityEngine.TextCore.Text.TextCoreVertex, UnityEngine.TextCoreTextEngineModule");
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/TextCoreVertex.bindings.cs

                // Unity 2022
                UIRStylePainterType =
                    Type.GetType(
                        "UnityEngine.UIElements.UIR.Implementation.UIRStylePainter, UnityEngine.UIElementsModule");
                if (UIRStylePainterType != null)
                    UIRStylePainterEntryType =
                        UIRStylePainterType.GetNestedType("Entry", ReflectionExtensions.BindingFlags);
                // See: https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/ModuleOverrides/com.unity.ui/Core/Renderer/UIRStylePainter.cs

                TextureIdType = Type.GetType("UnityEngine.UIElements.TextureId, UnityEngine.UIElementsModule");

                // Members

                s_uitkTextHandleMemberInfo = typeof(TextElement).GetMemberInfo("uitkTextHandle");

                if (UITKTextHandleType != null)
                {
                    s_textInfoMemberInfo = UITKTextHandleType.GetMemberInfo("textInfo");
                    s_textGenerationSettingsMemberInfo = UITKTextHandleType.GetMemberInfo("settings");
                    s_lastHashMemberInfo = UITKTextHandleType.GetMemberInfo("m_PreviousGenerationSettingsHash");
                    // Only available in Unity 6
                    s_isDirtyMemberInfo = UITKTextHandleType.GetMemberInfo("isDirty");
                    // Only available in Unity 6
                    s_updateMeshMethodInfo = UITKTextHandleType.GetMethod("UpdateMesh", ReflectionExtensions.BindingFlags);
                    // Only available in Unity 2023
                    s_updateMethodInfo = UITKTextHandleType.GetMethod("Update");
                }

                if (TextInfoType != null)
                {
                    s_textElementInfoFieldMemberInfo = TextInfoType.GetMemberInfo("textElementInfo");
                    s_textInfoCharacterCountMemberInfo = TextInfoType.GetMemberInfo("characterCount");
                    s_vertexIndexMemberInfo = TextElementInfoType.GetMemberInfo("vertexIndex");
                    s_materialReferenceIndexMemberInfo = TextElementInfoType.GetMemberInfo("materialReferenceIndex");
                    s_isVisibleMemberInfo = TextElementInfoType.GetMemberInfo("isVisible");
                    s_indexMemberInfo = TextElementInfoType.GetMemberInfo("index");
                    s_meshInfoMemberInfo = TextInfoType.GetMemberInfo("meshInfo");
                }

                if (LinkInfoType != null)
                {
                    s_linkInfoMemberInfo = TextInfoType.GetMemberInfo("linkInfo");
                    s_linkCountMemberInfo = TextInfoType.GetMemberInfo("linkCount");

                    s_linkTextFirstCharacterIndexMemberInfo =
                        LinkInfoType.GetMemberInfo(
                            "linkTextfirstCharacterIndex"); // <- typo (F/f)irst in the original Unity source.
                    // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/LinkInfo.cs#L20
                    if (s_linkTextFirstCharacterIndexMemberInfo == null)
                        s_linkTextFirstCharacterIndexMemberInfo =
                            LinkInfoType.GetMemberInfo(
                                "linkTextFirstCharacterIndex"); // <- in case they fix it some day.

                    s_linkTextLengthMemberInfo = LinkInfoType.GetMemberInfo("linkTextLength");
                }

                if (MeshInfoType != null)
                {
                    // Sadly this type can not be marshalled and throws an exception in il2cpp builds.
                    // ArgumentException: Type 'UnityEngine.TextCore.Text.MeshInfo' cannot be marshaled as an unmanaged structure; no meaningful size or offset can be computed.
                    // MeshInfoTypeSize = Marshal.SizeOf(MeshInfoType);
                    // only available in Unity 2023.x
                    s_vertexDataMemberInfo = MeshInfoType.GetMemberInfo("vertexData");
                }

#if UNITY_2023_1_OR_NEWER
                if (TextCoreVertexType != null)
                {
                    // Sadly this type can not be marshalled and throws an exception.
                    // TextCoreVertexTypeSize = Marshal.SizeOf(TextCoreVertexType);
                    s_textCoreVertexColorMemberInfo = TextCoreVertexType.GetMemberInfo("color");
                    s_textCoreVertexPositionMemberInfo = TextCoreVertexType.GetMemberInfo("position");
                    s_textCoreVertexUV2MemberInfo = TextCoreVertexType.GetMemberInfo("uv2");
                }
#else
                s_painterMemberInfo = typeof(MeshGenerationContext).GetMemberInfo("painter");
                if (UIRStylePainterType != null)
                {
                    s_entriesMemberInfo = UIRStylePainterType.GetMemberInfo("m_Entries");
                }

                if (UIRStylePainterEntryType != null)
                {
                    s_entryVerticesMemberInfo = UIRStylePainterEntryType.GetMemberInfo("vertices");
                    s_TextureIdMemberInfo = UIRStylePainterEntryType.GetMemberInfo("texture");
                }

                if (TextureIdType != null)
                {
                    // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/Renderer/UIRTextureRegistry.cs#L10
                    s_TextureIdIsValidMethodInfo = TextureIdType.GetMethod("IsValid", ReflectionExtensions.BindingFlags);
                }
#endif
            }
        }

        public static bool CacheIsValid()
        {
            bool result = s_uitkTextHandleMemberInfo != null
                   && s_textInfoMemberInfo != null
                   && s_textElementInfoFieldMemberInfo != null
                   && s_textInfoCharacterCountMemberInfo != null
                   && s_linkInfoMemberInfo != null
                   && s_linkCountMemberInfo != null
                   && s_linkTextFirstCharacterIndexMemberInfo != null
                   && s_linkTextLengthMemberInfo != null
                   && s_meshInfoMemberInfo != null
                   && s_materialReferenceIndexMemberInfo != null
                   && s_isVisibleMemberInfo != null
#if UNITY_2023_1_OR_NEWER
                   && s_textGenerationSettingsMemberInfo != null
                   && s_lastHashMemberInfo != null
#if UNITY_6000 || UNITY_6000_OR_NEWER
                   && s_isDirtyMemberInfo != null
                   && s_updateMeshMethodInfo != null
#else
                   && s_updateMethodInfo != null
#endif
                   && s_vertexDataMemberInfo != null
                   && s_textCoreVertexColorMemberInfo != null
                   && s_textCoreVertexPositionMemberInfo != null
                   && s_textCoreVertexUV2MemberInfo != null
#else
                   && s_painterMemberInfo != null
                   && s_entriesMemberInfo != null
                   && s_entryVerticesMemberInfo != null
                   && s_TextureIdMemberInfo != null
                   && s_TextureIdIsValidMethodInfo != null
#endif
                ;

            return result;

        }

        internal UITKTextHandle GetUITKTextHandle()
        {
            // Would be nice to cache these too but sadly the textElementInfo is regenerated internally
            // so we have to reacquire the reference every time.
            var uitkTextHandle = s_uitkTextHandleMemberInfo.GetValue(Element) as UITKTextHandle;
            return uitkTextHandle;
        }

        public bool HasLinkInfos()
        {
            return m_linkInfoCount > 0;
        }

        public bool HasAnimationLinks()
        {
            for (int i = 0; i < m_animationIdTexts.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_animationIdTexts[i]))
                    return true;
            }

            return false;
        }
        
        public bool HasTagsUsedForAnimation()
        {
            for (int i = 0; i < m_animationIdTexts.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_animationIdTexts[i]))
                    return true;
            }
            
            if (m_delayTagInfos.Count > 0)
            {
                return true;
            }

            return false;
        }

        public void UpdateLinkInfoCount()
        {
            // Would be nice to cache these too but sadly the textElementInfo is regenerated internally
            // so we have to reacquire the reference every time.
            var uitkTextHandle = GetUITKTextHandle();

            var textInfo = s_textInfoMemberInfo.GetValue(uitkTextHandle);
            if (textInfo == null)
            {
                m_linkInfoCount = 0;
                return;
            }

            m_linkInfoCount = (int)s_linkCountMemberInfo.GetValue(textInfo);
        }

        /// <summary>
        /// Converts raw source text indices to indices of the final text (char and quad).<br />
        /// Returns the character index of the TextElementInfo in the textElementInfo and the quad index of the rendered quad.
        /// If the character of the given index is visible (can be rendered as a quad) then it will be that index.
        /// If the character of the given index is invisible (part of a tag or an empty space) then the index of the next 
        /// visible char and quad are used. This means that if the index is at the very end the returned index can be bigger 
        /// than the testElementInfo list.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="characterIndex">Character index in the final rendered text (text without tags).</param>
        /// <param name="quadIndex">Index of the first quad after the index.</param>
        public void SourceTextIndexToRenderedIndex(int index, out int characterIndex, out int quadIndex)
        {
            // Would be nice to cache these too but sadly the textElementInfo is regenerated internally
            // so we have to reacquire the reference every time.
            var uitkTextHandle = GetUITKTextHandle();

            var textInfo = s_textInfoMemberInfo.GetValue(uitkTextHandle);
            if (textInfo == null)
            {
                characterIndex = -1;
                quadIndex = -1;
            }

            m_textElementInfoArray = s_textElementInfoFieldMemberInfo.GetValue(textInfo) as Array;
            if (m_textElementInfoArray == null)
            {
                characterIndex = -1;
                quadIndex = -1;
            }

            int qIndex = -1;
            int cIndex = -1;
            
            for (int i = 0; i < m_textElementInfoArray.Length; i++)
            {
                var element = m_textElementInfoArray.GetValue(i);
                if ((int)s_indexMemberInfo.GetValue(element) <= index)
                {
                    cIndex = i;

                    // Quads are only rendered for visible characters
                    if ((bool)s_isVisibleMemberInfo.GetValue(element))
                    {
                        qIndex++;
                    }
                }
            }

            // The index is invisible at the very end.
            characterIndex = cIndex + 1;
            quadIndex = qIndex + 1;
        }

        public void GetCharacterAndTagInfos(List<AnimationTagInfo> animationTagInfos = null)
        {
            m_linkInfoCount = 0;
            m_quadCount = 0;

            // Create animation results list if necessary
            if (animationTagInfos == null)
                animationTagInfos = new List<AnimationTagInfo>();
            // Return elements in existing list to pool and clear list if necessary
            else
                AnimationTagInfo.ReturnToPool(animationTagInfos);
            
            // Return elements in existing list to pool and clear list if necessary
            CharacterInfo.ReturnToPool(m_characterInfos);
            
            // Would be nice to cache these too but sadly the textElementInfo is regenerated internally
            // so we have to reacquire the reference every time.
            var uitkTextHandle = GetUITKTextHandle();

            var textInfo = s_textInfoMemberInfo.GetValue(uitkTextHandle);
            if (textInfo == null)
                return;

            m_textElementInfoArray = s_textElementInfoFieldMemberInfo.GetValue(textInfo) as Array;
            if (m_textElementInfoArray == null)
                return;

            m_characterCount = (int) s_textInfoCharacterCountMemberInfo.GetValue(textInfo);
            
            // CHARACTERS
            // string debugText = "";
            for (int i = 0; i < m_characterCount; i++)
            {
                // Get the vertex indices for the link text (+ what batch/mesh it belongs to).
                var element = m_textElementInfoArray.GetValue(i);
                var meshIndex = (int)s_materialReferenceIndexMemberInfo.GetValue(element);
                var vertexIndex = (int)s_vertexIndexMemberInfo.GetValue(element);
                var isVisible = (bool)s_isVisibleMemberInfo.GetValue(element);
                
                // Create character info for it.
                var characterInfo = CharacterInfo.GetFromPool();
                characterInfo.MeshIndex = meshIndex;
                characterInfo.VertexIndex = vertexIndex;
                characterInfo.IsVisible = isVisible;
                
                // // Debug: Maybe add the character info in the future?
                // var character = System.Convert.ToChar(TextElementInfoType.GetMemberInfo("character").GetValue(element));
                // debugText += character;

                if (isVisible)
                    m_quadCount++;
                
                // add to results
                m_characterInfos.Add(characterInfo);
            }
            // Debug.Log(debugText);
            
            // TAGS
            
            // Extract animation link infos (store whether or not a link has an animation).
            parseAnimationTags(Element.text, m_animationIdTexts);
            parseDelayTags(Element.text, m_delayTagInfos);

            // Execution will stop here if there is no link info.
            m_linkInfoArray = s_linkInfoMemberInfo.GetValue(textInfo) as Array;
            if (m_linkInfoArray == null || m_linkInfoArray.Length == 0)
                return;
            
            m_linkInfoCount = (int)s_linkCountMemberInfo.GetValue(textInfo);

            int validLinkCount = Mathf.Min(m_animationIdTexts.Count, m_linkInfoCount);
            if (m_animationIdTexts.Count != m_linkInfoCount && m_linkInfoCount > 0)
            {
                // Happens in opening/Closing tags are not in sync or mixed. Also happens in the editor sometimes.
                // TODO / N2H: Use linkIdFirstCharacterIndex instead of regular expressions to extract the link tag content (anim=".."). This would solve this issue.
                // see: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/TextCoreTextEngine/Managed/LinkInfo.cs
                Debug.Log("Animation link regexp result count (" + m_animationIdTexts.Count +
                           ") does not match linkInfo count (" + m_linkInfoCount + ")! " +
                           "You probably have a malformed tag structures (mixed opening and closing tags). " +
                           "Animations will only happen for the first (" + validLinkCount +
                           ") tags and ids may be mixed up.");
            }

            for (int i = 0; i < validLinkCount; i++)
            {
                var animationTagInfo = AnimationTagInfo.GetFromPool();

                // Skip further processing if it's <link> tag without a valid "anim" attribute.
                if (string.IsNullOrEmpty(m_animationIdTexts[i]))
                    continue;

                animationTagInfo.Ids = m_animationIdTexts[i].Split(',');

                // Get Link info (start index and length)
                var linkInfo = m_linkInfoArray.GetValue(i);
                int linkTextFirstCharacterIndex = (int)s_linkTextFirstCharacterIndexMemberInfo.GetValue(linkInfo);
                int linkTextLength = (int)s_linkTextLengthMemberInfo.GetValue(linkInfo);

                animationTagInfo.LinkTextFirstCharacterIndex = linkTextFirstCharacterIndex;
                animationTagInfo.LinkTextLength = linkTextLength;

                // Get vertex infos from text element infos
                var vertexIndices = new List<int>();
                var materialReferenceIndices = new List<int>();
                for (int c = 0; c < linkTextLength; c++)
                {
                    // Get the vertex indices for the link text (+ what batch/mesh it belongs to).
                    var element = m_textElementInfoArray.GetValue(linkTextFirstCharacterIndex + c);
                    // SKip invisible characters (like space or tab etc.) since these always have a vertex index of 0.
                    var isVisible = (bool)s_isVisibleMemberInfo.GetValue(element);
                    if (!isVisible)
                        continue;
                    vertexIndices.Add((int)s_vertexIndexMemberInfo.GetValue(element));
                    materialReferenceIndices.Add((int)s_materialReferenceIndexMemberInfo.GetValue(element));
                }

                animationTagInfo.VertexIndices = vertexIndices;
                animationTagInfo.MaterialReferenceIndices = materialReferenceIndices;

                animationTagInfos.Add(animationTagInfo);
            }
        }

#if UNITY_2023_1_OR_NEWER
        public bool MeshIsDirty()
        {
            var uitkTextHandle = GetUITKTextHandle();
            System.Object settings = s_textGenerationSettingsMemberInfo.GetValue(null);
            int hashCode = settings.GetHashCode();
            int previousHash = (int)s_lastHashMemberInfo.GetValue(uitkTextHandle);
            // IsDirty(int hash) == isDirty || hash != hash == isDirty when hash == previousHash
            bool isDirty = s_isDirtyMemberInfo == null || (bool)s_isDirtyMemberInfo.GetValue(uitkTextHandle);
            
            // Mirrors the inverse of UpdateMesh() of UITKTextHandle, see: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/Text/UITKTextHandle.cs#L74
            // if (this.m_PreviousGenerationSettingsHash == hashCode && !this.isDirty) 
            return previousHash != hashCode || isDirty;
        }
        
        public void UpdateMesh()
        {
            var uitkTextHandle = GetUITKTextHandle();
#if UNITY_6000_0_OR_NEWER
            uitkTextHandle.UpdateMesh();
#else
            uitkTextHandle.Update();
            //s_updateMethodInfo.Invoke(uitkTextHandle, null);
#endif
        }
#endif

        public delegate void ChangeCharacterDelegate(
            AnimationTagInfo tagInfo,
            int characterIndex,
            int quadIndex,
            int totalQuadCount,
            QuadVertexData vertexData, 
            List<DelayTagInfo> delayTagInfos);

        protected int m_quadCount;
        public int QuadCount => m_quadCount;
        
#if UNITY_2023_1_OR_NEWER
        
        // Unity 2023 and Unity 6

        protected Vector2 m_lastVertex0UV2 = new Vector2(-1, -1);
        protected bool m_verticesOverwrittenByUnity;
        internal List<Array> m_tmpMeshVertices = new();
        
        // A flag that tells us whether or not a typewriter animation has been run on all vertices.
        // If yes then the tag animation run can assume all vertices are reset to original and does not have to
        // use the cached originals.
        public bool DidCacheAllOriginalVertices;

        public void CacheMeshVertexAccess()
        {
            m_tmpMeshVertices.Clear(); 
            
            // Would be nice to cache these too but sadly the textElementInfo is regenerated internally
            // so we have to reacquire the reference every time.
            var uitkTextHandle = GetUITKTextHandle();
            if (uitkTextHandle == null)
                return;

            var textInfo = s_textInfoMemberInfo.GetValue(uitkTextHandle);
            if (textInfo == null)
                return;

            var meshInfos = s_meshInfoMemberInfo.GetValue(textInfo) as Array;
            if (meshInfos != null)
            {
                // Check if the meshInfos array is big enough to contain the tagInfo.MaterialReferenceIndices
                // NOTICE: We assume the tagInfo.MaterialReferenceIndices are zero based and match the meshInfoArray
                //         so we can use it to match tag infos to mesh infos.
                for (int i = 0; i < meshInfos.Length; i++)
                {
                    var meshInfo = meshInfos.GetValue(i);
                    var vertices = s_vertexDataMemberInfo != null
                        ? s_vertexDataMemberInfo.GetValue(meshInfo) as Array
                        : null;

                    // Fill list for later use.
                    // NOTICE we do not skip invalid vertices entries (null or empty ones).
                    // Why? Because in the ExecuteOnTagVertices() method we use the materialReferenceIndex
                    // as the list index for m_tmpMeshVertices and thus we have to preserve the indices (even
                    // if some values may be null or empty).
                    m_tmpMeshVertices.Add(vertices);
                }
            }
        }
        
        /// <summary>
        /// Detects if vertices have been reset by uv2 change.<br />
        /// To do this we use a nasty hack: We change the uv2 of the very first vertex by
        /// a tiny amount and then use that value to check if it was changed.
        /// This assumes that we never change the uv2 in any animation which is guaranteed
        /// by only allowing color and position changes via changeCharacterFunc().
        /// </summary>
        public void UpdateVerticesOverwrittenFlag()
        {
            m_verticesOverwrittenByUnity = false;
            
            bool vertexChangeCheckPerformed = false;
            foreach (var vertices in m_tmpMeshVertices)
            {
                // Skip invalid vertices lists
                if (vertices == null || vertices.Length == 0)
                    continue;

                if (!vertexChangeCheckPerformed)
                {
                    vertexChangeCheckPerformed = true;
                    var vertex = vertices.GetValue(0);
                    var uv2 = (Vector2)s_textCoreVertexUV2MemberInfo.GetValue(vertex);
                    if (uv2 != m_lastVertex0UV2)
                    {
                        uv2 += uv2.x > 0.5f
                            ? new Vector2(-0.001f, 0f)
                            : new Vector2(0.001f, 0f);
                        s_textCoreVertexUV2MemberInfo.SetValue(vertex, uv2);
                        vertices.SetValue(vertex, 0);
                        
                        m_lastVertex0UV2 = uv2;
                        
                        // Mark as changed
                        m_verticesOverwrittenByUnity = true;
                        ClearOriginalVertexCache();

                        break;
                    }
                }
            }
        }
        
        public void ClearOriginalVertexCache()
        {
            VertexData.ReturnToPool(m_originalVertices);
        }

        private void cacheOriginalVertex(int meshIndex, int vertexIndex0, Vector3 position, Color32 color)
        {
            int key = getKeyFrom(meshIndex, vertexIndex0);
            if (!m_originalVertices.ContainsKey(key))
            {
                var vertexData = VertexData.GetFromPool(position, color);
                m_originalVertices.Add(key, vertexData);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int getKeyFrom(int meshIndex, int vertexIndex)
        {
            return meshIndex * 10000 + vertexIndex;
        }

        public void ExecuteOnAllVertices(ChangeCharacterDelegate changeCharacterFunc)
        {
            if (changeCharacterFunc == null)
                return;

            int charCount = m_characterInfos.Count;
            int quadCount = 0;
            for (int c = 0; c < charCount; c++)
            {
                var charInfo = m_characterInfos[c];
                if (!charInfo.IsVisible)
                    continue;

                // TODO: Investigate why this happens (usually after a recompile / domain reload).
                if (charInfo.MeshIndex >= m_tmpMeshVertices.Count)
                    continue;
                
                var vertices = m_tmpMeshVertices[charInfo.MeshIndex];
                int vertexIndex3 = charInfo.VertexIndex + 3;
                if (vertexIndex3 >= vertices.Length)
                    continue;

                quadCount++;
            }

            int quadIndex = 0;
            for (int c = 0; c < charCount; c++)
            {
                var charInfo = m_characterInfos[c];
                
                if (!charInfo.IsVisible)
                    continue;

                // TODO: Investigate why this happens (usually after a recompile / domain reload).
                if (charInfo.MeshIndex >= m_tmpMeshVertices.Count)
                    continue;
                
                // var entry = m_tmpTextEntries[quad.MeshIndex];
                var vertices = m_tmpMeshVertices[charInfo.MeshIndex];
                
                int vertexIndex0 = charInfo.VertexIndex;
                int vertexIndex1 = charInfo.VertexIndex + 1;
                int vertexIndex2 = charInfo.VertexIndex + 2;
                int vertexIndex3 = charInfo.VertexIndex + 3;
                
                // TODO: Investigate if still necessary
                if (vertexIndex3 >= vertices.Length)
                    continue;

                Color32 color0 = new Color32();
                Color32 color1 = new Color32();
                Color32 color2 = new Color32();
                Color32 color3 = new Color32();
                
                Vector3 position0 = Vector3.zero;
                Vector3 position1 = Vector3.zero;
                Vector3 position2 = Vector3.zero;
                Vector3 position3 = Vector3.zero;

                int validPositionCount = 0;

                if (m_verticesOverwrittenByUnity)
                {
                    // Cache
                    position0 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                    position1 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex1));
                    position2 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex2));
                    position3 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex3));

                    color0 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                    color1 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                    color2 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                    color3 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                    
                    cacheOriginalVertex(charInfo.MeshIndex, vertexIndex0, position0, color0);
                    cacheOriginalVertex(charInfo.MeshIndex, vertexIndex1, position1, color1);
                    cacheOriginalVertex(charInfo.MeshIndex, vertexIndex2, position2, color2);
                    cacheOriginalVertex(charInfo.MeshIndex, vertexIndex3, position3, color3);

                    validPositionCount = 4;
                }
                else
                {
                    // Get cached positions
                    VertexData vertexData;
                    if (m_originalVertices.TryGetValue(getKeyFrom(charInfo.MeshIndex, vertexIndex0), out vertexData))
                    {
                        position0 = vertexData.Position;
                        color0 = vertexData.Color;
                        validPositionCount++;
                    }

                    if (m_originalVertices.TryGetValue(getKeyFrom(charInfo.MeshIndex, vertexIndex1), out vertexData))
                    {
                        position1 = vertexData.Position;
                        color1 = vertexData.Color;
                        validPositionCount++;
                    }

                    if (m_originalVertices.TryGetValue(getKeyFrom(charInfo.MeshIndex, vertexIndex2), out vertexData))
                    {
                        position2 = vertexData.Position;
                        color2 = vertexData.Color;
                        validPositionCount++;
                    }

                    if (m_originalVertices.TryGetValue(getKeyFrom(charInfo.MeshIndex, vertexIndex3), out vertexData))
                    {
                        position3 = vertexData.Position;
                        color3 = vertexData.Color;
                        validPositionCount++;
                    }
                }

                if (validPositionCount == 4)
                {
                    var quadVertexData = QuadVertexData.GetFromPool();
                    quadVertexData.BottomLeftPosition = position0;
                    quadVertexData.TopLeftPosition = position1;
                    quadVertexData.TopRightPosition = position2;
                    quadVertexData.BottomRightPosition = position3;
                    quadVertexData.BottomLeftColor = color0;
                    quadVertexData.TopLeftColor = color1;
                    quadVertexData.TopRightColor = color2;
                    quadVertexData.BottomRightColor = color3;
                    
                    // Change
                    changeCharacterFunc.Invoke(
                        null, -1, quadIndex, quadCount, quadVertexData,
                        m_delayTagInfos
                    );

                    // Write Position
                    var vertex = vertices.GetValue(vertexIndex0);
                    s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.BottomLeftPosition);
                    vertices.SetValue(vertex, vertexIndex0);

                    vertex = vertices.GetValue(vertexIndex1);
                    s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.TopLeftPosition);
                    vertices.SetValue(vertex, vertexIndex1);

                    vertex = vertices.GetValue(vertexIndex2);
                    s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.TopRightPosition);
                    vertices.SetValue(vertex, vertexIndex2);

                    vertex = vertices.GetValue(vertexIndex3);
                    s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.BottomRightPosition);
                    vertices.SetValue(vertex, vertexIndex3);

                    // Write Color
                    vertex = vertices.GetValue(vertexIndex0);
                    s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.BottomLeftColor);
                    vertices.SetValue(vertex, vertexIndex0);

                    vertex = vertices.GetValue(vertexIndex1);
                    s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.TopLeftColor);
                    vertices.SetValue(vertex, vertexIndex1);

                    vertex = vertices.GetValue(vertexIndex2);
                    s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.TopRightColor);
                    vertices.SetValue(vertex, vertexIndex2);

                    vertex = vertices.GetValue(vertexIndex3);
                    s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.BottomRightColor);
                    vertices.SetValue(vertex, vertexIndex3);
                    
                    QuadVertexData.ReturnToPool(quadVertexData);
                }
                
                quadIndex++;
            }

            DidCacheAllOriginalVertices = true;
        }
        
        public void ExecuteOnTagVertices(List<AnimationTagInfo> animationTagInfos, ChangeCharacterDelegate changeCharacterFunc)
        {
            if (changeCharacterFunc == null)
                return;
            
            if (m_tmpMeshVertices == null || m_tmpMeshVertices.Count == 0)
                return;
            
            // GO through each tag
            foreach (var tagInfo in animationTagInfos)
            {
                // Go through each character (tagInfo.VertexIndices has one vertexIndex entry per character).
                for (int c = 0; c < tagInfo.VertexIndices.Count; c++)
                {
                    int meshIndex = tagInfo.MaterialReferenceIndices[c];
                    
                    // Check if m_tmpMeshVertices is big enough to contain the tagInfo.MaterialReferenceIndices
                    // NOTICE: We assume the tagInfo.MaterialReferenceIndices are zero based and match m_tmpMeshVertices
                    //         so we can use it to match tag infos to mesh infos.
                    if (meshIndex >= m_tmpMeshVertices.Count)
                        continue;
                    
                    var vertices = m_tmpMeshVertices[meshIndex];
                    if (vertices.Length > tagInfo.VertexIndices[c] + 3)
                    {
                        int vertexIndex0 = tagInfo.VertexIndices[c];
                        int vertexIndex1 = tagInfo.VertexIndices[c] + 1;
                        int vertexIndex2 = tagInfo.VertexIndices[c] + 2;
                        int vertexIndex3 = tagInfo.VertexIndices[c] + 3;

                        Vector3 position0 = Vector3.zero;
                        Vector3 position1 = Vector3.zero;
                        Vector3 position2 = Vector3.zero;
                        Vector3 position3 = Vector3.zero;

                        var color0 = new Color32();
                        var color1 = new Color32();
                        var color2 = new Color32();
                        var color3 = new Color32();

                        int validPositionCount = 0;
                        
                        // Read
                        // Notice: DidCacheAllOriginalVertices makes no sense at first glance but if it is true
                        // then the vertices have already been "reset" to original by a typewriter animation and thus
                        // we have to use the vertices data directly (instead of reading for it from the originals cache)
                        // or else we would lose the changes made by the typewriter animation.
                        if (m_verticesOverwrittenByUnity || DidCacheAllOriginalVertices)
                        {
                            position0 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                            position1 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex1));
                            position2 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex2));
                            position3 = (Vector3)s_textCoreVertexPositionMemberInfo.GetValue(vertices.GetValue(vertexIndex3));

                            color0 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                            color1 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                            color2 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                            color3 = (Color32)s_textCoreVertexColorMemberInfo.GetValue(vertices.GetValue(vertexIndex0));
                            
                            // Cache them if needed (only if changed and not yet cached).
                            if (m_verticesOverwrittenByUnity)
                            {
                                cacheOriginalVertex(meshIndex, vertexIndex0, position0, color0);
                                cacheOriginalVertex(meshIndex, vertexIndex1, position1, color1);
                                cacheOriginalVertex(meshIndex, vertexIndex2, position2, color2);
                                cacheOriginalVertex(meshIndex, vertexIndex3, position3, color3);
                            }

                            validPositionCount = 4;
                        }
                        else
                        {
                            VertexData vertexData;
                            
                            // Fetch the original cache vertex data.
                            if (m_originalVertices.TryGetValue(getKeyFrom(meshIndex, vertexIndex0), out vertexData))
                            {
                                position0 = vertexData.Position;
                                color0 = vertexData.Color;
                                validPositionCount++;
                            }

                            if (m_originalVertices.TryGetValue(getKeyFrom(meshIndex, vertexIndex1), out vertexData))
                            {
                                position1 = vertexData.Position;
                                color1 = vertexData.Color;
                                validPositionCount++;
                            }

                            if (m_originalVertices.TryGetValue(getKeyFrom(meshIndex, vertexIndex2), out vertexData))
                            {
                                position2 = vertexData.Position;
                                color2 = vertexData.Color;
                                validPositionCount++;
                            }

                            if (m_originalVertices.TryGetValue(getKeyFrom(meshIndex, vertexIndex3), out vertexData))
                            {
                                position3 = vertexData.Position;
                                color3 = vertexData.Color;
                                validPositionCount++;
                            }
                        }

                        if (validPositionCount == 4)
                        {
                            var quadVertexData = QuadVertexData.GetFromPool();
                            quadVertexData.BottomLeftPosition = position0;
                            quadVertexData.TopLeftPosition = position1;
                            quadVertexData.TopRightPosition = position2;
                            quadVertexData.BottomRightPosition = position3;
                            quadVertexData.BottomLeftColor = color0;
                            quadVertexData.TopLeftColor = color1;
                            quadVertexData.TopRightColor = color2;
                            quadVertexData.BottomRightColor = color3;
                            
                            // Change
                            changeCharacterFunc.Invoke(tagInfo, tagInfo.LinkTextFirstCharacterIndex + c,
                                -1, -1, quadVertexData,
                                m_delayTagInfos
                            );
                            
                            // Write Position
                            var vertex = vertices.GetValue(vertexIndex0);
                            s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.BottomLeftPosition);
                            vertices.SetValue(vertex, vertexIndex0);

                            vertex = vertices.GetValue(vertexIndex1);
                            s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.TopLeftPosition);
                            vertices.SetValue(vertex, vertexIndex1);

                            vertex = vertices.GetValue(vertexIndex2);
                            s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.TopRightPosition);
                            vertices.SetValue(vertex, vertexIndex2);

                            vertex = vertices.GetValue(vertexIndex3);
                            s_textCoreVertexPositionMemberInfo.SetValue(vertex, quadVertexData.BottomRightPosition);
                            vertices.SetValue(vertex, vertexIndex3);

                            // Write Color
                            vertex = vertices.GetValue(vertexIndex0);
                            s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.BottomLeftColor);
                            vertices.SetValue(vertex, vertexIndex0);

                            vertex = vertices.GetValue(vertexIndex1);
                            s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.TopLeftColor);
                            vertices.SetValue(vertex, vertexIndex1);

                            vertex = vertices.GetValue(vertexIndex2);
                            s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.TopRightColor);
                            vertices.SetValue(vertex, vertexIndex2);

                            vertex = vertices.GetValue(vertexIndex3);
                            s_textCoreVertexColorMemberInfo.SetValue(vertex, quadVertexData.BottomRightColor);
                            vertices.SetValue(vertex, vertexIndex3);
                    
                            QuadVertexData.ReturnToPool(quadVertexData);
                        }
                    }
                }
            }
        }
#else
        
        // UNITY 2022
        
        public struct TextQuadReference
        {
            public int MeshIndex;
            public int VertexIndex;

            public TextQuadReference(int meshIndex, int vertexIndex)
            {
                MeshIndex = meshIndex;
                VertexIndex = vertexIndex;
            }
        }

        protected List<TextQuadReference> m_quadRefs = new List<TextQuadReference>();

        /// <summary>
        /// Will cause a renewal of that in the next call to TextInfoAccessor.ExecuteOnAllVertices();
        /// </summary>
        public void ClearQuadInformation()
        {
            if (IsValid)
                m_quadRefs.Clear();
        }

        public void ExecuteOnAllVertices(MeshGenerationContext mgc, ChangeCharacterDelegate changeCharacterFunc)
        {
            if (changeCharacterFunc == null)
                return;

            var painter = mgc.painter as UIRStylePainter;
            if (painter == null)
                return;
            
            var entries = painter.entries;
            if (entries == null || entries.Count == 0)
                return;
            
            // Filter out entries invalid entries and store in s_tmpTextEntries
            filterTextEntriesIfNeeded(entries, m_tmpTextEntries, m_tmpTextEntriesVertices);

            if (m_quadRefs.Count == 0)
                updateQuadInformation(m_tmpTextEntries, m_tmpTextEntriesVertices);

            int quadCount = m_quadRefs.Count;
            for (int quadIndex = 0; quadIndex < quadCount; quadIndex++)
            {
                var quad = m_quadRefs[quadIndex];
                // Each entry is one mesh (batch). New font types will cause additional entries (batches) as
                // will the usage of <sprite>s (basically anything that requires a different material).
                
                // var entry = m_tmpTextEntries[quad.MeshIndex];
                var vertices = m_tmpTextEntriesVertices[quad.MeshIndex];
                
                // Check if there are enough vertices. Why is this necessary?
                // If for example a <sprite> tag was in the text but the sprite atlas was missing then the text will
                // contain the text "<sprite ...>" but if the user then assigns a proper sprite atlas then the "<sprite..>"
                // text (multiple quads) will be replaced with just one quad. This shortens the number of vertices and makes
                // this check necessary. TODO: Emmit a event or trigger a rebuild of the caches if this happens. For now we
                // just  ignore it since it auto resolves and typically is an Editor-only issue.
                if (vertices.Length < quad.VertexIndex)
                    continue;
                
                // Group the 4 vertices of each vertex together and hand it over to changeCharacterFunc.
                Vertex vertex0 = vertices[quad.VertexIndex];
                Vertex vertex1 = vertices[quad.VertexIndex + 1];
                Vertex vertex2 = vertices[quad.VertexIndex + 2];
                Vertex vertex3 = vertices[quad.VertexIndex + 3];

                var vertexData = QuadVertexData.CreateFromVertices(vertex0, vertex1, vertex2, vertex3);
                
                changeCharacterFunc.Invoke(
                    null, -1,
                    quadIndex, quadCount,
                    vertexData,
                    m_delayTagInfos
                );

                vertex0.position = vertexData.BottomLeftPosition;
                vertex1.position = vertexData.TopLeftPosition;
                vertex2.position = vertexData.TopRightPosition;
                vertex3.position = vertexData.BottomRightPosition;

                vertex0.tint = vertexData.BottomLeftColor;
                vertex1.tint = vertexData.TopLeftColor;
                vertex2.tint = vertexData.TopRightColor;
                vertex3.tint = vertexData.BottomRightColor;
                
                QuadVertexData.ReturnToPool(vertexData);

                vertices[quad.VertexIndex] = vertex0;
                vertices[quad.VertexIndex + 1] = vertex1;
                vertices[quad.VertexIndex + 2] = vertex2;
                vertices[quad.VertexIndex + 3] = vertex3;
            }
        }

        private bool m_textEntriesFiltered;
        
        private void filterTextEntriesIfNeeded(List<UIRStylePainter.Entry> entries, List<UIRStylePainter.Entry> tmpTextEntries, List<NativeSlice<Vertex>> tmpTextEntriesVertices)
        {
            if (m_textEntriesFiltered)
                return;
            
            // Filter out entries with invalid textures.
            // Sometimes the entries list is filled with entries that do not have valid textures.
            // I am not 100% why this happens (TODO: Find out why exactly this happens).
            // The current solution is to filter out any invalid entries and use that list (see: s_tmpTextEntries).
            // That makes it match with the MaterialReferenceIndices.
            tmpTextEntries.Clear();
            tmpTextEntriesVertices.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].texture.IsValid())
                {
                    // Add to filtered list.
                    tmpTextEntries.Add(entries[i]);
                    
                    // Extract vertices here and cache them in tmpTextEntriesVertices for later use.
                    tmpTextEntriesVertices.Add(entries[i].vertices);
                }
            }

            m_textEntriesFiltered = true;
        }

        public void OnStartGenerateVisualContent()
        {
            m_textEntriesFiltered = false;
        }

        internal void updateQuadInformation(List<UIRStylePainter.Entry> filteredEntries, List<NativeSlice<Vertex>> filteredEntriesVertices)
        {
            if (filteredEntries == null || filteredEntries.Count == 0)
                return;

            // Since we have no character to vertex information (except for tags) we attempt
            // to sort the vertices by quad (assuming every 4 vertices is a new quad = new char).
            // We also count the number of quads which is handy for typewriter like animations.
            m_quadRefs.Clear();
            for (int e = 0; e < filteredEntries.Count; e++)
            {
                // The vertex order is bottom-left, top-left, ...
                // We want to compare by bottom left so we use the first vertex for the quad
                var vertices = filteredEntriesVertices[e];
                int numOfVertices = vertices.Length;
                for (int i = 0; i < numOfVertices; i+=4)
                {
                    m_quadRefs.Add(new TextQuadReference(e, i));

                    float min = 100000f; 
                    for (int j = 0; j < 3; j++)
                    {
                        min = Mathf.Min(m_tmpTextEntriesVertices[e][i+j].position.x, min);
                    }
                }
            }
            // Sort the quads by position (assuming top-left text).
            // Problem: What if some text is bigger (can not sort properly ..).
            // Solution: Sort by lower-left vertices because baseline is rarely changed.
            // Solution improvement: Take order within batch into account.
            m_quadRefs.Sort(compareQuadsLeftToRight);

            m_quadCount = m_quadRefs.Count;
        }
        
        /// <summary>
        /// IF A comes before B then -1 is returned, otherwise 0 (equal) or 1.
        /// </summary>
        /// <param name="quadA"></param>
        /// <param name="quadB"></param>
        /// <returns></returns>
        protected int compareQuadsLeftToRight(TextQuadReference quadA, TextQuadReference quadB)
        {
            if (quadA.MeshIndex == quadB.MeshIndex)
                return quadA.VertexIndex - quadB.VertexIndex;

            var verticesA = m_tmpTextEntriesVertices[quadA.MeshIndex];
            var verticesB = m_tmpTextEntriesVertices[quadB.MeshIndex];
            float lineHeight = Mathf.Max(
                Mathf.Abs(verticesA[quadA.VertexIndex].position.y - verticesA[quadA.VertexIndex + 1].position.y),
                Mathf.Abs(verticesB[quadB.VertexIndex].position.y - verticesB[quadB.VertexIndex + 1].position.y)
            );
            return compareVertexLeftToRightText(
                verticesA[quadA.VertexIndex].position,
                verticesB[quadB.VertexIndex].position, lineHeight) ? -1 : 1;
        }

        // Right to left not yet supported since UI Toolkit does not support it either in this Unity version.
        // It is only added in Unity 6 via the Advanced Text Generator, see:
        // https://discussions.unity.com/t/right-to-left-and-arabic-support-for-labels/888593/6
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool compareVertexLeftToRightText(Vector3 vertexA, Vector3 vertexB, float lineHeight)
        {
            // Either it's above or to the left (with a margin of 0.5 line height).
            float margin = lineHeight * 0.5f;
            bool inSameLine = vertexA.y > vertexB.y - margin && vertexA.y < vertexB.y + margin;
            return vertexA.y < vertexB.y - margin || (inSameLine && vertexA.x < vertexB.x);
        }

        private List<UIRStylePainter.Entry> m_tmpTextEntries = new List<UIRStylePainter.Entry>();
        private List<NativeSlice<Vertex>> m_tmpTextEntriesVertices = new List<NativeSlice<Vertex>>();

        public void ExecuteOnTagVertices(MeshGenerationContext mgc, List<AnimationTagInfo> animationTagInfos,
            ChangeCharacterDelegate changeCharacterFunc)
        {
            if (changeCharacterFunc == null)
                return;

            // Would be nice to cache these too but sadly the textElementInfo is regenerated internally
            // so we have to reacquire the reference every time.
            var uitkTextHandle = GetUITKTextHandle();

            var textInfo =  uitkTextHandle.textInfo;
            if (textInfo == null)
                return;

            var painter = mgc.painter as UIRStylePainter;
            if (painter == null)
                return;
            
            var entries = painter.entries;
            if (entries == null || entries.Count == 0)
                return;

            // Filter out entries with invalid textures.
            // Sometimes the entries list is filled with entries that do not have valid textures.
            // I am not 100% why this happens (TODO: Find out why exactly this happens).
            // The current solution is to filter out any invalid entries and use that list (see: s_tmpTextEntries).
            // That makes it match with the MaterialReferenceIndices.
            filterTextEntriesIfNeeded(entries, m_tmpTextEntries, m_tmpTextEntriesVertices);
            
            var meshInfoArray = textInfo.meshInfo;
            if (meshInfoArray != null)
            {
                // Go through each tag
                foreach (var tagInfo in animationTagInfos)
                {
                    // Go through each character
                    for (int c = 0; c < tagInfo.VertexIndices.Count; c++)
                    {
                        // Check if the entries list is big enough to contain the tagInfo.MaterialReferenceIndices
                        // NOTICE: We assume the tagInfo.MaterialReferenceIndices are zero based and match the entries
                        //         so we can use it to match tag infos to entries.
                        // TODO: Sadly this does not always hold true, especially when sprite tags are involved or
                        //       tags are mixed (opening / closing tags).
                        if (m_tmpTextEntries.Count > tagInfo.MaterialReferenceIndices[c])
                        {
                            int meshIndex = tagInfo.MaterialReferenceIndices[c];
                            // Each entry is one mesh (batch). New font types will cause additional entries (batches) as
                            // will the usage of <sprite>s (basically anything that requires a different material).
                            // var entry = m_tmpTextEntries[meshIndex];
                            var vertices = m_tmpTextEntriesVertices[meshIndex];

                            // Securing against index out of bounds errors (this only happens if the
                            // MaterialReference assumption from above fails, which is rare).
                            if (vertices.Length < tagInfo.VertexIndices[c] + 3)
                                continue;
                            
                            // Group the 4 vertices of each vertex together and hand it over to changeCharacterFunc.
                            Vertex vertex0 = vertices[tagInfo.VertexIndices[c]];
                            Vertex vertex1 = vertices[tagInfo.VertexIndices[c] + 1];
                            Vertex vertex2 = vertices[tagInfo.VertexIndices[c] + 2];
                            Vertex vertex3 = vertices[tagInfo.VertexIndices[c] + 3];
                            
                            var vertexData = QuadVertexData.CreateFromVertices(vertex0, vertex1, vertex2, vertex3);

                            changeCharacterFunc.Invoke(
                                tagInfo, tagInfo.LinkTextFirstCharacterIndex + c,
                                -1, -1,
                                vertexData,
                                m_delayTagInfos
                            );

                            vertex0.position = vertexData.BottomLeftPosition;
                            vertex1.position = vertexData.TopLeftPosition;
                            vertex2.position = vertexData.TopRightPosition;
                            vertex3.position = vertexData.BottomRightPosition;

                            vertex0.tint = vertexData.BottomLeftColor;
                            vertex1.tint = vertexData.TopLeftColor;
                            vertex2.tint = vertexData.TopRightColor;
                            vertex3.tint = vertexData.BottomRightColor;
                            
                            QuadVertexData.ReturnToPool(vertexData);

                            vertices[tagInfo.VertexIndices[c]] = vertex0;
                            vertices[tagInfo.VertexIndices[c] + 1] = vertex1;
                            vertices[tagInfo.VertexIndices[c] + 2] = vertex2;
                            vertices[tagInfo.VertexIndices[c] + 3] = vertex3;
                        }
                    }
                }
            }
        }

#endif
        
        // Allows both <link anim="id">text</link> or <link anim='id'>text</link>
        static string s_animationTagPattern = @"<link( .*?anim=['""]([^""']*)['""][^>]*|[^>]*)>";
        static Regex s_animationTagRegEx = new Regex(s_animationTagPattern);
        
        /// <summary>
        /// Adds one entry for each link tag. If the link tag does not have an "anim" attribute then the id will be NULL but each link tag will have an entry in the ids list.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="idTexts">One or more ids separated by a comma</param>
        /// <returns></returns>
        List<string> parseAnimationTags(string text, List<string> idTexts)
        {
            if (idTexts == null)
                idTexts = new List<string>();
            else
                idTexts.Clear();
            
            MatchCollection matches = s_animationTagRegEx.Matches(text);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    idTexts.Add(match.Groups[2].Value);
                }
            }

            return idTexts;
        }
        
        // Allows both </noparse delay="3"> or </noparse delay='3'>
        static string s_delayTagPattern = @"</noparse( .*?delay=['""]([^""']*)['""][^>]*|[^>]*)>";
        static Regex s_delayTagRegEx = new Regex(s_delayTagPattern);

        public class DelayTagInfo
        {
            private static Stack<DelayTagInfo> s_pool = new Stack<DelayTagInfo>();

            public static DelayTagInfo GetFromPool()
            {
                if (s_pool.Count > 0)
                {
                    return s_pool.Pop();
                }
                
                var newCopy = new DelayTagInfo();
                return newCopy;
            }
        
            public static void ReturnToPool(DelayTagInfo info)
            {
                s_pool.Push(info);
            }
        
            public static void ReturnToPool(IList<DelayTagInfo> infos)
            {
                if (infos == null)
                    return;
                
                for (int i = infos.Count - 1; i >= 0; i--)
                {
                    if (infos[i] != null)
                        ReturnToPool(infos[i]);
                }

                infos.Clear();
            }
        
            public int CharIndex;
            public int QuadIndex;
            public float DelayInSec;
        }
        
        List<DelayTagInfo> parseDelayTags(string text, List<DelayTagInfo> delayInfos)
        {
            if (delayInfos == null)
                delayInfos = new List<DelayTagInfo>();
            else
                DelayTagInfo.ReturnToPool(delayInfos);

            MatchCollection matches = s_delayTagRegEx.Matches(text);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float delayInSec))
                    {
                        var info = DelayTagInfo.GetFromPool();
                        SourceTextIndexToRenderedIndex(match.Index, out info.CharIndex, out info.QuadIndex);
                        info.DelayInSec = delayInSec;
                        delayInfos.Add(info);
                    }
                }
            }

            return delayInfos;
        }
    }
}