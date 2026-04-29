using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Kamgam.UIToolkitTextAnimation
{
    public abstract partial class TextAnimation
    {
        private static Dictionary<string, Stack<TextAnimation>> s_pool = new Dictionary<string, Stack<TextAnimation>>();

        public static int PoolSize => s_pool.Count;

        public static void GetCopiesBasedOnTagInfos(List<TextInfoAccessor.AnimationTagInfo> tagInfos, TextAnimations globalConfigs, List<TextAnimation> results)
        {
            if (results == null)
                results = new List<TextAnimation>();
            else
                ResetAndReturnToPool(results);
                
            foreach (var tagInfo in tagInfos)
            {
                foreach (var id in tagInfo.Ids)
                {
                    var baseConfig = globalConfigs.GetAnimation(id);
                    if (baseConfig != null && baseConfig)
                    {
                        var animation = GetCopyFromPool(baseConfig);
                        results.Add(animation);
                    }
                    else
                    {
                        results.Add(null);
                    }
                }
            }
        }
        
        public static void GetCopiesFromIds(List<string> ids, TextAnimations globalConfigs, List<TextAnimation> results)
        {
            if (results == null)
                results = new List<TextAnimation>();
            else
                ResetAndReturnToPool(results);

            foreach (var id in ids)
            {
                results.Add(GetCopyFromPool(globalConfigs.GetAnimation(id)));
            }
        }
        
        public static TextAnimation GetCopyFromPool(TextAnimation baseAnimation)
        {
            if (baseAnimation == null)
                return null;
            
            string id = baseAnimation.m_id;
            
            if (s_pool.TryGetValue(id, out var pool))
            {
                if (pool.Count > 0)
                {
                    var copy = pool.Pop();
                    copy.CopyValuesFrom(baseAnimation);
                    copy.Parent = baseAnimation;
                    return copy;
                }
            }
            return baseAnimation.Copy();
        }
        
        public static void ResetAndReturnToPool(TextAnimation config)
        {
            if (config == null)
                return;
            
            string id = config.Id;
            
            if (!s_pool.ContainsKey(id))
                s_pool.Add(id, new Stack<TextAnimation>());
            
            config.Reset();
            s_pool[id].Push(config);
        }
        
        public static void ResetAndReturnToPool(IList<TextAnimation> configs)
        {
            if (configs == null)
                return;
            
            for (int i = configs.Count - 1; i >= 0; i--)
            {
                ResetAndReturnToPool(configs[i]);
            }

            configs.Clear();
        }
        
        
        // Quad transformation helpers
        public static Vector3 GetQuadCenter(TextInfoAccessor.QuadVertexData quadVertexData)
        {
            return (quadVertexData.BottomLeftPosition + quadVertexData.TopRightPosition) * 0.5f;
        }
        
        /// <summary>
        /// Returns the relative center to the quad (-1/-1 = bottom left, 1/1 = top right).
        /// </summary>
        /// <param name="quadVertexData"></param>
        /// <param name="relativeCenter"></param>
        /// <returns></returns>
        public static Vector3 GetQuadCenter(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 relativeCenter)
        {
            var center = (quadVertexData.BottomRightPosition + quadVertexData.TopLeftPosition) * 0.5f;
            var centerToTopRight = quadVertexData.BottomRightPosition - center;
            return new Vector3(
                center.x + centerToTopRight.x * relativeCenter.x,
                center.y + centerToTopRight.y * relativeCenter.y,
                center.z + centerToTopRight.z * relativeCenter.z
            );
        }

        public static void RotateQuadAround(TextInfoAccessor.QuadVertexData quadVertexData, Vector2 rotationCenter, Vector3 anglesToRotateBy)
        {
            RotateQuadAround(quadVertexData, rotationCenter, anglesToRotateBy, AffectedCorners.one);
        }

        public static void RotateQuadAround(TextInfoAccessor.QuadVertexData quadVertexData, Vector2 rotationCenter, Vector3 anglesToRotateBy, AffectedCorners affectedCorners)
        {
            var center = new Vector3(rotationCenter.x, rotationCenter.y, GetQuadCenter(quadVertexData).z);
            RotateQuadAround(quadVertexData, center, anglesToRotateBy, affectedCorners);
        }
        
        public static void RotateQuadAround(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 rotationCenter, Vector3 anglesToRotateBy)
        {
            RotateQuadAround(quadVertexData, rotationCenter, anglesToRotateBy, AffectedCorners.one);
        }

        public static void RotateQuadAround(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 rotationCenter, Vector3 anglesToRotateBy, AffectedCorners affectedCorners)
        {
            var q = Quaternion.Euler(anglesToRotateBy);
            
            quadVertexData.BottomLeftPosition = rotationCenter + Quaternion.Lerp(Quaternion.identity, q, affectedCorners.BottomLeft) * (quadVertexData.BottomLeftPosition - rotationCenter);
            quadVertexData.TopLeftPosition = rotationCenter + Quaternion.Lerp(Quaternion.identity, q, affectedCorners.TopLeft) * (quadVertexData.TopLeftPosition - rotationCenter);
            quadVertexData.TopRightPosition = rotationCenter + Quaternion.Lerp(Quaternion.identity, q, affectedCorners.TopRight) * (quadVertexData.TopRightPosition - rotationCenter);
            quadVertexData.BottomRightPosition = rotationCenter + Quaternion.Lerp(Quaternion.identity, q, affectedCorners.BottomRight) * (quadVertexData.BottomRightPosition - rotationCenter);
        }
        
        public static void ScaleQuad(TextInfoAccessor.QuadVertexData quadVertexData, Vector2 scaleCenter, Vector3 scaleFactors)
        {
            ScaleQuad(quadVertexData, scaleCenter, scaleFactors, AffectedCorners.one);
        }

        public static void ScaleQuad(TextInfoAccessor.QuadVertexData quadVertexData, Vector2 scaleCenter, Vector3 scaleFactors, AffectedCorners affectedCorners)
        {
            var center = new Vector3(scaleCenter.x, scaleCenter.y, GetQuadCenter(quadVertexData).z);
            ScaleQuad(quadVertexData, center, scaleFactors, affectedCorners);
        }
        
        public static void ScaleQuad(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 scaleCenter, Vector3 scaleFactors)
        {
            ScaleQuad(quadVertexData, scaleCenter, scaleFactors, AffectedCorners.one);
        }

        public static void ScaleQuad(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 scaleCenter, Vector3 scaleFactors, AffectedCorners affectedCorners)
        {
            quadVertexData.BottomLeftPosition = scaleCenter + ScaleVectorBy(quadVertexData.BottomLeftPosition - scaleCenter, Vector3.Lerp(Vector3.one, scaleFactors, affectedCorners.BottomLeft));
            quadVertexData.TopLeftPosition = scaleCenter + ScaleVectorBy(quadVertexData.TopLeftPosition - scaleCenter, Vector3.Lerp(Vector3.one, scaleFactors, affectedCorners.TopLeft));
            quadVertexData.TopRightPosition = scaleCenter + ScaleVectorBy(quadVertexData.TopRightPosition - scaleCenter, Vector3.Lerp(Vector3.one, scaleFactors, affectedCorners.TopRight));
            quadVertexData.BottomRightPosition = scaleCenter + ScaleVectorBy(quadVertexData.BottomRightPosition - scaleCenter, Vector3.Lerp(Vector3.one, scaleFactors, affectedCorners.BottomRight));
        }
        
        public static Vector3 ScaleVectorBy(Vector3 v, Vector3 scale) => new Vector3(scale.x * v.x, scale.y * v.y, scale.z * v.z);
        
        public static void TranslateQuad(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 delta)
        {
            TranslateQuad(quadVertexData, delta, AffectedCorners.one);
        }
        
        public static void TranslateQuad(TextInfoAccessor.QuadVertexData quadVertexData, Vector3 delta, AffectedCorners affectedCorners)
        {
            quadVertexData.BottomLeftPosition += delta * affectedCorners.BottomLeft;
            quadVertexData.TopLeftPosition += delta *  affectedCorners.TopLeft;
            quadVertexData.TopRightPosition += delta * affectedCorners.TopRight;
            quadVertexData.BottomRightPosition += delta * affectedCorners.BottomRight;
        }
        
        public static Color32 SetVertexColor(Color32 vertexColor, Color32 newColor, float affectCornerValue, bool setAlpha, bool alphaOnly, float lerp)
        {
            var alpha = vertexColor.a;
            
            Color32 tmpNewColor;
            if (setAlpha)
            {
                tmpNewColor = Color32.Lerp(vertexColor, newColor, lerp * affectCornerValue);
                // Alpha is not set but multiplied.
                tmpNewColor.a = (byte) Mathf.RoundToInt(alpha * (tmpNewColor.a / 255f));
            }
            else
            {
                tmpNewColor = Color32.Lerp(vertexColor, newColor, lerp * affectCornerValue);
                tmpNewColor.a = alpha;
            }
            
            if (alphaOnly && setAlpha)
            {
                var col = vertexColor;
                col.a = tmpNewColor.a;
                return col;
            }

            return tmpNewColor;
        }

        public static bool GetRandomBool()
        {
            return Random.value < 0.5f;
        }
        
        public static Vector3 GetRandomVector3(float min, float max)
        {
            return new Vector3(
                Random.Range(min, max),
                Random.Range(min, max),
                Random.Range(min, max)
            );
        }
        
        public static Vector3 GetRandomVector3(
            float xMin, float xMax,
            float yMin, float yMax,
            float zMin, float zMax
            )
        {
            return new Vector3(
                Random.Range(xMin, xMax),
                Random.Range(yMin, yMax),
                Random.Range(zMin, zMax)
            );
        }
        
        public static Vector2 GetRandomVector2(float min, float max)
        {
            return new Vector2(
                Random.Range(min, max),
                Random.Range(min, max)
            );
        }
        
        public static AffectedCorners GetRandomAffectedCorners(float min = -1f, float max = 1f)
        {
            return new AffectedCorners(
                Random.Range(min, max),
                Random.Range(min, max),
                Random.Range(min, max),
                Random.Range(min, max)
            );
        }
        
        public static AnimationCurve GetRandomCurve(float min = 0f, float max = 1f)
        {
            // Random number of keys between 2 and 5
            int keyCount = Random.Range(2, 6);
            Keyframe[] keys = new Keyframe[keyCount];

            // Set the first and last key to (0,0) and (1,1)
            keys[0] = new Keyframe(0f, 0f);
            keys[keyCount - 1] = new Keyframe(1f, 1f);

            // Generate random keys for the other keys
            for (int i = 1; i < keyCount - 1; i++)
            {
                float time = Random.Range(0f, 1f);
                float value = Random.Range(min, max);
                keys[i] = new Keyframe(time, value);
            
                // Randomize tangents
                keys[i].inTangent = Random.Range(-1f, 1f);
                keys[i].outTangent = Random.Range(-1f, 1f);
            }

            return new AnimationCurve(keys);
        }

        public static AnimationCurve GetDefaultCurve()
        {
            return new AnimationCurve( new Keyframe[]
            {
                new Keyframe(0f,0f),
                new Keyframe(0.4f,1f),
                new Keyframe(0.6f,1f),
                new Keyframe(1f,0f)
            });
        }
        
        public static AnimationCurve GetDefaultCurve01()
        {
            return new AnimationCurve( new Keyframe[]
            {
                new Keyframe(0f,0f),
                new Keyframe(1f,1f)
            });
        }
        
        public static Color32 GetRandomColor(int min = 0, int max = 255, bool randomizeAlpha = true)
        {
            var d = max - min;
            return new Color32(
                (byte)(min + Random.value * d),
                (byte)(min + Random.value * d),
                (byte)(min + Random.value * d),
                (byte)(randomizeAlpha ? min + Random.value * d : 255)
                );
        }
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/" + Installer.AssetName + "/Select Configs List", priority = 3)]
        public static void SelectAssetInEditor()
        {
            var configs = TextAnimations.FindAssetInEditor();
            if (configs != null)
            {
                UnityEditor.Selection.objects = new Object[] { configs };
                UnityEditor.EditorGUIUtility.PingObject(configs);
            }
        }
#endif
        
    }
}