# Variables

Syntax

```chartbuild
[let | const] name (:type) (?=value);
```

Variable definitions start with either the `let` or `const` keyword.

## let

The value of the variable can be changed.

## const

The value of the variable can be set only once.

whic is why this is also valid

```chartbuild
const a: i32;
// since const restircts the value to be settable only once
// declaring a const variable without a value is valid.
```

```chartbuild
const a = 0;
a = 5; // not allowed to set a value of a constant nore than once
```

## type annotations for variables

Type annotations are optional as long as there is a value assigned during initalization.

```chartbuild
let a = 0; // ok, a is i32 (infered)
let b: i32; // ok, b is also known
let c; // error, unknown type
```

It is possible to assign a value of a different type as long as it can be coerced into it.

```chartbuild
const a: f32 = 0; // i32 can be coerced into f32
```

## uninitalized variables

While a variable is not assigned a value, it cannot be used.

```chartbuild
let a;
print(a); // error, a is uninitalized
a = 0;
print(a); // ok
```