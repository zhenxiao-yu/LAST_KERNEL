using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Kamgam.UIToolkitTextAnimation
{
    public static class ReflectionExtensions
    {
        public static void SetValue(this MemberInfo member, object property, object value)
        {
            if (member.MemberType == MemberTypes.Property)
                ((PropertyInfo)member).SetValue(property, value, null);
            else if (member.MemberType == MemberTypes.Field)
                ((FieldInfo)member).SetValue(property, value);
            else
                throw new Exception("Property must be of type FieldInfo or PropertyInfo");
        }
        
        public static object GetValue(this MemberInfo member, object obj)
        {
            if (member.MemberType == MemberTypes.Property)
                return ((PropertyInfo)member).GetValue(obj, null);
            else if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).GetValue(obj);
            else
                throw new Exception("Property must be of type FieldInfo or PropertyInfo");
        }
        
        public static Type GetMemberType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                default:
                    throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo or EventInfo");
            }
        }

        public static BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        
        public static MemberInfo[] GetAllMemberInfos(this Type type, string name)
        {
            return type.GetFields(BindingFlags).Cast<MemberInfo>().Concat(type.GetProperties(BindingFlags)).ToArray();
        }
        
        public static MemberInfo GetMemberInfo(this Type type, string name)
        {
            return type.GetField(name, BindingFlags) as MemberInfo ?? type.GetProperty(name, BindingFlags) as MemberInfo;
        }
    }

    public class CachedMemberInfo<T>
    {
        bool _tried;
        string _memberName;
        public MemberInfo MemberInfo;

        public CachedMemberInfo(string memberName)
        {
            _memberName = memberName;
        }

        public void Reset()
        {
            _tried = false;
            MemberInfo = null;
        }
            
        public bool TryGetMemberInfo(out MemberInfo memberInfo)
        {
            if (_tried)
            {
                memberInfo = MemberInfo;
            }
            else
            {
                memberInfo = MemberInfo = typeof(T).GetMemberInfo(_memberName);
                _tried = true;
            }
                
            return memberInfo != null;
        }
        
        public bool TrySetValue(T target, object value)
        {
            try
            {
                if (TryGetMemberInfo(out var memberInfo))
                {
                    memberInfo.SetValue(target, value);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
        
        public bool TryGetValue<TValue>(T target, out TValue value) where TValue : class
        {
            try
            {
                if (TryGetMemberInfo(out var memberInfo))
                {
                    value = (TValue) memberInfo.GetValue(target);
                    return true;
                }

                value = null;
                return false;
            }
            catch
            {
                value = null;
                return false;
            }
        }
        
        public bool TryGetValue(T target, out object value)
        {
            try
            {
                if (TryGetMemberInfo(out var memberInfo))
                {
                    value = memberInfo.GetValue(target);
                    return true;
                }

                value = null;
                return false;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}