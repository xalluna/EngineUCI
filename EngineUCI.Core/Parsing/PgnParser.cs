using System.Text;
using System.Text.RegularExpressions;

namespace EngineUCI.Core.Parsing;

/// <summary>
/// Represents a parsed PGN (Portable Game Notation) game, containing the tag-pair headers,
/// the move list, and the game result.
/// </summary>
/// <remarks>
/// <para>
/// Each instance corresponds to one complete PGN game. The <see cref="Headers"/> dictionary
/// preserves all tag pairs found in the PGN header section, including the mandatory Seven Tag
/// Roster (<c>Event</c>, <c>Site</c>, <c>Date</c>, <c>Round</c>, <c>White</c>, <c>Black</c>,
/// <c>Result</c>) as well as any optional supplemental tags.
/// </para>
/// <para>
/// The <see cref="Moves"/> list contains only the bare move tokens in PGN algebraic notation,
/// stripped of move numbers, annotations (<c>!</c>, <c>?</c>), and check/checkmate symbols
/// (<c>+</c>, <c>#</c>). Comments (<c>{ }</c>), variations (<c>( )</c>), and Numeric Annotation
/// Glyphs (<c>$N</c>) are silently discarded during parsing.
/// </para>
/// </remarks>
/// <seealso cref="PgnParser"/>
public class PgnGame
{
    /// <summary>
    /// Gets or sets the collection of PGN tag-pair headers keyed by tag name.
    /// </summary>
    /// <value>
    /// A dictionary where each key is a PGN tag name (e.g., <c>"Event"</c>, <c>"White"</c>)
    /// and each value is the corresponding tag value string. Initialized to an empty dictionary.
    /// </value>
    /// <remarks>
    /// Tag names are case-sensitive as per the PGN specification. The dictionary is populated
    /// in the order the tags appear in the source text, but enumeration order is not guaranteed
    /// to match insertion order across all runtimes.
    /// </remarks>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>
    /// Gets or sets the ordered list of moves in PGN algebraic notation extracted from the game.
    /// </summary>
    /// <value>
    /// A list of move strings such as <c>"e4"</c>, <c>"Nf3"</c>, <c>"O-O"</c>, <c>"exd5"</c>,
    /// or <c>"e8=Q"</c>. Move numbers, annotations, and check/checkmate symbols are excluded.
    /// The list alternates between white and black moves beginning with white's first move.
    /// Initialized to an empty list.
    /// </value>
    public List<string> Moves { get; set; } = [];

    /// <summary>
    /// Gets or sets the game result token as it appears in the PGN move text.
    /// </summary>
    /// <value>
    /// One of the standard PGN result strings: <c>"1-0"</c> (white wins), <c>"0-1"</c>
    /// (black wins), <c>"1/2-1/2"</c> (draw), or <c>"*"</c> (game in progress or unknown).
    /// Initialized to <see cref="string.Empty"/> when no result token is present.
    /// </value>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Returns a human-readable representation of this game, showing the headers, move list,
    /// and result.
    /// </summary>
    /// <returns>
    /// A multi-line string that begins with a section heading, lists each header tag on its own
    /// line in standard PGN bracket format, and ends with the move list and result.
    /// </returns>
    /// <example>
    /// <code>
    /// var game = parser.ParseGame(pgnText);
    /// Console.WriteLine(game.ToString());
    /// // === PGN Game ===
    /// // [Event "World Championship"]
    /// // [White "Carlsen, M"]
    /// // ...
    /// // Moves: e4, e5, Nf3, ...
    /// // Result: 1-0
    /// </code>
    /// </example>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== PGN Game ===");

        foreach (var header in Headers)
        {
            sb.AppendLine($"[{header.Key} \"{header.Value}\"]");
        }

        sb.AppendLine();
        sb.AppendLine($"Moves: {string.Join(", ", Moves)}");
        sb.AppendLine($"Result: {Result}");

        return sb.ToString();
    }
}

/// <summary>
/// Parses PGN (Portable Game Notation) text into structured <see cref="PgnGame"/> objects.
/// Handles headers, moves, annotations, comments, variations, and Numeric Annotation Glyphs (NAGs).
/// </summary>
/// <remarks>
/// <para>
/// The parser uses a token-driven state machine to process PGN text character by character.
/// It correctly handles all standard PGN constructs:
/// </para>
/// <list type="bullet">
///   <item><description>Tag pairs enclosed in square brackets (e.g., <c>[Event "Wch"]</c>).</description></item>
///   <item><description>Move number indicators followed by dots (e.g., <c>1.</c>, <c>3...</c>).</description></item>
///   <item><description>Standard algebraic moves, castling (<c>O-O</c>, <c>O-O-O</c>), and promotions (e.g., <c>e8=Q</c>).</description></item>
///   <item><description>Brace comments (<c>{ comment }</c>), which are silently discarded.</description></item>
///   <item><description>Parenthetical variations (<c>( variation )</c>), which are silently discarded.</description></item>
///   <item><description>Numeric Annotation Glyphs (<c>$N</c>), which are silently discarded.</description></item>
///   <item><description>Move annotations (<c>!</c>, <c>?</c>, <c>!?</c>, <c>?!</c>), which are stripped from moves.</description></item>
///   <item><description>Check (<c>+</c>) and checkmate (<c>#</c>) symbols, which are stripped from moves.</description></item>
/// </list>
/// <para>
/// This parser does not validate whether moves are legal according to chess rules. It performs
/// only syntactic parsing to extract the token sequence from the PGN text.
/// </para>
/// </remarks>
/// <seealso cref="PgnGame"/>
public class PgnParser
{
    /// <summary>
    /// Parses a single PGN game from a string and returns a structured <see cref="PgnGame"/>.
    /// </summary>
    /// <param name="pgnText">
    /// A string containing the PGN text of exactly one game, including both the header section
    /// (tag pairs) and the move text section. The text may contain comments, variations, and
    /// annotations, which will be silently discarded.
    /// </param>
    /// <returns>
    /// A <see cref="PgnGame"/> containing the parsed headers, moves, and result. If the input
    /// contains no recognizable PGN tokens, the returned game will have empty headers, an empty
    /// move list, and an empty result string.
    /// </returns>
    /// <remarks>
    /// This method processes the entire input as a single game. To parse a file or string that
    /// contains multiple consecutive games, use <see cref="ParseMultipleGames"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var parser = new PgnParser();
    /// var game = parser.ParseGame("[Event \"Test\"]\n1. e4 e5 2. Nf3 Nc6 1-0");
    /// Console.WriteLine(game.Moves[0]); // "e4"
    /// Console.WriteLine(game.Result);   // "1-0"
    /// </code>
    /// </example>
    public PgnGame ParseGame(string pgnText)
    {
        var game = new PgnGame();
        IProcessingState state = new InitialState(game);
        foreach (var token in Tokenize(pgnText))
            state = state.Process(token);
        return game;
    }

    /// <summary>
    /// Parses multiple PGN games from a single string or file content.
    /// </summary>
    /// <param name="pgnText">
    /// A string containing one or more PGN games. Individual games are delimited by the
    /// <c>[Event ...]</c> tag that begins each game's header section. Text before the first
    /// <c>[Event ...]</c> tag is ignored.
    /// </param>
    /// <returns>
    /// A list of <see cref="PgnGame"/> objects, one per game found in the input, in the order
    /// they appear in the source text. Returns an empty list if no games are found.
    /// </returns>
    /// <remarks>
    /// This method splits the input by <c>[Event ...]</c> boundaries before parsing each segment
    /// independently via <see cref="ParseGame"/>. Blank segments between games are skipped.
    /// </remarks>
    /// <example>
    /// <code>
    /// var parser = new PgnParser();
    /// string pgnFileContent = File.ReadAllText("games.pgn");
    /// var games = parser.ParseMultipleGames(pgnFileContent);
    /// Console.WriteLine($"Parsed {games.Count} games.");
    /// </code>
    /// </example>
    public List<PgnGame> ParseMultipleGames(string pgnText)
    {
        var games = new List<PgnGame>();

        var gameTexts = SplitIntoGames(pgnText);

        foreach (var gameText in gameTexts)
        {
            if (!string.IsNullOrWhiteSpace(gameText))
            {
                games.Add(ParseGame(gameText));
            }
        }

        return games;
    }

    /// <summary>
    /// Splits PGN text into individual game strings by detecting <c>[Event ...]</c> boundaries.
    /// </summary>
    /// <param name="pgnText">The full PGN text that may contain one or more games.</param>
    /// <returns>
    /// A list of strings where each element contains the raw PGN text of one game,
    /// starting from its <c>[Event ...]</c> tag.
    /// </returns>
    private List<string> SplitIntoGames(string pgnText)
    {
        var games = new List<string>();
        var currentGame = new StringBuilder();
        var lines = pgnText.Split(['\r', '\n'], StringSplitOptions.None);

        var gameStarted = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // New game starts with [Event header
            if (trimmedLine.StartsWith("[Event "))
            {
                if (gameStarted && currentGame.Length > 0)
                {
                    games.Add(currentGame.ToString());
                    currentGame.Clear();
                }
                gameStarted = true;
            }

            if (gameStarted)
            {
                currentGame.AppendLine(line);
            }
        }

        // Add the last game
        if (currentGame.Length > 0)
        {
            games.Add(currentGame.ToString());
        }

        return games;
    }

    /// <summary>
    /// Tokenizes PGN text into a sequence of meaningful tokens, discarding comments, variations,
    /// and Numeric Annotation Glyphs in a single pass.
    /// </summary>
    /// <param name="pgnText">The raw PGN text to tokenize.</param>
    /// <returns>
    /// An enumerable sequence of token strings including bracket characters (<c>"["</c>,
    /// <c>"]"</c>), quoted strings (e.g., <c>"\"World Championship\""</c>), tag names,
    /// move numbers, move tokens, and result strings. Comments, variations, and NAGs are not
    /// yielded.
    /// </returns>
    private static IEnumerable<string> Tokenize(string pgnText)
    {
        var word = new StringBuilder();
        int i = 0;
        int len = pgnText.Length;

        while (i < len)
        {
            char c = pgnText[i];

            if (c == '[')
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                yield return "[";
                i++;
            }
            else if (c == ']')
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                yield return "]";
                i++;
            }
            else if (c == '"')
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                var quoted = new StringBuilder();
                quoted.Append('"');
                i++;
                while (i < len && pgnText[i] != '"')
                {
                    quoted.Append(pgnText[i]);
                    i++;
                }
                if (i < len) { quoted.Append('"'); i++; }
                yield return quoted.ToString();
            }
            else if (c == '{')
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                i++;
                while (i < len && pgnText[i] != '}') i++;
                if (i < len) i++;
            }
            else if (c == '(')
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                int depth = 1;
                i++;
                while (i < len && depth > 0)
                {
                    if (pgnText[i] == '(') depth++;
                    else if (pgnText[i] == ')') depth--;
                    i++;
                }
            }
            else if (c == '$')
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                i++;
                while (i < len && char.IsDigit(pgnText[i])) i++;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (word.Length > 0) { yield return word.ToString(); word.Clear(); }
                i++;
            }
            else
            {
                word.Append(c);
                i++;
            }
        }

        if (word.Length > 0) yield return word.ToString();
    }

    private interface IProcessingState
    {
        IProcessingState Process(string token);
    }

    private class InitialState(PgnGame game) : IProcessingState
    {
        public IProcessingState Process(string token)
        {
            if (token == "[")
                return new HeaderTagNameState(game);
            return new MoveTextState(game).Process(token);
        }
    }

    private class HeaderTagNameState(PgnGame game) : IProcessingState
    {
        public IProcessingState Process(string token) =>
            new HeaderTagValueState(game, token);
    }

    private class HeaderTagValueState(PgnGame game, string tagName) : IProcessingState
    {
        public IProcessingState Process(string token)
        {
            var value = token.Length >= 2 && token.StartsWith('"') && token.EndsWith('"')
                ? token[1..^1]
                : token;
            return new HeaderCloseState(game, tagName, value);
        }
    }

    private class HeaderCloseState(PgnGame game, string tagName, string tagValue) : IProcessingState
    {
        public IProcessingState Process(string token)
        {
            if (token == "]")
                game.Headers[tagName] = tagValue;
            return new InitialState(game);
        }
    }

    private class MoveTextState(PgnGame game) : IProcessingState
    {
        private static readonly Regex MoveNumberPattern = new(@"^\d+\.+$", RegexOptions.Compiled);
        private static readonly Regex MovePattern = new(@"^[NBRQK]?[a-h]?[1-8]?x?[a-h][1-8](=[NBRQ])?$", RegexOptions.Compiled);
        private static readonly Regex AnnotationPattern = new(@"[!?]+", RegexOptions.Compiled);
        private static readonly HashSet<string> Results = ["1-0", "0-1", "1/2-1/2", "*"];
        private static readonly HashSet<string> CastlingMoves = ["O-O", "O-O-O", "0-0", "0-0-0"];

        public IProcessingState Process(string token)
        {
            if (MoveNumberPattern.IsMatch(token))
                return this;

            if (Results.Contains(token))
            {
                game.Result = token;
                return new TerminalState();
            }

            if (IsValidMove(token))
                game.Moves.Add(CleanMove(token));

            return this;
        }

        private static bool IsValidMove(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var clean = AnnotationPattern.Replace(token, "").TrimEnd('+', '#');
            return CastlingMoves.Contains(clean) || MovePattern.IsMatch(clean);
        }

        private static string CleanMove(string move) =>
            AnnotationPattern.Replace(move, "");
    }

    private class TerminalState : IProcessingState
    {
        public IProcessingState Process(string token) => this;
    }
}
