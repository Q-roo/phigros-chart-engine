# Chartbuild

Chartbuild is a custom scripting language made to store charts in a programatic way. That is because the features (planned for) this project would result in hard to read and large files if the data were to be stored in formats like json.

Chartbuild is an interpreted language.
Before a chartbuild script can run, it goes trough multiple stages.
1. tokenization
2. parsing
3. analysis

    This step is perhaps the most important. The VM does not do any checks to see if the instructions are valid. These validations are left to the analyzer which also tries to optimize the code (e.g: with constant folding).

    The analyzer is also responsible for executing commands

    <div class="warning">
        Currently, this step is ommited becuse the previous analyzer was not good enough due to my lack of programming experience.
    </div>
4. byte code emission

And after all that, it can finally run in a VM (virtual machine).

<div class="warning">
    There are plans to just walk down the AST instead of generating and interpreting byte code for multiple reasons.
    <ul>
        <li>The current implementation is an insult to byte code. While AST walking is supposed to be slower and memory hogging it would still probably be more performant than the abomination that I wrote</li>
        <li>It is less error prone. Even now, there probably are a few bugs in the VM that could've been avoided if I dodn't try to do something facny like this.</li>
    </ul>
</div>

## features

* commands (there's probably a proer name for this)
* variables
* functions and closures
* control flow
    * if, else

        <div class="warning">
            Curently, this statement has a bug (wrong jump location. Avoid using it until it is fixed)
        </div>
    * loops