// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using UnityEngine.UIElements;
using UnityEngine;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// A component that changes the color randomly every frame. Just for demo purposes.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Text Random Colors")]
    public class UITextColorHSV : UIComponentBase<TextElement>
    {
        public bool AutoUpdate = false;

        public float Hue = 0;
        public float Saturation = 1f;
        public float Value = 0.5f;

        void Update()
        {
            if (AutoUpdate)
            {
                // Move the color along the hue.
                Hue = (Hue + (Time.deltaTime / 5f)) % 1f;
                setColorFromHue();
            }
        }

        void setColorFromHue()
        {
            var color = Color.HSVToRGB(Hue, Saturation, Value);

            // Apply to labels
            if (HasElements())
            {
                foreach (var ele in Elements)
                {
                    if (ele == null)
                        continue;

                    ele.style.color = color;
                }
            }
        }

        public void PickRandomColor()
        {
            Hue = Random.value;
            setColorFromHue();
        }

        public void ToogleAutoUpdate()
        {
            AutoUpdate = !AutoUpdate;
        }

        public void SetHue(float hue)
        {
            Hue = hue;
            setColorFromHue();
        }
    }
}

#endif
