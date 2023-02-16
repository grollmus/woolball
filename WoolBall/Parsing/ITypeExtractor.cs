using Microsoft.CodeAnalysis;
using WoolBall.CommandLine;
using WoolBall.Services;

namespace WoolBall.Parsing;

internal interface ITypeExtractor
{
	IAsyncEnumerable<string> GetTypeNamesIn(Project project);
	IAsyncEnumerable<Reference> GetTypeReferencesIn(Project project, ReferenceType referenceType);
}