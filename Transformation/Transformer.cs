// File: Transformer.cs
using SolidityRDP.Models.Solidity;
using SolidityRDP.Models.PetriNet;
using System.Collections.Generic;
using System.Linq;

namespace SolidityRDP.Transformation
{
    public class Transformer
    {
        private int _idCounter = 0;
        private string NextId() => $"id{_idCounter++}";
        private Dictionary<string, string> _placeIdMap = new();

        public PnmlDocument Transform(SolidityContract contract)
        {
            var page = new Page { Id = "page1" };
            var net = new Net { Id = "net1", Page = page };

            CreatePlacesFromState(contract, page);

            if (contract.Constructor != null)
            {
                ProcessFunction(contract.Constructor, page, isConstructor: true);
            }
            foreach (var func in contract.Functions)
            {
                ProcessFunction(func, page);
            }

            var pnmlDocument = new PnmlDocument { Net = net };
            return pnmlDocument;
        }

        private void CreatePlacesFromState(SolidityContract contract, Page page)
        {
            foreach (var gv in contract.GlobalVariables)
            {
                var placeId = NextId();
                var place = new Place
                {
                    Id = placeId,
                    Name = new TextElement { Text = gv.Name }
                };

                if (!string.IsNullOrEmpty(gv.InitialValue))
                {
                    place.InitialMarking = new TextElement { Text = gv.InitialValue };
                }

                page.Places.Add(place);
                _placeIdMap[gv.Name] = placeId;
            }
        }

        private void ProcessFunction(FunctionDefinition func, Page page, bool isConstructor = false)
        {
            // Lugar de entrada da função
            var entryPlace = new Place { Id = NextId(), Name = new TextElement { Text = $"{func.Name}_entry" } };
            page.Places.Add(entryPlace);

            if (isConstructor)
            {
                // Um token no lugar de entrada do construtor para iniciar a rede
                entryPlace.InitialMarking = new TextElement { Text = "1`()" };
            }

            string lastControlPlaceId = entryPlace.Id;

            foreach (var stmt in func.Body)
            {
                if (stmt is OperationStatement op)
                {
                    var opTransition = new Transition
                    {
                        Id = NextId(),
                        Name = new TextElement { Text = $"{func.Name}_op_{op.TargetVariable}" }
                    };
                    page.Transitions.Add(opTransition);

                    // Conecta o fluxo de controle anterior à transição
                    page.Arcs.Add(CreateArc(lastControlPlaceId, opTransition.Id, "()"));

                    // Conecta a variável de estado (lugar) à transição para leitura
                    if (_placeIdMap.TryGetValue(op.TargetVariable, out var sourcePlaceId))
                    {
                        page.Arcs.Add(CreateArc(sourcePlaceId, opTransition.Id, "x"));
                    }

                    // Conecta a transição de volta à variável de estado (lugar) para escrita
                    if (_placeIdMap.TryGetValue(op.TargetVariable, out var targetPlaceId))
                    {
                        string arcExpression = op.Expression.Replace(op.TargetVariable, "x");
                        page.Arcs.Add(CreateArc(opTransition.Id, targetPlaceId, arcExpression));
                    }

                    // Cria um novo lugar de controle para o próximo statement
                    var nextControlPlace = new Place { Id = NextId(), Name = new TextElement { Text = "ctrl" } };
                    page.Places.Add(nextControlPlace);
                    page.Arcs.Add(CreateArc(opTransition.Id, nextControlPlace.Id, "()"));
                    lastControlPlaceId = nextControlPlace.Id;
                }
                else if (stmt is ConditionalStatement cond)
                {
                    var condTransition = new Transition
                    {
                        Id = NextId(),
                        Name = new TextElement { Text = $"{cond.Type}_{cond.StartLine}" },
                        Guard = new TextElement { Text = $"[{cond.Condition}]" }
                    };
                    page.Transitions.Add(condTransition);

                    // Conecta o fluxo de controle à transição condicional
                    page.Arcs.Add(CreateArc(lastControlPlaceId, condTransition.Id, "()"));

                    var nextControlPlace = new Place { Id = NextId(), Name = new TextElement { Text = "ctrl_after_{cond.Type}" } };
                    page.Places.Add(nextControlPlace);
                    page.Arcs.Add(CreateArc(condTransition.Id, nextControlPlace.Id, "()"));
                    lastControlPlaceId = nextControlPlace.Id;
                }
            }
        }

        private Arc CreateArc(string sourceId, string targetId, string expression)
        {
            return new Arc
            {
                Id = NextId(),
                SourceId = sourceId,
                TargetId = targetId,
                Expression = new TextElement { Text = expression }
            };
        }
    }
}
