# Scopes

There is not much to say, just know these 4
* Scopes are where variables are stored.
* Blocks create new scopes.
* Rules applied in the parent scope are also applied in the child scope.
* a child scope can access the variables from the parent scope.

```chartbuild
// global scope
// add the print, ...etc functions to the current scope
# enable logging

print(0);

{
    // child scope
    print(1); // a child scope can access the parent scope
    # disable logging
    // from here, print is not useable
    // it won't be removed since it's not in this scope
    print(2); // error, cannot use print
}

print(3);
// the disable rule only applied to the child scope,
// it can be used again
```