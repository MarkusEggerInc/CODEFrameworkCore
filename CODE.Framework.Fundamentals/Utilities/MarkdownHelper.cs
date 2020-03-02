using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace CODE.Framework.Fundamentals.Utilities
{
    public static class MarkdownHelper
    {
        public static string ToHtml(string markdown,
                                    bool useFontAwesomeInMarkdown = true, bool useAbbreviations = true, bool useAutoIdentifiers = true, bool useCitations = true,
                                    bool useCustomContainers = true, bool useEmojiAndSmiley = true, bool useEmphasisExtras = true, bool useFigures = true,
                                    bool useFootnotes = true, bool useGenericAttributes = false, bool useGridTables = true, bool useListExtras = true,
                                    bool useMediaLinks = true, bool usePipeTables = true, bool usePragmaLines = false, bool useSmartyPants = true,
                                    bool useTaskLists = true, bool useYamlFrontMatter = true, bool useAutoLinks = true)
        {
            var pipeline = BuildPipeline(useAbbreviations, useAutoIdentifiers, useCitations, useCustomContainers, useEmojiAndSmiley, useEmphasisExtras, useFigures,
                                         useFootnotes, useGenericAttributes, useGridTables, useListExtras, useMediaLinks, usePipeTables, usePragmaLines,
                                         useSmartyPants, useTaskLists, useYamlFrontMatter, useAutoLinks).Build();

            var html = Markdown.ToHtml(markdown, pipeline);

            if (useFontAwesomeInMarkdown)
                html = ParseFontAwesomeIcons(html);

            return html;
        }

        public static Regex FontAwesomeIconRegEx = new Regex(@"@icon-.*?[\s|\.|\,|\<]");

        /// <summary>
        /// Post processing routine that post-processes the HTML and replaces @icon- with font-awesome icons
        /// </summary>
        /// <param name="html">Html with font-awesome expressions</param>
        /// <returns></returns>
        private static string ParseFontAwesomeIcons(string html)
        {
            var matches = FontAwesomeIconRegEx.Matches(html);
            foreach (Match match in matches)
            {
                var iconBlock = match.Value.Substring(0, match.Value.Length - 1);
                var icon = iconBlock.Replace("@icon-", "");
                html = html.Replace(iconBlock, "<i class=\"fa fa-" + icon + "\"></i> ");
            }

            return html;
        }

        private static MarkdownPipelineBuilder BuildPipeline(bool useAbbreviations, bool useAutoIdentifiers, bool useCitations, bool useCustomContainers,
                                                             bool useEmojiAndSmiley, bool useEmphasisExtras, bool useFigures, bool useFootnotes,
                                                             bool useGenericAttributes, bool useGridTables, bool useListExtras, bool useMediaLinks,
                                                             bool usePipeTables, bool usePragmaLines, bool useSmartyPants, bool useTaskLists, 
                                                             bool useYamlFrontMatter, bool useAutoLinks)
        {
            var builder = new MarkdownPipelineBuilder();

            if (useAbbreviations) builder = builder.UseAbbreviations();
            if (useAutoIdentifiers) builder = builder.UseAutoIdentifiers(AutoIdentifierOptions.GitHub);
            if (useCitations) builder = builder.UseCitations();
            if (useCustomContainers) builder = builder.UseCustomContainers();
            if (useEmojiAndSmiley) builder = builder.UseEmojiAndSmiley();
            if (useEmphasisExtras) builder = builder.UseEmphasisExtras();
            if (useFigures) builder = builder.UseFigures();
            if (useFootnotes) builder = builder.UseFootnotes();
            if (useGenericAttributes) builder = builder.UseGenericAttributes();
            if (useGridTables) builder = builder.UseGridTables();
            if (useListExtras) builder = builder.UseListExtras();

            if (useMediaLinks) builder = builder.UseMediaLinks();
            if (usePipeTables) builder = builder.UsePipeTables();
            if (usePragmaLines) builder = builder.UsePragmaLines();
            if (useSmartyPants) builder = builder.UseSmartyPants();
            if (useTaskLists) builder = builder.UseTaskLists();
            if (useYamlFrontMatter) builder = builder.UseYamlFrontMatter();

            if (useAutoLinks) builder = builder.UseAutoLinks();

            return builder;
        }
    }
}