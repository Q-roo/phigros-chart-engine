using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class DeclaredFunction : CBFunction {
    public BlockStatementNode body;
    public override Either<ICBValue, ErrorType> Call(params ICBValue[] args) {
        if (isLastParams) {
            if (args.Length < argumentNames.Length - 1)
                return ErrorType.InvalidArgument; // since the last argument is an array, it can be an empty one

            for (int i = 0; i < argumentNames.Length - 1; i++) {
                string parameter = argumentNames[i];
                switch (body.scope.GetVariable(parameter).Case) {
                    case ICBValue variable:
                        switch (variable.SetValue(args[i])) {
                            case ErrorType error when error is not ErrorType.NoError:
                                return error; // this is why invalid argument error exists but this might be more helpful
                        }
                        break;
                    default:
                        throw new UnreachableException(); // the analyzer should've daclared all of the parameters as nonconstant beforehand
                }
            }

            switch (body.scope.GetVariable(argumentNames[^1]).Case) {
                case ICBValue variable:
                    ArrayValue array = new();
                    foreach (ICBValue value in args[(argumentNames.Length - 2)..])
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
            if (args.Length < argumentNames.Length)
                return ErrorType.InvalidArgument;
            for (int i = 0; i < argumentNames.Length; i++) {
                string parameter = argumentNames[i];
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
                    if (value.Type != returnType)
                        return ErrorType.InvalidType;

                    return Either<ICBValue, ErrorType>.Left(value);
                case ErrorType error when error is not ErrorType.NoError:
                    return error;
            }

        return body.scope.GetVariable("unset").Case switch {
            ICBValue value => value.Type != returnType ? ErrorType.InvalidType : Either<ICBValue, ErrorType>.Left(value),
            _ => throw new UnreachableException() // the global scope should have it defined
        };
    }
}