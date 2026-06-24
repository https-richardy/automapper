# Mapping — AutoMapper Fork (Free & .NET 8+ Compatible)

This is a fork of [AutoMapper](https://github.com/AutoMapper/AutoMapper) v14.0.0, published as the free `Mapping` NuGet package. It exists to solve two problems that hit at the same time: AutoMapper became a paid library, and projects on modern .NET runtimes started crashing with a frustrating assembly resolution error before a single line of application code could run.

## The problem

The crash looks like this:

```
An assembly specified in the application dependencies manifest (YourProject.deps.json) was not found:
  package: 'AutoMapper', version: '12.0.0'
  path: 'lib/netstandard2.1/AutoMapper.dll'
```

What's happening is that older AutoMapper packages ship only a `netstandard2.1` TFM asset. The .NET SDK is perfectly happy to reference it at build time when targeting `net8.0` or later, but the runtime's asset-resolution logic in some host configurations can't locate it — killing the test host or the app process before anything useful happens.

This fork retargets the library to `net8.0` natively, cutting out the `netstandard2.1` indirection entirely. The package ID is `Mapping` so you can drop it in without touching your object-mapping code.

## Getting started

```bash
dotnet add package Mapping
```

Or via the Package Manager Console:

```powershell
Install-Package Mapping
```

The public API is identical to AutoMapper — just replace the `AutoMapper` namespace with `Mapping` and you're done.

## License

MIT, same as the original. See [LICENSE](LICENSE) for details. Original author: [Jimmy Bogard](https://github.com/jbogard).
