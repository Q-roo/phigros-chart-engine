namespace PCE.Chartbuild.Runtime;


// as it turns out, scopes have to be reconstructed for nested functions
// and keeping track of them would be a pain
// this will walk down the ast and won't remove variable and function declarations
// but it will do the other optimizations thought
// it will also reconstruct scopes regularly
// use this to alalyze and optimize the ast
// after that, the byte code generator can remove the variable and function declarations
public class Interpreter(ASTRoot ast) {
    ASTRoot ast = ast;
}