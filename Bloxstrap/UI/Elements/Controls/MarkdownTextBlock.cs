using Bloxstrap.UI.ViewModels;
using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

using WpfInline = System.Windows.Documents.Inline;
using MarkBlock = Markdig.Syntax.Block;
using MarkCodeBlock = Markdig.Syntax.CodeBlock;
using MarkFencedCodeBlock = Markdig.Syntax.FencedCodeBlock;
using MarkHeadingBlock = Markdig.Syntax.HeadingBlock;
using MarkHtmlBlock = Markdig.Syntax.HtmlBlock;
using MarkListBlock = Markdig.Syntax.ListBlock;
using MarkListItemBlock = Markdig.Syntax.ListItemBlock;
using MarkParagraphBlock = Markdig.Syntax.ParagraphBlock;
using MarkQuoteBlock = Markdig.Syntax.QuoteBlock;
using MarkThematicBreakBlock = Markdig.Syntax.ThematicBreakBlock;
using MarkInline = Markdig.Syntax.Inlines.Inline;
using MarkContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MarkCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MarkEmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using MarkLineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
using MarkLinkInline = Markdig.Syntax.Inlines.LinkInline;
using MarkLiteralInline = Markdig.Syntax.Inlines.LiteralInline;

namespace Bloxstrap.UI.Elements.Controls
{
    /// <summary>
    /// TextBlock with markdown support.
    /// </summary>
    [ContentProperty("MarkdownText")]
    [Localizability(LocalizationCategory.Text)]
    class MarkdownTextBlock : TextBlock
    {
        private static readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder()
                .UseEmphasisExtras(Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions.Marked) // enable '==' support
                .UseSoftlineBreakAsHardlineBreak()
                .Build();

        public static readonly DependencyProperty MarkdownTextProperty =
            DependencyProperty.Register(nameof(MarkdownText), typeof(string), typeof(MarkdownTextBlock),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnTextMarkdownChanged));

        [Localizability(LocalizationCategory.Text)]
        public string MarkdownText
        {
            get => (string)GetValue(MarkdownTextProperty);
            set => SetValue(MarkdownTextProperty, value);
        }

        private static WpfInline? GetWpfInlineFromMarkdownInline(MarkInline? inline)
        {
            if (inline is MarkLiteralInline literalInline)
            {
                return new Run(literalInline.ToString());
            }

            if (inline is MarkCodeInline codeInline)
            {
                return new Run(codeInline.Content.ToString())
                {
                    FontFamily = new System.Windows.Media.FontFamily("Cascadia Code")
                };
            }

            if (inline is MarkEmphasisInline emphasisInline)
            {
                Span span = new();
                bool hasContent = false;

                foreach (var childInline in emphasisInline)
                {
                    WpfInline? child = GetWpfInlineFromMarkdownInline(childInline);
                    if (child is not null)
                    {
                        span.Inlines.Add(child);
                        hasContent = true;
                    }
                }

                if (!hasContent)
                    return null;

                if (emphasisInline.DelimiterChar is '*' or '_')
                    span.FontStyle = FontStyles.Italic;

                if (emphasisInline.DelimiterCount > 1)
                    span.FontWeight = FontWeights.Bold;

                if (emphasisInline.DelimiterChar == '=')
                    span.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

                return span;
            }

            if (inline is MarkLinkInline linkInline)
            {
                WpfInline? child = GetWpfInlineFromMarkdownInline(linkInline.FirstChild);
                if (child is null || string.IsNullOrEmpty(linkInline.Url))
                    return child;

                return new Hyperlink(child)
                {
                    Command = GlobalViewModel.OpenWebpageCommand,
                    CommandParameter = linkInline.Url
                };
            }

            if (inline is MarkLineBreakInline)
                return new LineBreak();

            return null;
        }

        private void AddMarkdownInline(MarkInline? inline)
        {
            WpfInline? child = GetWpfInlineFromMarkdownInline(inline);
            if (child is not null)
                Inlines.Add(child);
        }

        private void AddMarkdownInlines(MarkContainerInline? inline)
        {
            if (inline is null)
                return;

            foreach (var childInline in inline)
                AddMarkdownInline(childInline);
        }

        private void AddIndent(int indent)
        {
            if (indent > 0)
                Inlines.Add(new Run(new string('\u00A0', indent * 4)));
        }

        private void AddLineBreak()
        {
            Inlines.Add(new LineBreak());
        }

        private void AddCodeLines(Markdig.Helpers.StringLineGroup lines, int indent)
        {
            foreach (var line in lines.Lines)
            {
                AddIndent(indent);
                Inlines.Add(new Run(line.ToString())
                {
                    FontFamily = new System.Windows.Media.FontFamily("Cascadia Code")
                });
                AddLineBreak();
            }
        }

        private bool AddMarkdownBlock(MarkBlock block, int indent)
        {
            if (block is MarkParagraphBlock paragraph)
            {
                AddIndent(indent);
                AddMarkdownInlines(paragraph.Inline);
                return true;
            }

            if (block is MarkHeadingBlock heading)
            {
                AddIndent(indent);

                Span headingSpan = new()
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = Math.Max(12, (double.IsNaN(FontSize) ? 14 : FontSize) + (4 - heading.Level) * 2)
                };

                if (heading.Inline is not null)
                {
                    foreach (var childInline in heading.Inline)
                    {
                        WpfInline? child = GetWpfInlineFromMarkdownInline(childInline);
                        if (child is not null)
                            headingSpan.Inlines.Add(child);
                    }
                }

                Inlines.Add(headingSpan);
                return true;
            }

            if (block is MarkListBlock list)
                return AddMarkdownList(list, indent);

            if (block is MarkQuoteBlock quote)
            {
                foreach (var childBlock in quote)
                {
                    AddIndent(indent);
                    Inlines.Add(new Run("> "));
                    AddMarkdownBlock(childBlock, indent + 1);
                    AddLineBreak();
                }

                return true;
            }

            if (block is MarkFencedCodeBlock fencedCodeBlock)
            {
                AddCodeLines(fencedCodeBlock.Lines, indent);
                return true;
            }

            if (block is MarkCodeBlock codeBlock)
            {
                AddCodeLines(codeBlock.Lines, indent);
                return true;
            }

            if (block is MarkHtmlBlock htmlBlock)
            {
                AddCodeLines(htmlBlock.Lines, indent);
                return true;
            }

            if (block is MarkThematicBreakBlock)
            {
                Inlines.Add(new Run("────────"));
                AddLineBreak();
                return true;
            }

            return false;
        }

        private bool AddMarkdownList(MarkListBlock list, int indent)
        {
            int orderedNumber = int.TryParse(list.OrderedStart, out int startNumber) ? startNumber : 1;

            foreach (var childBlock in list)
            {
                if (childBlock is not MarkListItemBlock listItem)
                    continue;

                AddIndent(indent);

                string marker = list.IsOrdered
                    ? $"{orderedNumber++}{list.OrderedDelimiter}\u00A0"
                    : indent > 0 ? "◦\u00A0" : "•\u00A0";
                Inlines.Add(new Run(marker)
                {
                    FontWeight = FontWeights.SemiBold
                });

                bool firstChild = true;
                foreach (var itemBlock in listItem)
                {
                    if (!firstChild)
                        AddLineBreak();

                    AddMarkdownBlock(itemBlock, firstChild ? indent : indent + 1);

                    firstChild = false;
                }

                AddLineBreak();
            }

            return true;
        }

        private void AddPlainTextFallback(string rawDocument)
        {
            string normalized = rawDocument.Replace("\r\n", "\n", StringComparison.Ordinal);
            string[] lines = normalized.Split('\n');

            for (int index = 0; index < lines.Length; index++)
            {
                Inlines.Add(new Run(lines[index]));
                if (index < lines.Length - 1)
                    AddLineBreak();
            }
        }

        private static void OnTextMarkdownChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyObject is not MarkdownTextBlock markdownTextBlock)
                return;

            if (dependencyPropertyChangedEventArgs.NewValue is not string rawDocument)
                return;

            markdownTextBlock.Inlines.Clear();

            MarkdownDocument document = Markdown.Parse(rawDocument, _markdownPipeline);
            List<MarkBlock> blocks = document.ToList();
            int renderedBlocks = 0;

            for (int index = 0; index < blocks.Count; index++)
            {
                if (!markdownTextBlock.AddMarkdownBlock(blocks[index], 0))
                    continue;

                renderedBlocks++;

                if (index < blocks.Count - 1)
                    markdownTextBlock.AddLineBreak();
            }

            // Keep uncommon/older Markdown documents visible instead of silently dropping them.
            if (renderedBlocks == 0 && !string.IsNullOrWhiteSpace(rawDocument))
                markdownTextBlock.AddPlainTextFallback(rawDocument);
        }
    }
}
