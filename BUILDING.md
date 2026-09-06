# Building Sensus

Use a .NET SDK supporting `netstandard2.1`, your own Lethal Company V81 installation, a BepInEx 5 profile, and the LethalConfig DLL as a compile-time reference.

```powershell
dotnet build src/Sensus/Sensus.csproj -c Release -p:GameDir="PATH_TO_GAME" -p:TestProfile="PATH_TO_PROFILE" -p:LethalConfigDll="PATH_TO_LethalConfig.dll" -p:DeployToTestProfile=false
```

The output is `src/Sensus/bin/Release/netstandard2.1/Sensus.dll`. Install only this DLL. Publicized copies of game references remain in build intermediates and must not be distributed.

All runtime C# sources, including the editable generated binding/catalog files, are included. A normal build does not require the internal resource audit, decompiled game scripts, or code-generation tools. The lock file preserves the Publicizer dependency. LethalConfig is optional at runtime.

Audio capture targets the audited V81 assembly fingerprint in `RuntimeBaseline.cs`; other fingerprints disable capture. Supporting another game build requires a fresh compatibility review. The current implementation is Auditus; Visus is planned. Complete multiplayer and audibility validation is still ongoing.

Source is provided under GPL-3.0-only; preserve the third-party notices. Source snapshots accompanying downloadable binaries should identify the exact version/commit used to build them.
