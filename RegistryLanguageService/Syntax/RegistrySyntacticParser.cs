using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace RegistryLanguageService.Syntax
{
    [Export("Registry", typeof(ISyntacticParser))]
    internal sealed class RegistrySyntacticParser : ISyntacticParser
    {
        [ImportingConstructor]
        public RegistrySyntacticParser(IClassificationTypeRegistryService registry)
        {
            _commentType = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _versionType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
            _delimiterType = registry.GetClassificationType("Registry/Delimiter");
            _propertyNameType = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
            _propertyValueType = registry.GetClassificationType(PredefinedClassificationTypeNames.Character);
            _sectionNameType = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
            _stringType = registry.GetClassificationType(PredefinedClassificationTypeNames.String);
            _typeType = registry.GetClassificationType("Registry/Type");
            _operatorType = registry.GetClassificationType(PredefinedClassificationTypeNames.Operator);
            _numberType = registry.GetClassificationType(PredefinedClassificationTypeNames.Number);
        }

        private readonly IClassificationType _commentType;
        private readonly IClassificationType _sectionNameType;
        private readonly IClassificationType _delimiterType;
        private readonly IClassificationType _propertyNameType;
        private readonly IClassificationType _propertyValueType;
        private readonly IClassificationType _stringType;
        private readonly IClassificationType _typeType;
        private readonly IClassificationType _operatorType;
        private readonly IClassificationType _versionType;
        private readonly IClassificationType _numberType;


        public SyntaxTree Parse(ITextSnapshot snapshot)
        {
            RegistryDocumentSyntax root = new RegistryDocumentSyntax() { Snapshot = snapshot };

            List<SnapshotToken> leadingTrivia = new List<SnapshotToken>();
            RegistrySectionSyntax section = null;

            foreach (ITextSnapshotLine line in snapshot.Lines)
            {
                SnapshotPoint cursor = line.Start;
                snapshot.ReadWhiteSpace(ref cursor); // skip white space

                // read version
                if (line.LineNumber == 0)
                {
                    SnapshotToken versionToken = new SnapshotToken(snapshot.ReadToCommentOrLineEndWhile(ref cursor, _ => true), _versionType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    root.VersionToken = versionToken;

                    if (snapshot.IsAtExact(cursor, RegistrySyntaxFacts.Comment))
                    {
                        SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);
                        leadingTrivia.Add(commentToken);
                    }

                    continue;
                }

                // skip blank lines
                if (cursor == line.End)
                    continue;

                char first = cursor.GetChar();

                // comment
                if (first == RegistrySyntaxFacts.Comment)
                {
                    SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);
                    leadingTrivia.Add(commentToken);
                }

                // section
                else if (first == RegistrySyntaxFacts.SectionNameOpeningBracket)
                {
                    if (section != null)
                        root.Sections.Add(section);
                    
                    SnapshotToken deleteToken = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.DeleteKey), _operatorType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken openingBracket = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.SectionNameOpeningBracket), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);

                    // read key
                    SeparatedTokenListSyntax path = new SeparatedTokenListSyntax() { Section = section };

                    SnapshotToken name = new SnapshotToken(snapshot.ReadSectionName(ref cursor), _sectionNameType);
                    path.Tokens.Add(name);
                    snapshot.ReadWhiteSpace(ref cursor);

                    while (snapshot.IsAtExact(cursor, RegistrySyntaxFacts.KeySeparator))
                    {
                        SnapshotToken separator = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.KeySeparator), _sectionNameType);
                        path.Separators.Add(separator);
                        snapshot.ReadWhiteSpace(ref cursor);

                        name = new SnapshotToken(snapshot.ReadSectionName(ref cursor), _sectionNameType);
                        path.Tokens.Add(name);
                        snapshot.ReadWhiteSpace(ref cursor);
                    }
                    
                    SnapshotToken closingBracket = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.SectionNameClosingBracket), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);

                    IList<SnapshotToken> trailingTrivia = new List<SnapshotToken>();
                    if (!commentToken.IsMissing)
                        trailingTrivia.Add(commentToken);

                    section = new RegistrySectionSyntax()
                    {
                        Document = root,
                        LeadingTrivia = leadingTrivia,
                        DeleteToken = deleteToken,
                        OpeningBracketToken = openingBracket,
                        NameSyntax = path,
                        ClosingBracketToken = closingBracket,
                        TrailingTrivia = trailingTrivia,
                    };
                    leadingTrivia = new List<SnapshotToken>();
                }

                // property
                else if (Char.IsLetter(first) || first == RegistrySyntaxFacts.Quote)
                {
                    // read key name
                    SnapshotToken nameOpeningQuote = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.Quote), _stringType);
                    SnapshotToken name = new SnapshotToken(snapshot.ReadPropertyName(ref cursor), _propertyNameType);
                    SnapshotToken nameClosingQuote = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.Quote), _stringType);

                    // delimiter
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken nameValueDelimiter = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.PropertyNameValueDelimiter), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    
                    // delete token
                    SnapshotToken deleteToken = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.DeleteKey), _operatorType);
                    snapshot.ReadWhiteSpace(ref cursor);

                    SnapshotToken valueOpeningQuote = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.Quote), _stringType);

                    // read type
                    SnapshotSpan typeSpan = snapshot.ReadType(ref cursor);

                    bool readType = !typeSpan.IsEmpty && RegistrySyntaxFacts.IsKnownDataTypeNameOrShortcut(typeSpan.GetText());
                    
                    SnapshotToken type = readType ? new SnapshotToken(typeSpan, _typeType) : SnapshotToken.CreateMissing(cursor, _typeType);
                    SnapshotToken typeSpecifierOpeningBrace = readType ? new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.TypeSpecifierOpeningBrace), _typeType) : SnapshotToken.CreateMissing(cursor, _typeType);
                    SnapshotToken typeSpecifier = readType ? new SnapshotToken(snapshot.ReadTypeSpecifier(ref cursor), _typeType) : SnapshotToken.CreateMissing(cursor, _typeType);
                    SnapshotToken typeSpecifierClosingBrace = readType ? new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.TypeSpecifierClosingBrace), _typeType) : SnapshotToken.CreateMissing(cursor, _typeType);

                    SnapshotToken typeValueDelimiter = readType ? new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.TypeValueDelimiter), _delimiterType) : SnapshotToken.CreateMissing(cursor, _typeType);

                    if (!readType)
                        cursor = typeSpan.Start;

                    // read value
                    SnapshotToken value = new SnapshotToken(snapshot.ReadPropertyValue(ref cursor), type.IsMissing ? _stringType : _propertyValueType);
                    SnapshotToken valueClosingQuote = new SnapshotToken(snapshot.ReadExact(ref cursor, RegistrySyntaxFacts.Quote), _stringType);

                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken commentToken = new SnapshotToken(snapshot.ReadComment(ref cursor), _commentType);

                    IList<SnapshotToken> trailingTrivia = new List<SnapshotToken>();
                    if (!commentToken.IsMissing)
                        trailingTrivia.Add(commentToken);

                    RegistryPropertySyntax property = new RegistryPropertySyntax()
                    {
                        Section = section,
                        LeadingTrivia = leadingTrivia,
                        NameOpeningQuoteToken = nameOpeningQuote,
                        NameToken = name,
                        NameClosingQuoteToken = nameClosingQuote,
                        NameValueDelimiterToken = nameValueDelimiter,
                        DeleteToken = deleteToken,
                        ValueOpeningQuoteToken = valueOpeningQuote,
                        TypeToken = type,
                        TypeSpecifierOpeningBraceToken = typeSpecifierOpeningBrace,
                        TypeSpecifierToken = typeSpecifier,
                        TypeSpecifierClosingBraceToken = typeSpecifierClosingBrace,
                        TypeValueDelimiterToken = typeValueDelimiter,
                        ValueToken = value,
                        ValueClosingQuoteToken = valueClosingQuote,
                        TrailingTrivia = trailingTrivia,
                    };
                    section.Properties.Add(property);
                    leadingTrivia = new List<SnapshotToken>();
                }

                // error
                else
                    ; // TODO: report error
            }

            if (section != null && leadingTrivia.Any())
                foreach (var trivia in leadingTrivia)
                    section.TrailingTrivia.Add(trivia);

            if (section != null)
                root.Sections.Add(section);

            return new SyntaxTree(snapshot, root);
        }
    }

    internal static class RegistryScanner
    {
        public static SnapshotSpan ReadType(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, Char.IsLetter);
        }
        public static SnapshotSpan ReadTypeSpecifier(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, Char.IsNumber);
        }
        public static SnapshotSpan ReadSectionName(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, c => c != RegistrySyntaxFacts.KeySeparator && c != RegistrySyntaxFacts.SectionNameClosingBracket);
        }
        public static SnapshotSpan ReadPropertyName(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, c => c != RegistrySyntaxFacts.Quote && c != RegistrySyntaxFacts.PropertyNameValueDelimiter);
        }
        public static SnapshotSpan ReadPropertyValue(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToCommentOrLineEndWhile(ref point, c => c != RegistrySyntaxFacts.Quote);
        }

        public static SnapshotSpan ReadComment(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            if (point.Position == snapshot.Length || point.GetChar() != RegistrySyntaxFacts.Comment)
                return new SnapshotSpan(point, 0);

            return snapshot.ReadToLineEndWhile(ref point, _ => true);
        }

        public static SnapshotSpan ReadToCommentOrLineEndWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate)
        {
            return snapshot.ReadToLineEndWhile(ref point, c => c != RegistrySyntaxFacts.Comment && predicate(c));
        }
    }
}
