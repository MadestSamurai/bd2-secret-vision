# 跨版本适配 / Client compatibility

组件源码随工具内嵌，连接时用 Mono.Cecil 读取本机元数据，以类型结构、规范化方法体哈希和成员使用位置匹配接口，再由 Roslyn 编译。发行物不携带游戏程序集、游戏表或反编译源文件。

The tool embeds its own component source. At connection time, Mono.Cecil resolves local metadata using type shapes, normalized method hashes and member-use sites; Roslyn then compiles against the installed client. No game assemblies, tables or decompiled game source are shipped.

当前契约包含15类、57个必要成员。程序集MVID仅用于诊断，既不是固定版本白名单，也不是兼容证明。签名变化、缺失或歧义会阻止连接。编译通过只证明接口可绑定，不证明新版本玩法语义未变。

The current contract covers 15 types and 57 required members. MVID is diagnostic metadata, not a version whitelist or proof of behavior. Signature changes, missing members and ambiguous matches block connection. Compilation verifies bindings, not unchanged game semantics.

维护者可对本机 Managed 目录执行只读检查：
Maintainers can run a read-only check against an installed Managed directory:

```powershell
dotnet run --project compatibility-cli -c Release -- check "<game>\BrownDust II_Data\Managed"
```

只有审阅新的接口语义后才重新生成契约。生成器不用于在用户端绕过失败：
Regenerate the contract only after reviewing changed semantics. The generator is not an end-user bypass:

```powershell
dotnet run --project compatibility-cli -c Release -- contract "<game>\BrownDust II_Data\Managed" "<repository>"
```

已验证 / Checked: current installed client compiles; synthetic symbol renaming, member reordering and unrelated extensions resolve; missing / changed / ambiguous members fail closed. The standalone runtime also completed one natural stage-1 clear at 100% with server confirmation. Other stages and future-client gameplay semantics are not covered by that result.

The embedded hook sources and contract retain their exact line endings through `.gitattributes`. Their bytes contribute to component identity; a checkout must not silently change that identity for an otherwise unchanged component.
