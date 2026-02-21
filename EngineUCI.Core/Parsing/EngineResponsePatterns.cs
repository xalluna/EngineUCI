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
