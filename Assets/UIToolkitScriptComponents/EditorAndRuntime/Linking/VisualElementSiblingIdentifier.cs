// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    [System.Serializable]
    public class VisualElementSiblingIdentifier
    {
        // Persistent parts (used to find the Element again if needed).
        public string Type;
        public string Name;
        public string DataKey;
        public int Index;

        public static VisualElementSiblingIdentifier CreateOrUpdate(VisualElement element, VisualElementSiblingIdentifier identifier = null)
        {
            if (identifier == null)
                identifier = new VisualElementSiblingIdentifier();

            if (element == null)
                return identifier;

            identifier.Update(element);

            return identifier;
        }

        public void Update(VisualElement element)
        {
            if (element == null)
                return;

            string type, name, dataKey;
            GetElementIdentifyingAttributes(element, out type, out name, out dataKey);

            Type = type;
            Name = name;
            DataKey = dataKey;
            Index = DetermineSiblingIndex(element, type, name, dataKey);
        }

        public static int DetermineSiblingIndex(VisualElement element, string type, string name, string dataKey)
        {
            if (element.parent == null)
                return 0;

            // By name
            if (!string.IsNullOrEmpty(name))
            {
                // find siblings by name
                int siblingIndex = -1;
                for (int i = 0; i < element.parent.childCount; i++)
                {
                    var child = element.parent[i];
                    string childName;
                    GetElementIdentifyingAttributes(child, out _, out childName, out _);
                    if (name == childName)
                    {
                        siblingIndex++;
                    }
                    if (element == child)
                    {
                        return siblingIndex;
                    }
                }
            }
            // By dataKey
            else if (!string.IsNullOrEmpty(dataKey))
            {
                // find siblings by dataKey
                int siblingIndex = -1;
                for (int i = 0; i < element.parent.childCount; i++)
                {
                    var child = element.parent[i];
                    string childDataKey;
                    GetElementIdentifyingAttributes(child, out _, out _, out childDataKey);
                    if (childDataKey == dataKey)
                    {
                        siblingIndex++;
                    }
                    if (element == child)
                    {
                        return siblingIndex;
                    }
                }
            }
            // by type
            else
            {
                // find siblings by type
                int siblingIndex = -1;
                for (int i = 0; i < element.parent.childCount; i++)
                {
                    var child = element.parent[i];
                    string childType;
                    GetElementIdentifyingAttributes(child, out childType, out _, out _);
                    if (childType == type)
                    {
                        siblingIndex++;
                    }
                    if (element == child)
                    {
                        return siblingIndex;
                    }
                }
            }

            return 0;
        }

        public static void GetElementIdentifyingAttributes(VisualElement ele, out string type, out string name, out string dataKey)
        {
            type = ele.GetType().Name;
            name = ele.name;
            dataKey = ele.viewDataKey;
        }

        public bool Matches(VisualElement element)
        {
            string type, name, dataKey;
            GetElementIdentifyingAttributes(element, out type, out name, out dataKey);
            int index = DetermineSiblingIndex(element, type, name, dataKey);

            return Matches(type, name, dataKey, index);
        }

        public bool Matches(VisualElementSiblingIdentifier other)
        {
            return Matches(other.Type, other.Name, other.DataKey, other.Index);
        }

        public bool Matches(string type, string name, string dataKey, int index)
        {
            // Match by Name + index
            if (!string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(name))
            {
                return Name == name && Index == index;
            }
            // If Name is not available then match by DataKey + index
            else if (!string.IsNullOrEmpty(DataKey) || !string.IsNullOrEmpty(dataKey))
            {
                return DataKey == dataKey && Index == index;
            }
            // If DataKey is not available then match by Type + index
            else if (!string.IsNullOrEmpty(Type) || !string.IsNullOrEmpty(type))
            {
                return Type == type && Index == index;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Searches the direct children. No recursion. Does not search the element itself.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns>NULL if not found.</returns>
        public VisualElement FindInChildren(VisualElement parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var element = parent[i];
                if(Matches(element))
                {
                    return element;
                }
            }

            return null;
        }
    }
}
#endif