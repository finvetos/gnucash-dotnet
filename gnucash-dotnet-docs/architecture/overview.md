# Architecture Overview

GnuCash.DotNet keeps the .NET developer experience separate from the native GnuCash runtime.

```mermaid
flowchart LR
  App["Consumer .NET app"] --> Sdk["GnuCash.DotNet SDK"]
  Sdk --> Protocol["Protocol contracts"]
  Sdk --> Bridge["GnuCash.DotNet.Bridge win-x86 process"]
  Bridge --> Native["Official installed GnuCash DLLs"]
  Bridge --> Book["GnuCash book file or SQL backend"]
```

The SDK is AnyCPU-friendly. The bridge is explicitly `win-x86` so x64 applications can use the official Windows GnuCash install without loading 32-bit DLLs in-process.