using System.Text.RegularExpressions;

namespace EngineUCI.Core.Parsing;

/// <summary>
/// Provides parsing functionality for UCI "bestmove" responses from chess engines.
/// Extracts the best move and optional ponder move from engine output.
/// </summary>
internal static partial class BestMove
{
    /// <summary>
    /// Defines the named capture groups used in the bestmove regular expression pattern.
    /// </summary>
    private static class CaptureGroups
    {
        /// <summary>
        /// Captures the "bestmove" token.
        /// </summary>
        public const string Response = "response";

        /// <summary>
        /// Captures the actual best move in algebraic notation.
        /// </summary>
        public const string BestMove = "bestMove";

        /// <summary>
        /// Captures the "ponder" token when present.
        /// </summary>
        public const string Ponder = "ponder";

        /// <summary>
        /// Captures the ponder move in algebraic notation when present.
        /// </summary>
        public const string PonderMove = "ponderMove";
    }

    /// <summary>
    /// Parses a UCI "bestmove" response and extracts the best move.
    /// </summary>
    /// <param name="value">The raw UCI response string from the engine.</param>
    /// <returns>
    /// The best move in algebraic notation (e.g., "e2e4", "g1f3") if parsing succeeds;
    /// otherwise, an empty string.
    /// </returns>
    /// <remarks>
    /// This method handles both simple "bestmove [move]" and extended "bestmove [move] ponder [pondermove]" formats.
    /// Only the best move is returned; the ponder move is ignored in the current implementation.
    /// </remarks>
    public static string Parse(string value)
    {
        var match = BestMoveRegex().Match(value);

        return match.Success
            ? match.Groups[CaptureGroups.BestMove].Value
            : string.Empty;
    }

    /// <summary>
    /// Compiled regular expression pattern for parsing UCI "bestmove" responses.
    /// Matches the format: "bestmove [move] ponder [pondermove]" where moves are in algebraic notation.
    /// </summary>
    /// <returns>A compiled regex pattern for efficient bestmove parsing.</returns>
    [GeneratedRegex("(?<response>bestmove) (?<bestMove>[a-h][1-8][a-h][1-8]) (?<ponder>ponder) (?<ponderMove>[a-h][1-8][a-h][1-8])")]
    private static partial Regex BestMoveRegex();
}

/// <summary>
/// Provides parsing functionality for UCI "info" responses containing position evaluations.
/// Extracts depth and centipawn score information from engine search output.
/// </summary>
internal static partial class Evaluation
{
    /// <summary>
    /// Defines the named capture groups used in evaluation regular expression patterns.
    /// </summary>
    private static class CaptureGroups
    {
        /// <summary>
        /// Captures the token identifier (e.g., "depth", "cp").
        /// </summary>
        public const string Token = "token";

        /// <summary>
        /// Captures the numeric value associated with the token.
        /// </summary>
        public const string Value = "value";
    }

    /// <summary>
    /// Parses a UCI "info" response and extracts evaluation information.
    /// </summary>
    /// <param name="value">The raw UCI "info" response string from the engine.</param>
    /// <returns>
    /// A <see cref="UciEvaluationResult"/> containing the search depth and centipawn evaluation
    /// if both depth and score information are found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method looks for both "depth" and "cp" (centipawn) tokens in the info string.
    /// Both must be present for a successful parse. The method ignores "seldepth" tokens.
    /// </remarks>
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

    /// <summary>
    /// Compiled regular expression pattern for extracting search depth from UCI "info" responses.
    /// Uses negative lookbehind to exclude "seldepth" matches and only capture "depth" values.
    /// </summary>
    /// <returns>A compiled regex pattern for efficient depth parsing.</returns>
    [GeneratedRegex("(?<!sel)(?<token>depth) (?<value>[0-9]+)")]
    private static partial Regex DepthRegex();

    /// <summary>
    /// Compiled regular expression pattern for extracting centipawn scores from UCI "info" responses.
    /// Captures both positive and negative centipawn values.
    /// </summary>
    /// <returns>A compiled regex pattern for efficient centipawn score parsing.</returns>
    [GeneratedRegex("(?<token>cp) (?<value>-{0,1}[0-9]+)")]
    private static partial Regex EvaluationRegex();
}

/// <summary>
/// Represents the result of parsing a UCI evaluation response.
/// Contains the search depth and corresponding position evaluation.
/// </summary>
/// <param name="Depth">The search depth in plies (half-moves) at which the evaluation was calculated.</param>
/// <param name="Evaluation">The position evaluation in centipawns as a string (e.g., "150", "-75").</param>
internal record UciEvaluationResult(int Depth, string Evaluation);
