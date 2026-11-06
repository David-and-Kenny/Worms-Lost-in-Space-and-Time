using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Checks serialized callbacks that ordinary C# compilation cannot validate.</summary>
internal static class SerializedBindingValidator
{
    public static int Validate(GameObject root, string assetPath)
    {
        int errors = 0;
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                Debug.LogError($"{assetPath}: missing script beneath {root.name}");
                errors++;
                continue;
            }
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.name != "m_MethodName" || property.propertyType != SerializedPropertyType.String
                    || !property.propertyPath.Contains(".m_PersistentCalls.m_Calls.")
                    || string.IsNullOrEmpty(property.stringValue)) continue;

                string callPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'));
                var target = serialized.FindProperty(callPath + ".m_Target")?.objectReferenceValue;
                if (target != null && HasMethod(target.GetType(), property.stringValue)) continue;
                Debug.LogError($"{assetPath}: invalid UnityEvent {component.name}.{property.propertyPath} -> {property.stringValue}");
                errors++;
            }
        }

        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator.runtimeAnimatorController == null) continue;
            foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
            foreach (var animationEvent in AnimationUtility.GetAnimationEvents(clip))
            {
                if (string.IsNullOrEmpty(animationEvent.functionName)) continue;
                bool receiverExists = animator.GetComponents<MonoBehaviour>()
                    .Any(receiver => receiver != null && HasMethod(receiver.GetType(), animationEvent.functionName));
                if (receiverExists) continue;
                Debug.LogError($"{assetPath}: animation {clip.name} has no receiver for {animationEvent.functionName} on {animator.name}");
                errors++;
            }
        }
        return errors;
    }

    private static bool HasMethod(Type type, string name)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Any(method => method.Name == name)) return true;
        }
        return false;
    }
}
