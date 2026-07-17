# Compiler Plants+

## Prérequis

- SDK .NET 6
- Plants vs. Zombies Fusion 3.8 avec MelonLoader 0.7.3
- Assemblies IL2CPP générées par MelonLoader
- `CustomizeLib.BepInEx.dll` provenant du port MelonLoader de CustomizeLib

## Structure attendue

Le projet trouve automatiquement les références lorsque `PVZF_GAME_DIR` pointe vers le dossier du jeu :

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
   └─ CustomizeLib.BepInEx.dll
```

## Commande de compilation

PowerShell :

```powershell
$env:PVZF_GAME_DIR = "C:\Chemin\Vers\PVZ Fusion"
dotnet build .\src\PlantsPlus\PlantsPlus.csproj -c Release
```

Le DLL est produit dans :

```text
build/PlantsPlus.dll
```

## Build de secours

Magnet-o-pea est activée par défaut. Pour l'exclure :

```powershell
dotnet build .\src\PlantsPlus\PlantsPlus.csproj -c Release -p:EnableMagnetOPea=false
```

Ne publie jamais les DLL du jeu, les assemblies IL2CPP générées ou tes dossiers de références locaux dans le dépôt.
