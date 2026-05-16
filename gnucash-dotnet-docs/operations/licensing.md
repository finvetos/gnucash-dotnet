# Licensing

GnuCash.DotNet source code is licensed under Apache-2.0.

The project should not bundle GnuCash binaries, headers, source code, or modified GnuCash builds. Users install official GnuCash separately, and the bridge discovers that installation at runtime.

Important boundaries:

- GnuCash remains governed by the GnuCash project's license terms.
- This repository should keep copied upstream code out of the SDK and bridge unless the license impact is explicitly reviewed.
- Native interop changes should document whether they dynamically load official installed DLLs, compile against headers, or distribute native assets.
- Public package metadata should keep the dependency relationship clear: this project is an SDK/bridge, not a GnuCash distribution.

This is an engineering policy, not legal advice. Review the distribution model before the first public package release.