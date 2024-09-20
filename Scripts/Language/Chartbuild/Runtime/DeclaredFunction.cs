using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class DeclaredFunction : CBFunction {
    public BlockStatementNode body;
    public override Either<ICBValue, ErrorType> Call(params ICBValue[] args) {
        if (isLastParams) {
            if (args.Length < parameterNames.Length - 1)
                return ErrorType.InvalidArgument; // since the last argument is an array, it can be an empty one

            for (int i = 0; i < parameterNames.Length - 1; i++) {
                string parameter = parameterNames[i];
                BaseType type = paremeterTypes[i];
                ICBValue arg = args[i];

                switch (body.scope.GetVariable(parameter).Case) {
                    case ICBValue variable:
                        if (!arg.Type.CanBeAssignedTo(type))
                            return ErrorType.InvalidArgument;
                        switch (type.Constructor(arg).Case) {
                            case ICBValue value:
                                variable.SetValue(value);
                                break;
                            case ErrorType err:
                                return err;
                            default:
                                throw new UnreachableException();
                        }
                        break;
                    default:
                        throw new UnreachableException(); // the analyzer should've daclared all of the parameters as nonconstant beforehand
                }
            }

            switch (body.scope.GetVariable(parameterNames[^1]).Case) {
                case ICBValue variable:
                    ArrayValue array = new();
                    foreach (ICBValue value in args[(parameterNames.Length - 2)..])
                        switch (array.AddMember(value)) {
                            case ErrorType error when error is not ErrorType.NoError:
                                return error;
                        } // TODO: hope I didn't mess up the indexing
                    switch (variable.SetValue(array)) {
                        case ErrorType error when error is not ErrorType.NoError:
                            return error; // this is why invalid argument error exists but this might be more helpful
                    }
                    break;
                default:
                    throw new UnreachableException(); // the analyzer should've daclared all of the parameters as nonconstant beforehand
            }


        } else {
            if (args.Length < parameterNames.Length)
                return ErrorType.InvalidArgument;
            for (int i = 0; i < parameterNames.Length; i++) {
                string parameter = parameterNames[i];
                switch (body.scope.GetVariable(parameter).Case) {
                    case CBVariable variable:
                        switch (variable.SetValue(args[i])) {
                            case ErrorType error when error is not ErrorType.NoError:
                                return error; // this is why invalid argument error exists but this might be more helpful
                        }
                        break;
                    default:
                        throw new UnreachableException(); // the analyzer should've daclared all of the parameters as nonconstant beforehand
                }
            }
        }


        foreach (StatementNode statement in body.body)
            switch (statement.Evaluate(body.scope).Case) {
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
}