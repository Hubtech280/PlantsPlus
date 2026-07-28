# Building Plants+

## Requirements

- .NET 6 SDK
- Plants vs. Zombies Fusion 3.8.1 with MelonLoader 0.7.3 installed
- MelonLoader-generated IL2CPP assemblies
- `CustomizeLib.MelonLoader.dll` 3.8.1-ml.1

## Expected game layout

The project can resolve references automatically when `PVZF_GAME_DIR` points to the game folder:

```text
<PVZF_GAME_DIR>/
├─ MelonLoader/
│  ├─ net6/
│  │  ├─ MelonLoader.dll
│  │  ├─ 0Harmony.dll
│  │  └─ Il2CppInterop.Runtime.dll
│  └─ Il2CppAssemblies/
│     ├─ Assembly-CSharp.dll
│     ├─ Il2Cppmscorlib.dll
│     ├─ Il2CppSystem.dll
│     └─ UnityEngine.*.dll
└─ Mods/
   └─ CustomizeLib.MelonLoader.dll
```

## Build command

PowerShell:

```powershell
$env:PVZF_GAME_DIR = "C:\Path\To\PVZ Fusion"
dotnet build .\src\PlantsPlus\PlantsPlus.csproj -c Release
```

Command Prompt:

```bat
set "PVZF_GAME_DIR=C:\Path\To\PVZ Fusion"
dotnet build .\src\PlantsPlus\PlantsPlus.csproj -c Release
```

The output is written to:

```text
src/build/PlantsPlus.dll
```

## Explicit reference properties

Instead of `PVZF_GAME_DIR`, the paths can be supplied directly:

```powershell
dotnet build .\src\PlantsPlus\PlantsPlus.csproj -c Release `
  -p:ReferenceRoot="C:\Path\To\PVZ Fusion\MelonLoader" `
  -p:CustomizeLibPath="C:\Path\To\PVZ Fusion\Mods\CustomizeLib.MelonLoader.dll"
```

## Optional fallback build

Magnet-o-pea is enabled by default. To exclude it:

```powershell
dotnet build .\src\PlantsPlus\PlantsPlus.csproj -c Release -p:EnableMagnetOPea=false
```

Do not commit game DLLs, generated IL2CPP assemblies or local reference folders.
