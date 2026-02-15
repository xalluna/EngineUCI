using System.Text.RegularExpressions;

namespace EngineUCI.Core.Engine;

internal static partial class BestMove
{
    private static class CaptureGroups
    {
        public const string Response = "response";
        public const string BestMove = "bestMove";
        public const string Ponder = "ponder";
        public const string PonderMove = "ponderMove";
    }

    public static string Parse(string value)
    {
        var match = BestMoveRegex().Match(value);

        return match.Success
            ? match.Groups[CaptureGroups.BestMove].Value
            : string.Empty;
    }

    [GeneratedRegex("(?<response>bestmove) (?<bestMove>[a-h][1-8][a-h][1-h]) (?<ponder>ponder) (?<ponderMove>[a-h][1-8][a-h][1-h])")]
    private static partial Regex BestMoveRegex();
}

internal static partial class Evaluation
{
    private static class CaptureGroups
    {
        public const string Token = "token";
        public const string Value = "value";
    }

    public static UciEvaluationResult? Parse(string value)
    {
        var depthMatch = DepthRegex().Match(value);
        var evaluationMatch = EvaluationRegex().Match(value);

        return depthMatch.Success && evaluationMatch.Success
            ? new UciEvaluationResult(
                int.Parse(depthMatch.Groups[CaptureGroups.Value].Value),
                evaluationMatch.Groups[CaptureGroups.Value].Value)
            : null;
    }

    [GeneratedRegex("(?<!sel)(?<token>depth) (?<value>[[:digit:]]+)")]
    private static partial Regex DepthRegex();

    [GeneratedRegex("(?<token>cp) (?<value>[[:digit:]]+)")]
    private static partial Regex EvaluationRegex();
}

internal record UciEvaluationResult(int Depth, string Evaluation);
