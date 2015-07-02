using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace RegistryLanguageService.Syntax
{
    public class SnapshotToken
    {
        public SnapshotToken(ClassificationSpan classificationSpan)
        {
            this.Span = classificationSpan;
        }
        public SnapshotToken(SnapshotSpan span, IClassificationType type)
            : this(new ClassificationSpan(span, type))
        { }

        public ClassificationSpan Span { get; private set; }

        public virtual string Value { get { return this.Span.Span.GetText(); } }

        public string ValueText { get { return this.Span.Span.GetText(); } }

        public bool IsMissing
        {
            get { return this.Span.Span.IsEmpty; }
        }


        public static SnapshotToken CreateMissing(SnapshotPoint point, IClassificationType type)
        {
            return new SnapshotToken(new SnapshotSpan(point, 0), type);
        }
    }
}
