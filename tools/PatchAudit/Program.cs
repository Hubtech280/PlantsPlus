using System.Collections;
using System.Reflection;
using System.Runtime.Loader;

// Metadata/target resolution only. No mod initialization, IL2CPP registration,
// native method call or Harmony patch is performed by this audit.
string modPath = Path.GetFullPath(args[0]);
string gamePath = Path.GetFullPath(args[1]);
string[] dirs = { Path.GetDirectoryName(modPath)!, Path.Combine(gamePath, "Mods"),
    Path.Combine(gamePath, "MelonLoader", "net6"),
    Path.Combine(gamePath, "MelonLoader", "Il2CppAssemblies") };
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    foreach (string dir in dirs)
    {
        string path = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
    }
    return null;
};
Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(modPath);
Type? supportedFusion = assembly.GetType("PlantsPlus.Core.NightRoofSupportedFusion");
if (supportedFusion != null)
{
    MethodInfo resultMethod = supportedFusion.GetMethod("Result", BindingFlags.NonPublic | BindingFlags.Static)!;
    MethodInfo roofScene = supportedFusion.GetMethod("IsRoofScene", BindingFlags.NonPublic | BindingFlags.Static)!;
    for (int scene = -1; scene < 100; scene++)
        if ((bool)roofScene.Invoke(null, new object[] { scene })! != (scene == 4 || scene == 5))
            throw new Exception("Unexpected roof scene scope.");
    Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[3], "Assembly-CSharp.dll"));
    Type plantType = game.GetType("Il2Cpp.PlantType", true)!;
    int sun = (int)plantType.GetField("SunFlower")!.GetRawConstantValue()!;
    int electric = (int)plantType.GetField("ElectricOnion")!.GetRawConstantValue()!;
    foreach (var pair in new[] { (sun, 6041), (electric, 6037), (6024, 6039) })
        foreach (var order in new[] { (6035, pair.Item1), (pair.Item1, 6035) })
            if ((int)resultMethod.Invoke(null, new object[] { order.Item1, order.Item2 })! != pair.Item2)
                throw new Exception("Supported fusion order mismatch.");
    foreach (var order in new[] { (27, sun), (sun, 27), (27, 6035), (6035, 27), (1184, sun), (6032, 6035) })
        if ((int)resultMethod.Invoke(null, new object[] { order.Item1, order.Item2 })! != -1)
            throw new Exception("Support pot/unrelated pair intercepted.");
    Console.WriteLine("PASS: supported fusion mapping works in both orders; pots and four-parent recipe excluded.");
}
Type? roofSupport = assembly.GetType("PlantsPlus.Core.NightRoofBounceSupport");
if (roofSupport != null && roofSupport.GetMethod("IsFamily", BindingFlags.NonPublic | BindingFlags.Static) is MethodInfo family)
{
    int[] expected = { 6035, 6036, 6037, 6039, 6041 };
    for (int id = -1; id <= 7000; id++)
        if ((bool)family.Invoke(null, new object[] { id })! != expected.Contains(id))
            throw new Exception("Unexpected Night Roof support policy for ID " + id);
    Console.WriteLine("PASS: Night Roof support allowlist covers exactly Bounce and its four fusions.");
}
if (args.Length > 2 && (args[2] == "--jit-fusion" || args[2] == "--jit-roof"))
{
    // Install one harmless managed-only fixture hook to activate the same
    // MonoMod JIT hook, without patching or invoking any native game method.
    Assembly harmonyAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[2], "0Harmony.dll"));
    Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true)!;
    Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", true)!;
    object harmony = Activator.CreateInstance(harmonyType, "PlantsPlus.OfflineFusionJitFixture")!;
    object prefix = Activator.CreateInstance(harmonyMethodType,
        new object[] { typeof(FusionJitFixture).GetMethod("Prefix")! })!;
    MethodInfo patch = harmonyType.GetMethods().Where(m => m.Name == "Patch" &&
        m.GetParameters()[0].ParameterType == typeof(MethodBase) &&
        m.GetParameters().Length > 1 && m.GetParameters()[1].ParameterType == harmonyMethodType)
        .OrderBy(m => m.GetParameters().Length).First();
    object?[] patchArgs = new object?[patch.GetParameters().Length];
    patchArgs[0] = typeof(FusionJitFixture).GetMethod("Target")!;
    patchArgs[1] = prefix;
    patch.Invoke(harmony, patchArgs);
    Console.WriteLine("Managed fixture detour active; no game patches installed.");
    bool roof = args[2] == "--jit-roof";
    Type fusion = assembly.GetType(args.Length > 3 ? args[3] : roof ? "PlantsPlus.Core.RoofFusionWork" : "PlantsPlus.Plants.BumpingFusionWork", true)!;
    foreach (string name in roof ? new[] { "Tick", "Observe", "Queue" } : new[] { "RemoveIngredients", "CreateResult" })
    {
        MethodInfo method = fusion.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
        Console.WriteLine("Preparing managed JIT (not invoking): " + method);
        System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
    }
    Type? removal = assembly.GetType("PlantsPlus.Plants.BumpingIngredientRemoval");
    if (removal != null)
    {
        MethodInfo bridge = removal.GetMethod("ByMix", BindingFlags.NonPublic | BindingFlags.Static)!;
        System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(bridge.MethodHandle);
        Console.WriteLine("PASS: native ByMix bridge JIT-compiled (not invoked).");
    }
    Console.WriteLine("PASS: fusion helper bodies JIT-compiled without invoking game methods.");
    return;
}
Type? bumpingRunner = assembly.GetType("PlantsPlus.Plants.BumpingFusionRunner");
Type? nativeRemoval = assembly.GetType("PlantsPlus.Plants.BumpingIngredientRemoval");
if (nativeRemoval != null)
{
    Assembly gameMetadata = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[3], "Assembly-CSharp.dll"));
    Type plantMetadata = gameMetadata.GetType("Il2Cpp.Plant", true)!;
    string fieldName = (string)nativeRemoval.GetField("DieField", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
    FieldInfo field = plantMetadata.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
    MethodInfo die = plantMetadata.GetMethod("Die")!;
    if (field == null || field.FieldType != typeof(IntPtr) || !die.IsVirtual || die.ReturnType != typeof(void) ||
        die.GetParameters().Length != 1 || Enum.GetUnderlyingType(die.GetParameters()[0].ParameterType) != typeof(int))
        throw new Exception("Native Plant.Die ABI does not match the removal bridge.");
    Console.WriteLine("PASS: native ByMix field and virtual int-enum/void signature match installed metadata (no pointers read).");
}
if (bumpingRunner != null)
{
    foreach (string helper in new[] { "TryQueue", "Queue" })
    {
        MethodInfo method = bumpingRunner.GetMethod(helper, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        if (!method.GetCustomAttributesData().Any(a =>
            a.AttributeType.FullName == "Il2CppInterop.Runtime.Attributes.HideFromIl2CppAttribute"))
            throw new Exception("Managed fusion helper exposed to IL2CPP: " + helper);
    }
    MethodInfo update = bumpingRunner.GetMethod("Update")!;
    if (update.GetCustomAttributesData().Any(a =>
        a.AttributeType.FullName == "Il2CppInterop.Runtime.Attributes.HideFromIl2CppAttribute"))
        throw new Exception("Fusion runner Update must remain a native lifecycle callback.");
    Type work = assembly.GetType("PlantsPlus.Plants.BumpingFusionWork", true)!;
    if (work.BaseType != typeof(object) || bumpingRunner.GetNestedTypes(BindingFlags.NonPublic).Length != 0 ||
        bumpingRunner.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length != 0)
        throw new Exception("Injected runner still owns fusion state.");
    foreach (string helper in new[] { "RemoveIngredients", "CreateResult" })
    {
        if (bumpingRunner.GetMethod(helper, BindingFlags.NonPublic | BindingFlags.Static) != null)
            throw new Exception("Fusion helper remains on injected type.");
        MethodInfo method = work.GetMethod(helper, BindingFlags.NonPublic | BindingFlags.Static)!;
        if (!method.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(int), typeof(int) }))
            throw new Exception("Fusion helper has a non-primitive signature.");
    }
    Console.WriteLine("PASS: injected runner has no fusion state/helpers; managed service uses primitive coordinates.");
}
if (args.Length > 2)
{
    Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[3], "Assembly-CSharp.dll"));
    if (args[2] == "--types")
    {
        foreach (Type type in game.GetTypes())
            if (System.Text.RegularExpressions.Regex.IsMatch(type.FullName!, args[3])) Console.WriteLine(type.FullName);
        return;
    }
    if (args[2] == "--methods")
    {
        foreach (Type type in game.GetTypes())
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (System.Text.RegularExpressions.Regex.IsMatch(method.Name, args[3]))
                    Console.WriteLine(type.FullName + ": " + method);
        return;
    }
    foreach (string typeName in args.Skip(2))
    {
        Type type = game.GetType("Il2Cpp." + typeName, false) ?? game.GetType(typeName, true)!;
        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            Console.WriteLine(typeName + ": " + member);
    }
    return;
}
Type bootstrap = assembly.GetType("PlantsPlus.Core.MetadataPatchBootstrap", true)!;
Type? nightSurvival = assembly.GetType("PlantsPlus.Core.NightRoofSurvival");
if (nightSurvival != null)
{
    Type nativeSurvival = AssemblyLoadContext.Default.LoadFromAssemblyPath(
        Path.Combine(dirs[3], "Assembly-CSharp.dll")).GetType("Il2Cpp.SurvivalLevel", true)!;
    foreach (string field in new[] { "Normal", "Hard", "Endless" })
    {
        int id = (int)nightSurvival.GetField(field, BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
        if (Enum.IsDefined(nativeSurvival, id)) throw new Exception("Night Roof survival ID collision: " + id);
    }
    Console.WriteLine("PASS: Night Roof survival IDs do not replace native survival IDs.");
}
Type? bamboom = assembly.GetType("PlantsPlus.Plants.Bamboom");
Type? solstice = assembly.GetType("PlantsPlus.Plants.SolsticeShroom");
if (solstice != null)
{
    int id = (int)solstice.GetField("ID")!.GetRawConstantValue()!;
    Type nativePlants = AssemblyLoadContext.Default.LoadFromAssemblyPath(
        Path.Combine(dirs[3], "Assembly-CSharp.dll")).GetType("Il2Cpp.PlantType", true)!;
    if (Enum.IsDefined(nativePlants, id)) throw new Exception("Solstice-shroom ID collision: " + id);
    Console.WriteLine("PASS: Solstice-shroom ID " + id + " is not a native plant ID.");
}
if (bamboom != null)
{
    int id = (int)bamboom.GetField("ID")!.GetRawConstantValue()!;
    Type nativePlants = AssemblyLoadContext.Default.LoadFromAssemblyPath(
        Path.Combine(dirs[3], "Assembly-CSharp.dll")).GetType("Il2Cpp.PlantType", true)!;
    if (Enum.IsDefined(nativePlants, id)) throw new Exception("Bamboom ID collides with native plant: " + id);
    Console.WriteLine("PASS: Bamboom ID " + id + " is not a native plant ID.");
}
var plans = (IEnumerable)bootstrap.GetMethod("ReadPlan", BindingFlags.Static | BindingFlags.NonPublic)!
    .Invoke(null, new object[] { assembly })!;
int groups = 0, installed = 0;
foreach (object plan in plans)
{
    groups++;
    foreach (string slot in new[] { "Prefix", "Postfix", "Finalizer" })
        if (plan.GetType().GetProperty(slot)!.GetValue(plan) != null) installed++;
}
int declared = 0;
foreach (Type type in assembly.GetTypes())
    foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
        BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        if (method.GetCustomAttributesData().Any(a => a.AttributeType.FullName is
            "HarmonyLib.HarmonyPrefix" or "HarmonyLib.HarmonyPostfix" or "HarmonyLib.HarmonyFinalizer") ||
            (type.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch") &&
             method.Name is "Prefix" or "Postfix" or "Finalizer")) declared++;
if (installed != declared || groups == 0)
    throw new Exception($"Patch count mismatch: {installed} planned, {declared} declared.");
Console.WriteLine($"PASS: {declared} patch methods in {groups} groups; all targets resolved, no patches applied.");

public static class FusionJitFixture
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static int Target() => 1;
    public static bool Prefix() => true;
}
