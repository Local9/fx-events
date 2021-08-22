using System;
using System.Text;

namespace Lusive.Events.Generator.Syntax
{
    public class CodeWriter
    {
        private readonly StringBuilder _content;
        private int _scope;
        private readonly ScopeTracker _tracker;

        public int CurrentScope => _scope;
        
        public CodeWriter()
        {
            _content = new StringBuilder();
            _tracker = new ScopeTracker(this);
        }

        public void AppendLine(string line) => _content.Append(new string('\t', _scope)).AppendLine(line);
        public void AppendLine() => _content.AppendLine();

        public void Open()
        {
            _content.Append(new string('\t', _scope)).AppendLine("{");
            _scope++;
        }

        public void Close()
        {
            _scope--;
            _content.Append(new string('\t', _scope)).AppendLine("}");
        }

        public IDisposable Scope(string? line = null)
        {
            if (line != null)
            {
                AppendLine(line);
            }

            Open();

            return _tracker;
        }

        public override string ToString() => _content.ToString();
    }

    internal class ScopeTracker : IDisposable
    {
        private readonly CodeWriter _parent;

        public ScopeTracker(CodeWriter parent)
        {
            _parent = parent;
        }

        public void Dispose()
        {
            _parent.Close();
        }
    }
}