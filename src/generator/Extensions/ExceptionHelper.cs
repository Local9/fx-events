using System;

namespace Lusive.Events.Generator.Extensions
{
    public static class ExceptionHelper
    {
        private const string SearchInPattern = " in ";
        private const string ProjectDirectory = "D:\\Project Files\\Moonlight\\src\\vendor\\fx-events";
        private const string GithubUrl = "https://github.com/frostylucas/fx-events/blob/main";
        
        public static string GetCulprit(string trace)
        {
            string Text()
            {
                var index = trace.IndexOf(SearchInPattern, StringComparison.Ordinal);

                if (index == -1) return trace.Replace(Environment.NewLine, ", ").Trim();
            
                var pos = index + SearchInPattern.Length;
                var end = trace.IndexOf(' ', pos);

                return end != -1 ? trace.Substring(pos, end) : trace.Substring(pos);   
            }

            var culprit = Text();
            var directoryIndex = culprit.IndexOf(ProjectDirectory, StringComparison.Ordinal);

            if (directoryIndex != -1)
            {
                return GithubUrl + culprit.Substring(directoryIndex + ProjectDirectory.Length).Replace("\\", "/").Replace(":line ", "#L");
            }

            return culprit;
        }
    }
}