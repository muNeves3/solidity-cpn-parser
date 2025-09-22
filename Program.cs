using Antlr4.Runtime;
using SolidityRDP.Models.Solidity;
using SolidityRDP.Parsing;
using SolidityRDP.Serialization;
using SolidityRDP.Transformation;
using System;
using System.IO;

namespace SolidityRDP
{
    /// <summary>
    /// Ponto de entrada principal da aplicação.
    /// Orquestra o pipeline de leitura, análise, transformação e serialização.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Análise de Argumentos da Linha de Comando
            string inputFile = null;
            string outputFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--input" && i + 1 < args.Length)
                {
                    inputFile = args[++i];
                }
                else if (args[i] == "--output" && i + 1 < args.Length)
                {
                    outputFile = args[++i];
                }
            }

            if (string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(outputFile))
            {
                Console.WriteLine("Uso: dotnet run -- --input <caminho_do_contrato.sol> --output <caminho_do_arquivo.xml>");
                return;
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Erro: O arquivo de entrada não foi encontrado em '{inputFile}'");
                return;
            }

            try
            {
                // 2. Leitura e Análise Sintática (Parsing) com ANTLR
                Console.WriteLine($"Lendo o contrato de: {inputFile}");
                var fileContents = File.ReadAllText(inputFile);
                var inputStream = new AntlrInputStream(fileContents);
                var lexer = new SolidityLexer(inputStream);
                var commonTokenStream = new CommonTokenStream(lexer);
                var parser = new SolidityParser(commonTokenStream);
                var context = parser.sourceUnit(); // 'sourceUnit' é a regra inicial da gramática Solidity

                // 3. Construção do Modelo de Objeto do Solidity usando o Visitor
                Console.WriteLine("Construindo modelo de objeto do contrato...");
                var visitor = new SolidityVisitor();
                var solidityContract = (SolidityContract)visitor.Visit(context);
                Console.WriteLine($"Contrato '{solidityContract.Name}' analisado com sucesso.");
                Console.WriteLine(solidityContract);


                string tree = context.ToStringTree(parser);

                // Imprime a árvore completa no console.
                Console.WriteLine("--- Árvore de Análise Sintática (Parse Tree) ---");
                Console.WriteLine(tree);
                Console.WriteLine("---------------------------------------------");

                // 4. Transformação para o Modelo de Rede de Petri Colorida
                Console.WriteLine("Iniciando a transformação para Rede de Petri Colorida...");
                var transformer = new Transformer();
                var petriNet = transformer.Transform(solidityContract);
                Console.WriteLine("Transformação concluída.");

                // 5. Serialização para XML
                Console.WriteLine($"Gerando arquivo XML em: {outputFile}");
                var xmlGenerator = new XmlGenerator();
                xmlGenerator.Serialize(petriNet, outputFile);
                Console.WriteLine("Arquivo XML gerado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro durante o processo: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
