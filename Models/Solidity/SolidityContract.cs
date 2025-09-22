// Representa o contrato inteligente como um todo.
//
//

using System.Collections.Generic;

namespace SolidityRDP.Models.Solidity
{
    public class SolidityContract
    {
        public string Name { get; set; }
        public List<GlobalVariable> GlobalVariables { get; set; } = new();
        public List<FunctionDefinition> Functions { get; set; } = new();
        public FunctionDefinition Constructor { get; set; }
    }

    // Representa uma variável de estado, um array ou um mapeamento.
    public class GlobalVariable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string InitialValue { get; set; }
        public string IndexType { get; set; } // Usado para mapeamentos e arrays.
        public string Visibility { get; set; }
    }

    // Representa a definição de uma função.
    public class FunctionDefinition
    {
        public string Name { get; set; } // "constructor" para construtores.
        public List<Parameter> Parameters { get; set; } = new();
        public List<Parameter> ReturnParameters { get; set; } = new();
        public string Visibility { get; set; }
        public List<IStatement> Body { get; set; } = new(); // O corpo contém operações, chamadas, condicionais.
    }

    public class Parameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    // Interface para unificar os diferentes tipos de declarações dentro de uma função.
    public interface IStatement { }

    public class OperationStatement : IStatement
    {
        public string TargetVariable { get; set; }
        public string Expression { get; set; }
        public List<string> SourceVariables { get; set; } = new();
    }

    public class FunctionCallStatement : IStatement
    {
        public string FunctionName { get; set; }
        public List<string> Arguments { get; set; } = new();
        public int LineNumber { get; set; }
    }

    public class ConditionalStatement : IStatement
    {
        public string Type { get; set; } // "if", "require"
        public string Condition { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
    }
}
