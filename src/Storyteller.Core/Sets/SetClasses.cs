﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Storyteller.Core.Conversion;
using Storyteller.Core.Engine;
using Storyteller.Core.Model;

namespace Storyteller.Core.Sets
{
    public class SetVerificationGrammar : IGrammar
    {
        private readonly ISetComparison _comparison;
        private string _leafName;
        private readonly string _title;
        private Cell[] _cells;
        private bool _ordered;

        public SetVerificationGrammar(string title, string leafName, ISetComparison comparison)
        {
            _title = title;
            _leafName = leafName;
            _comparison = comparison;
            _ordered = false;
        }

        public IExecutionStep CreatePlan(Step step, FixtureLibrary library)
        {
            var section = step
                .Collections[_leafName];

            var expected = section
                .Children.OfType<Step>()
                .Select(row => _cells.ToStepValues(row))
                .ToArray();

            var matcher = _ordered ? OrderedSetMatcher.Flyweight : UnorderedSetMatcher.Flyweight;

            return new VerificationSetPlan(section, matcher, _comparison, expected, _cells);
        }

        public GrammarModel Compile(CellHandling cells)
        {
            _cells = _comparison.BuildCells(cells);

            return new SetVerification
            {
                title = _title,
                cells = _cells,
                collection = _leafName,
                ordered = _ordered
            };
        }

        public SetVerificationGrammar Ordered()
        {
            _ordered = true;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leafName"></param>
        /// <returns></returns>
        public SetVerificationGrammar LeafNameIs(string leafName)
        {
            _leafName = leafName;
            return this;
        }

    }

    public class VerificationSetPlan : ILineExecution
    {
        private readonly Cell[] _cells;
        private readonly ISetComparison _comparison;
        private readonly IEnumerable<StepValues> _expected;
        private readonly ISetMatcher _matcher;
        private readonly Section _section;

        public VerificationSetPlan(Section section, ISetMatcher matcher, ISetComparison comparison,
            IEnumerable<StepValues> expected, Cell[] cells)
        {
            _section = section;
            _matcher = matcher;
            _comparison = comparison;
            _expected = expected;
            _cells = cells;
        }

        public int Count()
        {
            return 1;
        }

        public void AcceptVisitor(ISpecExecutor executor)
        {
            executor.Line(this);
        }

        public void Execute(ISpecContext context)
        {
            var fetch = _comparison.Fetch(context);

            _expected.Each(x =>
            {
                x.DoDelayedConversions(context);
                if (!x.Errors.Any()) return;

                context.LogResult(x.ToConversionErrorResult());
            });

            if (_expected.Any(x => x.HasErrors())) return;

            fetch.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // TODO -- do the Flatten() trick here on the aggregated exception
                    context.LogException(_section.Id, t.Exception);
                }

                if (t.IsCompleted)
                {
                    var result = CreateResults(_expected, t.Result);
                    context.LogResult(result);
                }
            }).Wait(context.Cancellation);
        }

        public object Position { get; set; }

        public SetVerificationResult CreateResults(IEnumerable<StepValues> expected, IEnumerable<StepValues> actual)
        {
            var result = _matcher.Match(_cells, expected, actual);
            result.id = _section.Id;

            return result;
        }
    }

    public class OrderedSetMatcher : ISetMatcher
    {
        public static readonly ISetMatcher Flyweight = new OrderedSetMatcher();

        public SetVerificationResult Match(Cell[] cells, IEnumerable<StepValues> expectedValues,
            IEnumerable<StepValues> actualValues)
        {
            throw new NotImplementedException();
        }
    }
}