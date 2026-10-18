using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace PlantsPlus.Core
{
    // Read declaration data without executing attribute constructors. In this
    // IL2CPP host the automatic attribute-instantiation scan can abort the CLR.
    internal static class MetadataPatchBootstrap
    {
        internal sealed record PatchPlan(MethodInfo Target, HarmonyMethod? Prefix,
            HarmonyMethod? Postfix, HarmonyMethod? Finalizer, string Source);

        internal static List<PatchPlan> ReadPlan(Assembly assembly)
        {
            var result = new List<PatchPlan>();
            foreach (Type type in assembly.GetTypes())
            {
                var classData = type.GetCustomAttributesData();
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var methodData = method.GetCustomAttributesData();
                    string? kind = methodData.Select(a => a.AttributeType.FullName)
                        .FirstOrDefault(n => n == "HarmonyLib.HarmonyPrefix" ||
                            n == "HarmonyLib.HarmonyPostfix" || n == "HarmonyLib.HarmonyFinalizer");
                    // Harmony also recognises convention-named methods in a
                    // patch class, without requiring an explicit marker.
                    if (kind == null && classData.Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
                        kind = method.Name switch {
                            "Prefix" => "HarmonyLib.HarmonyPrefix",
                            "Postfix" => "HarmonyLib.HarmonyPostfix",
                            "Finalizer" => "HarmonyLib.HarmonyFinalizer",
                            _ => null
                        };
                    if (kind == null) continue;
                    Type? declaringType = null;
                    string? methodName = null;
                    Type[]? parameters = null;
                    int priority = Priority.Normal;
                    foreach (CustomAttributeData attribute in classData.Concat(methodData))
                    {
                        string? name = attribute.AttributeType.FullName;
                        if (name == "HarmonyLib.HarmonyPriority")
                            priority = (int)attribute.ConstructorArguments[0].Value!;
                        if (name != "HarmonyLib.HarmonyPatch") continue;
                        foreach (CustomAttributeTypedArgument arg in attribute.ConstructorArguments)
                        {
                            if (arg.ArgumentType == typeof(Type)) declaringType = (Type)arg.Value!;
                            else if (arg.ArgumentType == typeof(string)) methodName = (string)arg.Value!;
                            else if (arg.ArgumentType == typeof(Type[]))
                                parameters = ((IEnumerable<CustomAttributeTypedArgument>)arg.Value!)
                                    .Select(item => (Type)item.Value!).ToArray();
                            else throw new NotSupportedException("Unsupported patch declaration: " + type.FullName);
                        }
                    }
                    if (declaringType == null || methodName == null)
                        throw new InvalidOperationException("Incomplete patch declaration: " + type.FullName);
                    MethodInfo target = AccessTools.Method(declaringType, methodName, parameters) ??
                        throw new MissingMethodException(declaringType.FullName, methodName);
                    // Do not use HarmonyMethod(MethodInfo): that constructor also
                    // imports instantiated attributes from the patch method.
                    var patch = new HarmonyMethod { method = method, priority = priority };
                    int index = result.FindIndex(p => p.Target == target && p.Source == type.FullName);
                    PatchPlan plan = index < 0 ? new PatchPlan(target, null, null, null, type.FullName!) : result[index];
                    plan = kind switch
                    {
                        "HarmonyLib.HarmonyPrefix" => plan with { Prefix = patch },
                        "HarmonyLib.HarmonyPostfix" => plan with { Postfix = patch },
                        _ => plan with { Finalizer = patch }
                    };
                    if (index < 0) result.Add(plan); else result[index] = plan;
                }
            }
            return result;
        }

        internal static void Apply(Assembly assembly)
        {
            List<PatchPlan> plans = ReadPlan(assembly);
            var harmony = new HarmonyLib.Harmony("PvZFusionPlus.MetadataPatches");
            foreach (PatchPlan plan in plans)
            {
                Plugin.Logger.LogInfo("[Patch Init] " + plan.Source + " -> " +
                    plan.Target.DeclaringType!.FullName + "." + plan.Target.Name);
                harmony.Patch(plan.Target, prefix: plan.Prefix, postfix: plan.Postfix,
                    finalizer: plan.Finalizer);
            }
            Plugin.Logger.LogInfo("[Patch Init] Installed " + plans.Count + " patch groups.");
        }
    }
}
