using System;
using System.Text;

namespace Moonlight.Generators.Syntax
{
    public class CodeWriter
    {
        public StringBuilder Content;
        public int Scope;

        private int _indentation;
        private readonly ScopeTracker _tracker;

        public CodeWriter()
        {
            Content = new StringBuilder();
            _tracker = new ScopeTracker(this, null);
        }

        public ScopeTracker Encapsulate()
        {
            return new(this, Scope);
        }

        public void Append(string line) => Content.Append(line);

        public void AppendLine(string line) => Content.Append(new string('\t', _indentation)).AppendLine(line);
        public void AppendLine() => Content.AppendLine();

        public void Open(bool scope = true)
        {
            Content.Append(new string('\t', _indentation)).AppendLine("{");
            _indentation++;

            if (scope)
            {
                Scope++;
            }
        }

        public void Close()
        {
            _indentation--;
            Content.Append(new string('\t', _indentation)).AppendLine("}");
        }

        public IDisposable BeginScope(string line)
        {
            AppendLine(line);

            return BeginScope();
        }

        public IDisposable BeginScope()
        {
            Open(false);

            return _tracker;
        }

        public override string ToString() => Content.ToString();
    }

    public class ScopeTracker : IDisposable
    {
        private int? Scope { get; }
        private CodeWriter Parent { get; }
        private int _references;

        public ScopeTracker(CodeWriter parent, int? scope)
        {
            Parent = parent;
            Scope = scope;
        }

        public ScopeTracker Reference()
        {
            _references++;

            return this;
        }

        public void Dispose()
        {
            if (_references > 0)
            {
                _references--;

                return;
            }
            
            if (Scope.HasValue)
            {
                while (Parent.Scope > Scope)
                {
                    Parent.Close();
                    Parent.Scope--;
                }
            }
            else
            {
                Parent.Close();
            }
        }
    }
}