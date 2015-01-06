using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuCore.Util;
using StoryTeller.Domain;


namespace StoryTeller.Model
{
    public interface IFixtureStructure
    {
        int GrammarCount { get; }
        IEnumerable<GrammarError> Errors { get; }
        IEnumerable<GrammarStructure> Grammars { get; }
        GrammarStructure GrammarFor(string grammarKey);
        void AddStructure(string grammarKey, GrammarStructure structure);
        bool Equals(FixtureStructure obj);
        void LogError(Exception exception);
        void LogError(GrammarError error);
    }

    [Serializable]
    public class FixtureStructure : IFixtureNode, IFixtureStructure
    {
        private readonly List<GrammarError> _errors = new List<GrammarError>();
        private readonly string _name;
        private readonly Cache<string, GrammarStructure> _structures = new Cache<string, GrammarStructure>();


        public FixtureStructure()
            : this(Guid.NewGuid().ToString())
        {
            // TESTING ONLY
        }

        public FixtureStructure(string name)
        {
            _name = name;
            Label = name;
        }

        public void ReadFrom(IFixture fixture, FixtureLibrary library)
        {
            Description = fixture.Description;
            Label = fixture.Title.IsEmpty() ? Name : fixture.Title;

            fixture.Errors.Each(x =>
            {
                x.Node = this;
                LogError(x);
            });

            fixture.ForEachGrammar((key, grammar) => readGrammar(grammar, key, library));
        }

        private void readGrammar(IGrammar grammar, string key, FixtureLibrary library)
        {
            GrammarStructure structure = grammar.ToStructure(library);
            structure.Description = grammar.Description;

            AddStructure(key, structure);
        }

        public int GrammarCount { get { return _structures.Count; } }

        #region IFixtureNode Members

        [Obsolete("Wanna kill this")]
        public IEnumerable<GrammarError> AllErrors()
        {
            foreach (GrammarError error in _errors)
            {
                yield return error;
            }

            foreach (GrammarStructure structure in _structures)
            {
                foreach (GrammarError error in structure.AllErrors())
                {
                    yield return error;
                }
            }
        }

        public IEnumerable<GrammarError> Errors
        {
            get
            {
                return _errors;
            }
        }

        public string Label { get; set; }

        public string Description { get; set; }

        public string Name { get { return _name; } }

        public TPath GetPath()
        {
            return new TPath(_name);
        }

        #endregion

        public GrammarStructure GrammarFor(string grammarKey)
        {
            return _structures[grammarKey];
        }

        public void AddStructure(string grammarKey, GrammarStructure structure)
        {
            structure.Name = grammarKey;
            structure.Parent = this;
            _structures[grammarKey] = structure;
        }

        public IEnumerable<GrammarStructure> Grammars
        {
            get
            {
                return _structures.GetAll();
            }
        }

        public bool Equals(FixtureStructure obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj._name, _name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(FixtureStructure)) return false;
            return Equals((FixtureStructure)obj);
        }

        public override int GetHashCode()
        {
            return (_name != null ? _name.GetHashCode() : 0);
        }

        public void LogError(Exception exception)
        {
            var error = new GrammarError
            {
                ErrorText = exception.ToString(),
                Message = "Fixture '{0}' could not be loaded".ToFormat(_name),
                Node = this
            };

            _errors.Add(error);
        }

        public void LogError(GrammarError error)
        {
            _errors.Add(error);
        }

    }
}