using System.Text;
using System.Text.RegularExpressions;

namespace EngineUCI.Core.Parsing;

/// <summary>
/// Represents a parsed PGN game with metadata and moves
/// </summary>
public class PgnGame
{
    public Dictionary<string, string> Headers { get; set; } = [];
    public List<string> Moves { get; set; } = [];
    public string Result { get; set; } = string.Empty;

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
/// Parser for PGN (Portable Game Notation) format
/// Handles headers, moves, annotations, comments, variations, and NAGs
/// </summary>
public class PgnParser
{
    /// <summary>
    /// Parse a single PGN game from a string
    /// </summary>
    /// <param name="pgnText">PGN text containing one game</param>
    /// <returns>Parsed PGN game</returns>
    public PgnGame ParseGame(string pgnText)
    {
        var game = new PgnGame();
        IProcessingState state = new InitialState(game);
        foreach (var token in Tokenize(pgnText))
            state = state.Process(token);
        return game;
    }

    /// <summary>
    /// Parse multiple PGN games from a string or file content
    /// </summary>
    /// <param name="pgnText">PGN text containing one or more games</param>
    /// <returns>List of parsed PGN games</returns>
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
    /// Split PGN text into individual game strings
    /// </summary>
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
    /// Tokenize PGN text into a sequence of tokens, skipping comments, variations, and NAGs
    /// </summary>
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
