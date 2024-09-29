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

## version

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