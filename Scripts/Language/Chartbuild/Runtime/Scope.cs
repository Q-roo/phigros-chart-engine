using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class Scope(Scope parent) {
    public readonly Dictionary<string, ExpressionNode> initalValues = [];
    private readonly Dictionary<string, CBVariable> varialbes = [];
    public Dictionary<string, CBVariable>.ValueCollection AllVariables => varialbes.Values;
    public Scope parent = parent;

    public Scope Clone() {
        Scope clone = new(parent);

        foreach (string name in varialbes.Keys) {
            CBVariable variable = varialbes[name];
            clone.varialbes[name] = new(variable.GetValueUnsafe(), variable.@readonly, variable.constant);

            if (initalValues.TryGetValue(name, out ExpressionNode expression))
                clone.varialbes[name].SetValue(expression.Evaluate(this).Case switch {
                    ICBValue value => value,
                    _ => throw new UnreachableException() // this should be able to evaluate no matter what
                });
        }

        return clone;
    }

    public Scope()
    : this(null) { }

    public bool HasVariable(string name) {
        // if (varialbes.ContainsKey(name))
        //     return true;

        // if (parent is not null)
        //     return parent.HasVariable(name);

        // return false;

        // allow
        // let a;
        // {
        //   let a;
        // }
        return varialbes.ContainsKey(name);
    }

    public ErrorType DeclareNonConstant(string name, bool @readonly, bool initalized) {
        if (HasVariable(name))
            return ErrorType.DuplicateIdentifier;

        varialbes.Add(name, new(@readonly, initalized, false));
        return ErrorType.NoError;
    }

    public ErrorType DeclareVariable(string name, ICBValue value, bool @readonly) {
        if (HasVariable(name))
            return ErrorType.DuplicateIdentifier;

        varialbes.Add(name, new(value, @readonly));
        return ErrorType.NoError;
    }

    public void SetDefaultValue(string name, ExpressionNode expression) {
        initalValues[name] = expression;
    }

    public Either<CBVariable, ErrorType> GetVariable(string name) {
        if (!varialbes.TryGetValue(name, out CBVariable value)) {
            if (parent is not null)
                return parent.GetVariable(name);
            return ErrorType.MissingMember;
        }
        return value;
    }

    public CBVariable GetVariableUnsafe(string name) {
        if (!varialbes.TryGetValue(name, out CBVariable value)) {
            if (parent is not null)
                return parent.GetVariableUnsafe(name);
            throw new UnreachableException();
        }
        return value;
    }

    public ErrorType DeclareNativeFunction(
        string name,
        Func<ICBValue[], Either<ICBValue, ErrorType>> function,
        bool pure,
        Dictionary<string, BaseType> parameters,
        bool isLastParams,
        BaseType returnType
    ) => DeclareVariable(
        name,
        new CBFunctionValue(new NativeFunctionBinding(function) {
            pure = pure,
            parameterNames = [.. parameters.Keys],
            paremeterTypes = [.. parameters.Values],
            isLastParams = isLastParams,
            returnType = returnType
        }),
        true
    );

    public ErrorType DeclareNativeMethod(
        string name,
        Action<ICBValue[]> method,
        bool pure,
        Dictionary<string, BaseType> parameters,
        bool isLastParams
    ) => DeclareVariable(
        name,
        new CBFunctionValue(new NativeFunctionBinding(method) {
            pure = pure,
            parameterNames = [.. parameters.Keys],
            paremeterTypes = [.. parameters.Values],
            isLastParams = isLastParams,
            returnType = new NullType()
        }),
        true
    );
}