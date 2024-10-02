# Functions and closures

Closures are function objects. Chartbuild handles declared function just like closures.

Closures can take in any number of arguments as parameters. If the functions receives more arugemnts than there are declared when it is called, the addition parameters will be ignored.

## functions

```chartbuild
fn add(a: i32, b: i32) -> i32 {
    return a + b;
}

add(1, 2, 3, 4); // 3 and 4 will be ignored
```

But if there are less parameters than declared, the analyzer will raise an error.

```chartbuild
fn add(a: i32, b: i32) -> {
    return a + b;
}

add(1); // error: Invalid arguments
```

Closures can declare the last parameter as a vararg parameter which are basically array parameters.

```chartbuild
fn sum(...numbers: i32) -> i32 {
    let ret = 0;
    for (const n in numbers)
        ret += n;
    
    return ret;
}

// this would behave exactly as sum
fn sum2(numbers: [i32]) {
    //...
}

sum(1, 2, 3); // returns 6

fn sum_mul(by: i32, ...to_sum: i32) {
    return by * sum(to_sum);
}
```

## closures

Closures behave exactly like functions but are declared differently.
The arguments are put between `|` chcaracters.

```
const print_a = || print('a');
const print_i32 = |i: i32| print(i);
```

## differences between function and closure declarations

The main difference is that functions require the paramter and return types to be defined, unless, it returns nothing, in which case the return type doesn't need to be defined. On the other hand, when the types are known beforehand, closures can ommit such.

```
fn use_adder(a: i32, b: i32, adder: fn(i32, i32) -> i32) -> i32 {
    return adder(a, b);
}

use_adder(1, 2, |a, b| return a + b);
```