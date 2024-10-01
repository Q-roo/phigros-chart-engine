# Commands

commands in chartbuild are executed by the analyzer during analysis.

<div class="warning">
Currently, no commands are implemented.
</div>

Currently, the available commands are

* version
* target
* enable
* disable
* meta

Apart from `enable` and `disable`, all of the commands can only be used once.

## version

Syntax
```chartbuild
#version [version number]
```

The language will receive new features and bugfixes. To ensure consistent behaviour accross versions, Each patch will be in a new version. Each bug could be re-enabled with an enable command.

The result would be the ability to update scripts without changing the behaviour.
and the following example could be done
```chartbuild title="buggy script"
#version x.x

function(true, 1) // old api
```

```chartbuild title="patched"
#version x.y
#enable x.x/bugs/bugn

function_updated_and_renamed(1, true) // new api
```

The script could be updates as easily as if it was just being formatted.

<div class="warning">
    Because of this, chartbuild scripts are required to start by specifying a version.
</div>

<div class="warning">
    Currently, there are no versions and defining this or not won't change anything.
</div>

## target

Syntax
```chartbuild
#target [CompatibilityLevel]

```

Chartbuild has a lot of features that is incompatible with other formats and yet, the editor allows exporting to those formats as well. That is because it will ignore all incompatibilities and just carries on. By setting the target, you are setting the format you wish the script to be fully compatible with. Which results in additional errors when incompatible features are used.

Possible values are the members of the `CompatibilityLevel` enum.

|name|description|value|
|-|-|-|
|PCE|The compatiblity level for phigros chart engine|0|
|RPE|The compatibility level for Re:PhiEdit|1|
|PHI|The compatibility level for Phigros|2|

default value: `PCE`

Under the hood, setting target just enables and disables certain features; Just think of it as a macro.

## enable & disable

Syntax
```chartbuild
#enable [feature]
#disable [group]/[nested group]/[feature]
```

Enable or disable features.

Available feautres that can be enabled or disabled

|name|full path|description|default|
|-|-|-|-|
|logging|editor/log|Gives access to the `log`, `print`, `info`, `warn`, `error` functions inside the editor.|enabled|
|custom events|events|Custom scripted events used in chartbuild charts.|enabled|
|value interpolate events|compatibility/events|The events used in Phigros and RPE which can only interpolate a property between 2 values.|disabled|

<div class="warning">
Enable and disable only adds or removes values or applies rules to the current scope.

```chartbuild
// global scope
{
    // a child scope of the global scope
    #enable logging
    print("it works"); // no issues
    // a child scope of the child scope
    {
        print("it also works here");
    }
}

// back in the global scope
print("this does not work");
```

</div>

## meta

Syntax
```chartbuild
#meta name=value
```

|name|description|value type|default value|
|-|-|-|-|
|charter|The creator of the chart.|str|Unknown|
|composer|The composer of the music used in the chart.|str|Unknown|
|artist|The background artist for the background used in the chart.|Unknown|str|
|preview|The song preview in the select menu.|range|0..=length|
|title|The title of the chart (in most cases, the song).|str|Unknown|
|name|The name of the difficulty.|str|FM|
|level|The level of the difficulty.|str|?|