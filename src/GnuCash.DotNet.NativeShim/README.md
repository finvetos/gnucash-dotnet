# GnuCash.DotNet.NativeShim

Reserved for a small C ABI shim if direct exported GnuCash functions are too awkward to consume from the bridge process.

The selected write path is the installed GnuCash native engine API. The first implementation should prefer managed P/Invoke from `GnuCash.DotNet.Bridge` because the stock Windows install exports the core business/session symbols from `libgnc-engine.dll`.

Add a native shim only when it removes meaningful interop complexity, improves testability, or creates a safer C ABI boundary over pointer-heavy GnuCash workflows. A shim must still run against the user's normal GnuCash install and must not require a forked GnuCash build.
