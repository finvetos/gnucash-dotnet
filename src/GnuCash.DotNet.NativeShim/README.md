# GnuCash.DotNet.NativeShim

Reserved for a small C ABI shim if direct exported GnuCash functions are too awkward to consume from the bridge process.

The first implementation should prefer managed P/Invoke from `GnuCash.DotNet.Bridge`. Add a native shim only when it removes meaningful interop complexity or improves testability.