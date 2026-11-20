# Limitations

What the current version of the library cannot do.

- **`Result<Exception>`** declares fine, but implicit conversion from an exception fails with CS0457 (ambiguous user-defined conversions), because both cases take an `Exception`. Wrap the payload in another type.
- **No `null` literal for a result.** A `default` result is an error; return a value or an exception instead.
- **`default` is not caught by `switch`.** A `switch` over a `default` result throws `SwitchExpressionException`, because the compiler cannot see that state. Operators and `Match` treat it as an error.
- **Equality is not payload-based.** Two results wrapping distinct class instances are not equal.
- **AOT and trimming are not verified.** The JSON converters use reflection.
- **Hand-written class unions** were not investigated. The library is built on C# `union` declarations.
- **Entity Framework Core** cannot map `Option<T>` with a value converter. See [Entity Framework Core](../entity-framework-core/README.md) for the backing-field pattern.

---

Back to: [Getting started](README.md) · Next: [Result](../result/README.md)
