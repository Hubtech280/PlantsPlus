using System;
using System.Reflection;
using Il2Cpp;
using Il2CppInterop.Runtime;

namespace PlantsPlus.Plants
{
    // Invoke the original virtual IL2CPP method through its metadata rather
    // than JIT-compiling the generated managed Plant.Die call site in fusion.
    // This still runs native ByMix cleanup/events; it is not Object.Destroy.
    internal static class BumpingIngredientRemoval
    {
        internal const string DieField = "NativeMethodInfoPtr_Die_Public_Virtual_New_Void_DieReason_0";
        private static IntPtr dieMethod;

        internal static void Validate()
        {
            if (dieMethod != IntPtr.Zero) return;
            FieldInfo field = typeof(Plant).GetField(DieField,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new MissingFieldException(typeof(Plant).FullName, DieField);
            dieMethod = (IntPtr)field.GetValue(null)!;
            if (dieMethod == IntPtr.Zero) throw new InvalidOperationException("Plant.Die metadata unavailable.");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static unsafe void ByMix(Plant plant)
        {
            Validate();
            IntPtr instance = IL2CPP.Il2CppObjectBaseToPtrNotNull(plant);
            IntPtr method = IL2CPP.il2cpp_object_get_virtual_method(instance, dieMethod);
            if (method == IntPtr.Zero) throw new InvalidOperationException("Plant.Die virtual method unavailable.");
            int reason = (int)Plant.DieReason.ByMix;
            void** arguments = stackalloc void*[1];
            arguments[0] = &reason;
            IntPtr exception = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(method, instance, arguments, ref exception);
            Il2CppException.RaiseExceptionIfNecessary(exception);
        }
    }
}
