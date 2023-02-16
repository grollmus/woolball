using Microsoft.CodeAnalysis;

namespace WoolBall.Parsing;

internal interface ISolutionLoader
{
	Task<List<Project>> ParseSolution(string solutionFile, bool includeTests);
}