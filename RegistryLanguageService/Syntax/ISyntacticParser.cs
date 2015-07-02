using Microsoft.VisualStudio.Text;
using System;

namespace RegistryLanguageService.Syntax
{
    public interface ISyntacticParser
    {
        SyntaxTree Parse(ITextSnapshot snapshot);
    }

    public static class CommonScanner
    {
        public static char? Peek(this ITextSnapshot snapshot, SnapshotPoint point, int delta = 0)
        {
            SnapshotPoint peekPoint = point + delta;

            if (peekPoint >= snapshot.Length)
                return null;

            return peekPoint.GetChar();
        }

        public static string PeekString(this ITextSnapshot snapshot, SnapshotPoint point, int length)
        {
            if (point >= snapshot.Length)
                return null;

            return new SnapshotSpan(point, Math.Min(length, snapshot.Length - point)).GetText();
        }

        public static bool IsAtExact(this ITextSnapshot snapshot, SnapshotPoint point, string text)
        {
            return snapshot.PeekString(point, text.Length) == text;
        }

        public static bool IsAtExact(this ITextSnapshot snapshot, SnapshotPoint point, char @char)
        {
            return snapshot.Peek(point) == @char;
        }

        public static SnapshotSpan ReadExact(this ITextSnapshot snapshot, ref SnapshotPoint point, char @char)
        {
            SnapshotPoint start = point;

            if (point < snapshot.Length && point.GetChar() == @char)
            {
                point = point + 1;
                return new SnapshotSpan(start, point);
            }

            return new SnapshotSpan(point, 0);
        }

        public static SnapshotSpan ReadWhiteSpace(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToLineEndWhile(ref point, Char.IsWhiteSpace, rewindWhiteSpace: false);
        }

        public static SnapshotSpan ReadToLineEndWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate, bool rewindWhiteSpace = true)
        {
            SnapshotPoint start = point;

            while (
                point.Position < snapshot.Length &&
                point.GetChar() != '\n' && point.GetChar() != '\r' &&
                predicate(point.GetChar())
            )
                point = point + 1;

            if (rewindWhiteSpace)
            {
                while (
                    point - 1 >= start &&
                    Char.IsWhiteSpace((point - 1).GetChar())
                )
                    point = point - 1;
            }

            return new SnapshotSpan(start, point);
        }
    }
}