using System.Linq.Expressions;
using System.Reflection;
using EngineUCI.Core.Engine;

namespace EngineUCI.Core.Parsing;

// ex info depth 20 seldepth 33 multipv 1 score cp 34 nodes 782174 nps 1092421 hashfull 309 tbhits 0 time 716
// pv e2e4 e7e5 g1f3 b8c6 f1b5 g8f6 e1g1 f6e4 f1e1 e4d6 f3e5 c6e5 b1c3 f8e7 e1e5 e8g8 b5f1 e7f6 e5e1 d6f5 c3d5 f6h4 d2d4 c7c6 d5f4 d7d5 g2g3 h4e7 c2c3 c8d7 a2a4

/// <summary>
/// Parses UCI <c>info</c> response strings into structured <see cref="UciInfoLine"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// The parser operates as a token-driven state machine. Each call to <see cref="Parse"/> splits
/// the raw response on whitespace and transitions between states as tokens are consumed.
/// </para>
/// <para>
/// Supported UCI <c>info</c> tokens: <c>depth</c>, <c>seldepth</c>, <c>multipv</c>, <c>nodes</c>,
/// <c>hashfull</c>, <c>tbhits</c>, <c>time</c>, <c>score cp</c>, and <c>pv</c>.
/// Any tokens not explicitly handled are silently ignored, ensuring forward-compatibility with
/// engines that emit non-standard tokens.
/// </para>
/// </remarks>
public class UciInfoResponseParser
{
    /// <summary>
    /// Gets or sets the <see cref="UciInfoLine"/> that accumulates parsed field values during a
    /// single <see cref="Parse"/> call. After parsing completes the instance is replaced with a
    /// fresh object so the parser is ready for the next response.
    /// </summary>
    /// <value>
    /// A non-null <see cref="UciInfoLine"/> that holds the partially or fully parsed state of the
    /// current <c>info</c> response. This property is <c>public</c> to allow subclasses and test
    /// fixtures to inspect intermediate state; it should not be written to by external consumers.
    /// </value>
    public UciInfoLine InfoLine { get; set; } = new();

    /// <summary>
    /// Parses a UCI <c>info</c> response string into a structured <see cref="UciInfoLine"/> object.
    /// </summary>
    /// <param name="infoResponse">
    /// The raw UCI info response string to parse, as received from the engine's standard output.
    /// The string should begin with the <c>info</c> keyword, for example:
    /// <c>"info depth 20 seldepth 33 multipv 1 score cp 34 nodes 782174 nps 1092421 time 716 pv e2e4 e7e5"</c>.
    /// </param>
    /// <returns>
    /// A <see cref="UciInfoLine"/> containing the parsed values extracted from the response.
    /// Fields not present in the response retain their default values (<c>0</c> for integers,
    /// <c>null</c> for nullable types, and <see cref="string.Empty"/> for strings).
    /// </returns>
    /// <remarks>
    /// Each call resets the internal <see cref="InfoLine"/> accumulator, so this method is
    /// not thread-safe. Use separate <see cref="UciInfoResponseParser"/> instances when parsing
    /// responses concurrently.
    /// </remarks>
    public UciInfoLine Parse(string infoResponse)
    {
        var tokens = infoResponse.Split(' ');
        IProcessingState state = new SearchProcessingState(InfoLine);

        foreach(var token in tokens) state = state.Process(token);

        var infoLine = InfoLine;
        InfoLine = new();
        return infoLine;
    }

    private interface IProcessingState
    {
        IProcessingState Process(string data);
    }

    private class SearchProcessingState(UciInfoLine infoLine) : IProcessingState
    {
        public IProcessingState Process(string data)
        {
            return data switch
            {
                UciTokens.Info.Depth => new IntProcessingState(infoLine, x => x.Depth),
                UciTokens.Info.SelDepth => new IntProcessingState(infoLine, x => x.SelDepth),
                UciTokens.Info.MultiPv => new IntProcessingState(infoLine, x => x.MultiPv),
                UciTokens.Info.Nodes => new IntProcessingState(infoLine, x => x.Nodes),
                UciTokens.Info.HashFull => new IntProcessingState(infoLine, x => x.HashFull),
                UciTokens.Info.TbHits => new IntProcessingState(infoLine, x => x.TbHits),
                UciTokens.Info.Time => new IntProcessingState(infoLine, x => x.TimeMs),
                UciTokens.Info.Cp => new StringProcessingState(infoLine, x => x.Score),
                UciTokens.Info.Pv => new PvProcessingState(infoLine),
                _ => this
            };
        }
    }

    private class IntProcessingState : IProcessingState
    {
        private PropertyInfo Property { get; init; }
        private UciInfoLine InfoLine { get; init; }

        public IntProcessingState(UciInfoLine infoLine, Expression<Func<UciInfoLine, int?>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a property access.");

            if (member.Member is not PropertyInfo prop)
                throw new ArgumentException("Member is not a property.");

            Property = prop;
            InfoLine = infoLine;
        }

        public IntProcessingState(UciInfoLine infoLine, Expression<Func<UciInfoLine, int>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a property access.");

            if (member.Member is not PropertyInfo prop)
                throw new ArgumentException("Member is not a property.");

            Property = prop;
            InfoLine = infoLine;
        }

        public IProcessingState Process(string data)
        {
            Property.SetValue(InfoLine, int.Parse(data));
            return new SearchProcessingState(InfoLine);
        }
    }

    private class StringProcessingState : IProcessingState
    {
        private PropertyInfo Property { get; init; }
        private UciInfoLine InfoLine { get; init; }

        public StringProcessingState(UciInfoLine infoLine, Expression<Func<UciInfoLine, string?>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a property access.");

            if (member.Member is not PropertyInfo prop)
                throw new ArgumentException("Member is not a property.");

            Property = prop;
            InfoLine = infoLine;
        }

        public IProcessingState Process(string data)
        {
            Property.SetValue(InfoLine, data);
            return new SearchProcessingState(InfoLine);
        }
    }

    private class PvProcessingState(UciInfoLine infoLine) : IProcessingState
    {
        public IProcessingState Process(string data)
        {
            infoLine.Pv += $" {data}";
            return this;
        }
    }
}

/// <summary>
/// Represents a parsed UCI <c>info</c> response line containing search statistics and evaluation data
/// reported by the engine during a search operation.
/// </summary>
/// <remarks>
/// Properties that correspond to optional UCI tokens are nullable; a <c>null</c> value indicates
/// that the engine did not include that token in the response line. Non-nullable properties
/// (<see cref="Depth"/>, <see cref="MultiPv"/>, and <see cref="Score"/>) have sensible defaults
/// so that consumers do not need to null-check them.
/// </remarks>
/// <seealso cref="UciInfoResponseParser"/>
public class UciInfoLine
{
    /// <summary>
    /// Gets or sets the current search depth in plies (half-moves).
    /// Corresponds to the UCI <c>info depth</c> token.
    /// </summary>
    /// <value>Zero when the token is absent; otherwise a positive integer.</value>
    public int Depth { get; set; }

    /// <summary>
    /// Gets or sets the selective search depth in plies, which represents the maximum depth
    /// reached along any branch due to extensions. Corresponds to the UCI <c>info seldepth</c> token.
    /// </summary>
    /// <value><c>null</c> when the token is absent; otherwise a positive integer.</value>
    public int? SelDepth { get; set; }

    /// <summary>
    /// Gets or sets the one-based principal variation index in Multi-PV mode.
    /// A value of <c>1</c> identifies the engine's best line; higher values identify progressively
    /// weaker alternatives. Corresponds to the UCI <c>info multipv</c> token.
    /// </summary>
    /// <value>Zero when the token is absent (single-PV mode); otherwise a positive integer.</value>
    public int MultiPv { get; set; }

    /// <summary>
    /// Gets or sets the position evaluation score as reported by the engine.
    /// Typically a centipawn value (e.g., <c>"34"</c>) or a mate score (e.g., <c>"3"</c>
    /// when paired with <c>score mate</c>). Corresponds to the value following the
    /// UCI <c>score cp</c> token.
    /// </summary>
    /// <value><see cref="string.Empty"/> when the token is absent; otherwise the raw score string.</value>
    public string Score { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of nodes searched so far.
    /// Corresponds to the UCI <c>info nodes</c> token.
    /// </summary>
    /// <value><c>null</c> when the token is absent; otherwise a non-negative integer.</value>
    public int? Nodes { get; set; }

    /// <summary>
    /// Gets or sets the search speed in nodes per second.
    /// Corresponds to the UCI <c>info nps</c> token.
    /// </summary>
    /// <value><c>null</c> when the token is absent; otherwise a non-negative integer.</value>
    public int? Nps { get; set; }

    /// <summary>
    /// Gets or sets the transposition table utilization expressed in per mille (0 to 1000,
    /// where 1000 means 100% full). Corresponds to the UCI <c>info hashfull</c> token.
    /// </summary>
    /// <value><c>null</c> when the token is absent; otherwise an integer in the range 0–1000.</value>
    public int? HashFull { get; set; }

    /// <summary>
    /// Gets or sets the number of endgame tablebase hits during the search.
    /// Corresponds to the UCI <c>info tbhits</c> token.
    /// </summary>
    /// <value><c>null</c> when the token is absent; otherwise a non-negative integer.</value>
    public int? TbHits { get; set; }

    /// <summary>
    /// Gets or sets the time elapsed since the search began, in milliseconds.
    /// Corresponds to the UCI <c>info time</c> token.
    /// </summary>
    /// <value><c>null</c> when the token is absent; otherwise a non-negative integer.</value>
    public int? TimeMs { get; set; }

    /// <summary>
    /// Gets or sets the principal variation as a space-separated sequence of moves in
    /// Long Algebraic Notation (e.g., <c>" e2e4 e7e5 g1f3"</c>).
    /// Corresponds to all tokens following the UCI <c>info pv</c> token.
    /// </summary>
    /// <value>
    /// <c>null</c> when the token is absent; otherwise a string beginning with a leading space
    /// followed by the moves of the best line.
    /// </value>
    public string? Pv { get; set; }
}
