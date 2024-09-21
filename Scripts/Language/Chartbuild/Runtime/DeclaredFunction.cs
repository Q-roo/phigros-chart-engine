using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class DeclaredFunction : CBFunction {
    public BlockStatementNode body;
    public override Either<ICBValue, ErrorType> Call(params ICBValue[] args) {
        // take the following example
        // fn make_adder(a) {
        //  return |b| {a + b;}
        // }
        // the scope needs to be reconstructed each time make_adder is called

        body.scope = body.scope.Clone();

        if (isLastParams) {
            if (args.Length < parameterNames.Length - 1) // the params is an array so it can be of size 0
                return ErrorType.InvalidArgument;

            ErrorType error;

            for (int i = 0; i < parameterNames.Length - 1; i++) {
                error = TryAssignParameter(i, args[i]);
                if (error != ErrorType.NoError)
                    return error;
            }

            ArrayValue array = new() {
                innerType = (paremeterTypes[^1] as ArrayType).type
            };

            foreach (ICBValue value in args[(parameterNames.Length - 2)..]) {
                if (!value.Type.CanBeAssignedTo(array.InnerType))
                    return ErrorType.InvalidArgument;

                switch (array.InnerType.Constructor(value).Case) {
                    case ICBValue casted:
                        array.AddMember(casted);
                        break;
                    case ErrorType err:
                        return err;
                    default:
                        throw new UnreachableException();
                }
            }

            error = TryAssignParameter(parameterNames.Length - 1, array);
            if (error != ErrorType.NoError)
                return error;
        } else {
            if (args.Length != parameterNames.Length)
                return ErrorType.InvalidArgument;

            for (int i = 0; i < args.Length; i++) {
                ErrorType error = TryAssignParameter(i, args[i]);
                if (error != ErrorType.NoError)
                    return error;
            }
        }

        foreach (StatementNode statement in body.body)
            switch (/* statement.Evaluate(body.scope) */Interpreter.Evaluate(statement, body.scope).Case) {
                case ICBValue value:
                    if (!value.Type.CanBeAssignedTo(returnType))
                        return ErrorType.InvalidType;

                    return Either<ICBValue, ErrorType>.Left(value);
                case ErrorType error when error is not ErrorType.NoError:
                    return error;
            }

        return body.scope.GetVariable("unset").Case switch {
            ICBValue value => value.Type.CanBeAssignedTo(returnType) ? Either<ICBValue, ErrorType>.Left(value) : ErrorType.InvalidType,
            _ => throw new UnreachableException() // the global scope should have it defined
        };
    }

    private ErrorType TryAssignParameter(int parameterIndex, ICBValue value) => TryAssignParameter(parameterIndex, value, body.scope);

    private ErrorType TryAssignParameter(int parameterIndex, ICBValue argument, Scope scope) {
        string name = parameterNames[parameterIndex];
        BaseType type = paremeterTypes[parameterIndex];
        if (!argument.Type.CanBeAssignedTo(type))
            return ErrorType.InvalidArgument;



        return scope.GetVariable(name).Case switch {
            CBVariable variable => type.Constructor(argument).Case switch {
                ICBValue value => variable.SetValue(value),
                ErrorType err => err,
                _ => throw new UnreachableException()
            },
            _ => throw new UnreachableException() // the variable shoud be defined by now
        };
    }
}