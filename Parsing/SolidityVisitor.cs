// File: SolidityVisitor.cs
using Antlr4.Runtime.Tree;
using SolidityRDP.Models.Solidity;
using System.Linq;
using System.Collections.Generic;

namespace SolidityRDP.Parsing
{
    public class SolidityVisitor : SolidityBaseVisitor<object>
    {
        private SolidityContract _contract = new SolidityContract();
        private FunctionDefinition _currentFunction = null;

        public override object VisitSourceUnit(SolidityParser.SourceUnitContext context)
        {
            base.VisitSourceUnit(context);
            return _contract;
        }

        public override object VisitContractDefinition(SolidityParser.ContractDefinitionContext context)
        {
            _contract.Name = context.identifier().GetText();
            return base.VisitContractDefinition(context);
        }

        public override object VisitStateVariableDeclaration(SolidityParser.StateVariableDeclarationContext context)
        {
            string visibility = "internal";

            for (int i = 1; i < context.ChildCount; i++)
            {
                var child = context.GetChild(i);
                if (child is SolidityParser.IdentifierContext) break;

                string childText = child.GetText();
                if (childText == "public" || childText == "private" || childText == "internal")
                {
                    visibility = childText;
                    break;
                }
            }

            var gv = new GlobalVariable
            {
                Type = context.typeName().GetText(),
                Name = context.identifier().GetText(),
                Visibility = visibility,
                InitialValue = context.expression()?.GetText()
            };

            if (context.typeName().children.FirstOrDefault() is SolidityParser.MappingContext mappingContext)
            {
                gv.IndexType = mappingContext.mappingKey().GetText();
                gv.Type = mappingContext.typeName().GetText();
            }

            _contract.GlobalVariables.Add(gv);
            return null;
        }

        public override object VisitFunctionDefinition(SolidityParser.FunctionDefinitionContext context)
        {
            string visibility = "internal";

            var modifierList = context.modifierList();
            if (modifierList?.children != null)
            {
                foreach (var modifier in modifierList.children)
                {
                    string modifierText = modifier.GetText();
                    if (modifierText == "public" || modifierText == "private" || modifierText == "internal" || modifierText == "external")
                    {
                        visibility = modifierText;
                        break;
                    }
                }
            }

            var func = new FunctionDefinition
            {
                Name = context.functionDescriptor().identifier()?.GetText() ?? context.functionDescriptor().GetText(),
                Visibility = visibility
            };

            var parameters = context.parameterList()?.parameter();
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    func.Parameters.Add(new Parameter { Type = p.typeName().GetText(), Name = p.identifier()?.GetText() });
                }
            }

            var returnParams = context.returnParameters()?.parameterList()?.parameter();
            if (returnParams != null)
            {
                foreach (var p in returnParams)
                {
                    func.ReturnParameters.Add(new Parameter { Type = p.typeName().GetText(), Name = p.identifier()?.GetText() });
                }
            }

            _currentFunction = func;
            if (context.block() != null)
            {
                base.VisitBlock(context.block()); // Visita o corpo da função
            }
            _currentFunction = null;

            if (func.Name == "constructor")
            {
                _contract.Constructor = func;
            }
            else
            {
                _contract.Functions.Add(func);
            }

            return null;
        }

        public override object VisitExpressionStatement(SolidityParser.ExpressionStatementContext context)
        {
            if (_currentFunction == null) return null;
            var expression = context.expression();

            if (expression.expression().Length == 2 && expression.children.Count == 3 && expression.GetChild(1).GetText().EndsWith("="))
            {
                var opStatement = new OperationStatement
                {
                    TargetVariable = expression.expression(0).GetText(),
                    Expression = expression.expression(1).GetText(),
                };
                _currentFunction.Body.Add(opStatement);
            }
            else if (expression.functionCallArguments() != null)
            {
                var fnName = expression.expression(0).GetText();

                if (fnName == "require" || fnName == "assert")
                {
                    var condStatement = new ConditionalStatement
                    {
                        Type = fnName,
                        Condition = expression.functionCallArguments().GetText().Trim('(', ')'),
                        StartLine = context.Start.Line
                    };
                    _currentFunction.Body.Add(condStatement);
                }
                else
                {
                    var funcCall = new FunctionCallStatement
                    {
                        FunctionName = fnName,
                        // ✅ CORREÇÃO APLICADA AQUI
                        Arguments = expression.functionCallArguments().expressionList()?.expression().Select(e => e.GetText()).ToList() ?? new List<string>(),
                        LineNumber = context.Start.Line
                    };
                    _currentFunction.Body.Add(funcCall);
                }
            }

            return base.VisitExpressionStatement(context);
        }

        public override object VisitIfStatement(SolidityParser.IfStatementContext context)
        {
            if (_currentFunction == null) return null;

            var condStatement = new ConditionalStatement
            {
                Type = "if",
                Condition = context.expression().GetText(),
                StartLine = context.Start.Line,
                EndLine = context.Stop.Line
            };
            _currentFunction.Body.Add(condStatement);

            return base.VisitIfStatement(context);
        }
    }
}
