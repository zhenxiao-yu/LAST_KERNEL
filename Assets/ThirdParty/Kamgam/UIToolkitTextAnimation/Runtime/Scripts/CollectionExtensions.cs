using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.UIToolkitTextAnimation
{
    public static class CollectionExtensions
    {
        public static void AddIfNotContained<T>(this IList<T> list, T element)
        {
            if (list == null)
                return;

            if (!list.Contains(element))
            {
                list.Add(element);
            }
        }

        public static void AddIfNotContained<T>(this IList<T> list, IList<T> elements)
        {
            if (list == null)
                return;

            foreach (var e in elements)
            {
                if (!list.Contains(e))
                {
                    list.Add(e);
                }
            }
        }

        public static void RemoveAll<T>(this IList<T> list, T element)
        {
            if (list == null)
                return;

            while (list.Contains(element))
                list.Remove(element);
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return (collection == null || collection.Count == 0);
        }

        // The constraint on Unity Objects is important to ensure Unitys "==" operator overload is used.
        // Otherwise it would not return true for a list of MissingReferences.
        public static bool IsNullOrEmptyDeep<T>(this IList<T> list, bool checkReferenceEquals = false) where T : UnityEngine.Object
        {
            if (list == null || list.Count == 0)
                return true;

            foreach (var item in list)
            {
                if (item != null && (!checkReferenceEquals || !System.Object.ReferenceEquals(item, null)))
                    return false;
            }

            return true;
        }

        public static void LogContent<T>(this IList<T> list)
        {
            if (list == null)
            {
                Debug.Log("Null");
                return;
            }

            if (list.Count == 0)
                Debug.Log("Empty");

            string str = "IList["+list.Count+"]: ";
            bool first = true;
            foreach (var ele in list)
            {
                str += (first ? "" : ", ") + ele.ToString();
                first = false;
            }
            Debug.Log(str);
        }
    }
}