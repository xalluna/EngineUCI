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
        
        var lines = pgnText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        
        var moveTextBuilder = new StringBuilder();
        var inMoveText = false;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                ParseHeader(trimmedLine, game);
            }
            else if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                inMoveText = true;
                moveTextBuilder.Append(trimmedLine + " ");
            }
        }
        
        if (inMoveText)
        {
            ParseMoveText(moveTextBuilder.ToString(), game);
        }
        
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
    /// Parse a single PGN header line
    /// </summary>
    private void ParseHeader(string headerLine, PgnGame game)
    {
        // Format: [TagName "TagValue"]
        var match = Regex.Match(headerLine, @"\[(\w+)\s+""([^""]*)""\]");
        
        if (match.Success)
        {
            string tagName = match.Groups[1].Value;
            string tagValue = match.Groups[2].Value;
            game.Headers[tagName] = tagValue;
        }
    }

    /// <summary>
    /// Parse the moveText section, extracting only actual moves
    /// </summary>
    private void ParseMoveText(string moveText, PgnGame game)
    {
        // Remove comments in curly braces { }
        moveText = Regex.Replace(moveText, @"\{[^}]*\}", " ");
        
        // Remove comments in parentheses (variations)
        moveText = RemoveNestedParentheses(moveText);
        
        // Remove NAG (Numeric Annotation Glyphs) like $1, $2, etc.
        moveText = Regex.Replace(moveText, @"\$\d+", " ");
        
        // Remove multiple spaces
        moveText = Regex.Replace(moveText, @"\s+", " ").Trim();
        
        // Split into tokens
        var tokens = moveText.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var token in tokens)
        {
            var cleanToken = token.Trim();
            
            // Skip move numbers (e.g., "1.", "2.", "15...")
            if (Regex.IsMatch(cleanToken, @"^\d+\.+$"))
                continue;
            
            // Check for result markers
            if (cleanToken == "1-0" || cleanToken == "0-1" || cleanToken == "1/2-1/2" || cleanToken == "*")
            {
                game.Result = cleanToken;
                continue;
            }
            
            // Check if it's a valid move
            if (IsValidMove(cleanToken))
            {
                // Remove trailing annotation symbols but keep the core move
                var move = CleanMove(cleanToken);
                game.Moves.Add(move);
            }
        }
    }

    /// <summary>
    /// Remove nested parentheses (variations) from moveText
    /// </summary>
    private string RemoveNestedParentheses(string text)
    {
        var result = new StringBuilder();
        int depth = 0;
        
        foreach (char c in text)
        {
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }
            else if (depth == 0)
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Check if a token represents a valid chess move
    /// </summary>
    private bool IsValidMove(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        
        // Remove annotation symbols for validation
        var cleanToken = Regex.Replace(token, @"[+#!?]+$", "");
        
        // Castling
        if (cleanToken == "O-O" || cleanToken == "O-O-O" || 
            cleanToken == "0-0" || cleanToken == "0-0-0")
            return true;
        
        // Regular move pattern: optional piece, optional file/rank, optional 'x', destination square, optional promotion
        // Examples: e4, Nf3, exd5, Nbd7, R1a3, e8=Q, axb8=N
        var movePattern = @"^[NBRQK]?[a-h]?[1-8]?x?[a-h][1-8](=[NBRQ])?$";
        
        return Regex.IsMatch(cleanToken, movePattern);
    }

    /// <summary>
    /// Clean a move by removing annotation symbols while keeping check/checkmate indicators
    /// </summary>
    private string CleanMove(string move)
    {
        // Keep +, # but remove !, ?, and combinations like !!, !?, ?!, ??
        return Regex.Replace(move, @"[!?]+", "");
    }
}