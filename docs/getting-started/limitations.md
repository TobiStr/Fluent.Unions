# Limitations

What the current version of the library cannot do.

- **`Result<Exception>`** declares fine, but implicit conversion from an exception fails with CS0457 (ambiguous user-defined conversions), because both cases take an `Exception`. Analyzer error [FLU0004](../analyzer/README.md#flu0004) reports the type. Wrap the payload in another type.
- **No `null` literal for a result.** A `default` result is an error; return a value or an exception instead.
- **`default` is not caught by `switch`.** A `switch` expression over a `default` result throws `SwitchExpressionException` unless it has a `null` arm, and a `switch` statement runs no branch, because the compiler cannot see that state. Operators and `Match` treat it as an error. See [The default result](../result/core-concepts.md#the-default-result).
- **No `==` on results.** `Result`, `Result<T>` and your own result types define no equality operators, so `a == b` does not compile (CS0019). `Equals` is the default struct equality: it compares the stored value or error with their own `Equals`. Two results with equal records are equal, two results with distinct class instances are not, and two errors are equal only when they hold the same exception instance.
- **AOT and trimming need explicit converters.** The core package is tested in native AOT and trimmed apps; `Option<T>` of a value type and your own unions need the generic converters there, and several integration packages are not supported. See [AOT and trimming](aot-and-trimming.md).
- **Hand-written class unions** were not investigated. The library is built on C# `union` declarations.
- **Entity Framework Core** cannot map `Option<T>` with a value converter. See [Entity Framework Core](../entity-framework-core/README.md) for the backing-field pattern.

---

Back to: [Getting started](README.md) · Next: [Result](../result/README.md)
