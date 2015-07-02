using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace RegistryLanguageService.CodeRefactorings
{
    public interface ICodeRefactoringProvider
    {
        IEnumerable<CodeAction> GetRefactorings(SnapshotSpan span);
    }
}
