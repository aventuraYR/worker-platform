# Politica de Versionado y Publicacion

Este repositorio usa una politica de versionado lockstep para la familia de paquetes `WorkerPlatform.Core`.

## Alcance

Los siguientes paquetes se versionan y publican juntos:

- WorkerPlatform.Core.Configuration
- WorkerPlatform.Core.Logging
- WorkerPlatform.Core.Resilience
- WorkerPlatform.Core.Health
- WorkerPlatform.Core.Hosting
- WorkerPlatform.Core (agregador)

## Regla de Versionado (Lockstep)

1. Todos los paquetes listados arriba deben compartir siempre la misma version.
2. Nunca publicar solo `WorkerPlatform.Core`.
3. Los consumidores deben actualizar solo `WorkerPlatform.Core` en `Directory.Packages.props`.
4. Los paquetes modulares internos son dependencias transitivas del agregador y deben existir en el feed.

## Flujo de Publicacion

1. Elegir la siguiente version (SemVer): `MAJOR.MINOR.PATCH`.
2. Compilar y empaquetar todos los paquetes usando el script de publicacion:

```powershell
.\scripts\Publish-Core.ps1 -Version 1.0.1
```

3. Confirmar que todos los archivos `.nupkg` existen en el feed interno (`\\YRMXNT002\NuGet`).
4. Actualizar la version consumida en `Directory.Packages.props`:

```xml
<PackageVersion Include="WorkerPlatform.Core" Version="1.0.1" />
```

5. Limpiar cache local de NuGet y restaurar:

```powershell
dotnet nuget locals all --clear
dotnet restore .\WorkerPlatform.slnx --no-cache
```

## Checklist de Release

- [ ] `dotnet build WorkerPlatform.slnx -c Release` exitoso.
- [ ] `Publish-Core.ps1` empaqueta todos los modulos + agregador.
- [ ] El feed contiene los 6 paquetes con la misma version.
- [ ] La restauracion del consumidor funciona sin `NU1101`.
- [ ] El cambio de version en `Directory.Packages.props` quedo en commit.

## Solucion de Problemas

### Error: `NU1101` para modulos `WorkerPlatform.Core.*`

Causa: se publico `WorkerPlatform.Core`, pero uno o mas paquetes modulares dependientes no se publicaron en el feed interno.

Solucion:

1. Publicar todos los paquetes modulares y el agregador con exactamente la misma version.
2. Limpiar cache de NuGet (`dotnet nuget locals all --clear`).
3. Restaurar de nuevo con `--no-cache`.

### Error: el paquete existe pero restore sigue fallando

- Verificar que el origen del feed este habilitado: `dotnet nuget list source`.
- Verificar que los archivos del paquete existan fisicamente en `\\YRMXNT002\NuGet`.
- Confirmar consistencia de version entre los 6 paquetes.

## Guia SemVer

- PATCH: correcciones de bugs/mejoras internas, sin cambios publicos incompatibles.
- MINOR: nuevas capacidades compatibles hacia atras.
- MAJOR: cambios incompatibles en la API de cualquier modulo.

Como los modulos estan en lockstep, un cambio breaking en un modulo requiere incrementar MAJOR para toda la familia de paquetes.
