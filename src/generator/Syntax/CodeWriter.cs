using System;
using System.Text;

namespace Moonlight.Generators.Syntax
{
    public class CodeWriter
    {
        private readonly StringBuilder _content = new();
        private int _indentLevel;
        private readonly ScopeTracker _tracker;

        public CodeWriter()
        {
            _tracker = new ScopeTracker(this);
        }

        public void Append(string line) => _content.Append(line);
        public void AppendLine(string line) => _content.Append(new string('\t', _indentLevel)).AppendLine(line);
        public void AppendLine() => _content.AppendLine();

        public IDisposable BeginScope(string line)
        {
            AppendLine(line);
            return BeginScope();
        }

        public IDisposable BeginScope()
        {
            _content.Append(new string('\t', _indentLevel)).AppendLine("{");
            _indentLevel += 1;
            
            return _tracker;
        }

        public void EndLine() => _content.AppendLine();

        public void EndScope()
        {
            _indentLevel -= 1;
            _content.Append(new string('\t', _indentLevel)).AppendLine("}");
        }

        public void StartLine() => _content.Append(new string('\t', _indentLevel));
        public override string ToString() => _content.ToString();

        private string EscapeString(string text) => text.Replace("\"", "\"\"");

        private class ScopeTracker : IDisposable
        {
            private CodeWriter Parent { get; }

            public ScopeTracker(CodeWriter parent)
            {
                Parent = parent;
            }

            public void Dispose()
            {
                Parent.EndScope();
            }
        }
    }
}