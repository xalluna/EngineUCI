namespace EngineUCI.Core.Conversions;

/// <summary>
/// Converts chess moves from PGN (Portable Game Notation) algebraic notation to LAN
/// (Long Algebraic Notation) by maintaining a lightweight internal board representation.
/// </summary>
/// <remarks>
/// <para>
/// PGN moves such as <c>"Nf3"</c> or <c>"exd5"</c> omit the source square and require
/// knowledge of the current board position to be resolved unambiguously. This class maintains
/// an 8x8 character board, updated after each move, so that every subsequent <see cref="ConvertMove"/>
/// call has the context needed to locate the moving piece and produce the equivalent LAN string
/// (e.g., <c>"g1f3"</c>, <c>"e4d5"</c>) accepted by UCI engine commands.
/// </para>
/// <para>
/// The converter supports all standard move types: pawn pushes, pawn captures, piece moves
/// with optional file or rank disambiguation, promotions, and both castling variants. Check
/// (<c>+</c>), checkmate (<c>#</c>), and annotation symbols (<c>!</c>, <c>?</c>) are stripped
/// automatically before processing.
/// </para>
/// <para>
/// This implementation performs syntactic conversion only. It does not validate move legality
/// beyond determining whether a piece of the correct type can physically reach the destination
/// square. En-passant captures are not currently handled.
/// </para>
/// </remarks>
/// <seealso cref="IPgnMoveConverter"/>
/// <seealso cref="InvalidMoveException"/>
public class PgnToLanConverter : IPgnMoveConverter
{
    private readonly Dictionary<char, int> fileToIndex = new()
    {
        {'a', 0}, {'b', 1}, {'c', 2}, {'d', 3}, {'e', 4}, {'f', 5}, {'g', 6}, {'h', 7}
    };

    private readonly Dictionary<int, char> indexToFile = new()
    {
        {0, 'a'}, {1, 'b'}, {2, 'c'}, {3, 'd'}, {4, 'e'}, {5, 'f'}, {6, 'g'}, {7, 'h'}
    };

    private char[,] board = new char[8, 8];
    private bool whiteToMove = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgnToLanConverter"/> class with the board
    /// set to the standard chess starting position and white to move.
    /// </summary>
    public PgnToLanConverter()
    {
        InitializeBoard();
    }

    /// <summary>
    /// Initializes the internal board array to the standard chess starting position.
    /// </summary>
    /// <remarks>
    /// White pieces occupy ranks 1 and 2 (array indices 0 and 1); black pieces occupy ranks
    /// 7 and 8 (indices 6 and 7). Empty squares are represented by the <c>'.'</c> character.
    /// </remarks>
    private void InitializeBoard()
    {
        board = new char[8, 8];
        whiteToMove = true;

        // Empty squares
        for (int rank = 2; rank < 6; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                board[rank, file] = '.';
            }
        }

        // White pieces (rank 0 = rank 1 in chess notation)
        board[0, 0] = 'R'; board[0, 1] = 'N'; board[0, 2] = 'B'; board[0, 3] = 'Q';
        board[0, 4] = 'K'; board[0, 5] = 'B'; board[0, 6] = 'N'; board[0, 7] = 'R';
        for (int file = 0; file < 8; file++)
            board[1, file] = 'P';

        // Black pieces (rank 7 = rank 8 in chess notation)
        board[7, 0] = 'r'; board[7, 1] = 'n'; board[7, 2] = 'b'; board[7, 3] = 'q';
        board[7, 4] = 'k'; board[7, 5] = 'b'; board[7, 6] = 'n'; board[7, 7] = 'r';
        for (int file = 0; file < 8; file++)
            board[6, file] = 'p';
    }

    /// <summary>
    /// Resets the internal board to the standard chess starting position and sets the side to
    /// move back to white.
    /// </summary>
    /// <remarks>
    /// Call this method before converting moves for a new game, or to recover from an
    /// <see cref="InvalidMoveException"/> that may have left the board in an inconsistent state.
    /// </remarks>
    public void Reset()
    {
        InitializeBoard();
        whiteToMove = true;
    }

    /// <summary>
    /// Converts a single move from PGN algebraic notation to Long Algebraic Notation (LAN)
    /// and updates the internal board state to reflect the move.
    /// </summary>
    /// <param name="pgnMove">
    /// The move in PGN algebraic notation. Check (<c>+</c>), checkmate (<c>#</c>), and
    /// annotation symbols (<c>!</c>, <c>?</c>) are stripped automatically. Examples:
    /// <c>"Nf3"</c>, <c>"exd5"</c>, <c>"O-O"</c>, <c>"e8=Q"</c>.
    /// </param>
    /// <returns>
    /// The move in Long Algebraic Notation, e.g., <c>"g1f3"</c>, <c>"e4d5"</c>, <c>"e1g1"</c>.
    /// For promotions the promotion piece is appended with an equals sign, e.g., <c>"e7e8=Q"</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pgnMove"/> is <c>null</c>, empty, or whitespace-only.
    /// </exception>
    /// <exception cref="InvalidMoveException">
    /// Thrown when no piece of the appropriate type can reach the destination square from the
    /// current board state, which typically indicates an illegal move or an out-of-sync board.
    /// </exception>
    /// <example>
    /// <code>
    /// var converter = new PgnToLanConverter();
    /// string lan = converter.ConvertMove("e4");   // returns "e2e4"
    /// lan = converter.ConvertMove("e5");           // returns "e7e5"
    /// lan = converter.ConvertMove("Nf3");          // returns "g1f3"
    /// </code>
    /// </example>
    public string ConvertMove(string pgnMove)
    {
        if (string.IsNullOrWhiteSpace(pgnMove))
        {
            throw new ArgumentException("Move cannot be null or empty", nameof(pgnMove));
        }

        // Remove check (+) and checkmate (#) symbols
        pgnMove = pgnMove.TrimEnd('+', '#', '!', '?').Trim();

        // Handle castling
        if (pgnMove == "O-O" || pgnMove == "0-0")
        {
            string result = HandleCastling(true);
            return result;
        }
        if (pgnMove == "O-O-O" || pgnMove == "0-0-0")
        {
            string result = HandleCastling(false);
            return result;
        }

        // Parse the move
        char piece = 'P'; // Default to pawn
        char? promotionPiece = null;
        char? sourceFile = null;
        int? sourceRank = null;
        string destination = "";

        // Check for promotion
        if (pgnMove.Contains('='))
        {
            int promotionIndex = pgnMove.IndexOf('=');
            promotionPiece = pgnMove[promotionIndex + 1];
            pgnMove = pgnMove.Substring(0, promotionIndex);
        }

        // Remove 'x' for parsing
        string moveWithoutCapture = pgnMove.Replace("x", "");

        // Check if first character is a piece (uppercase letter)
        if (char.IsUpper(moveWithoutCapture[0]) && moveWithoutCapture[0] != 'O')
        {
            piece = moveWithoutCapture[0];
            moveWithoutCapture = moveWithoutCapture.Substring(1);
        }

        // Extract destination (last 2 characters should be file+rank)
        if (moveWithoutCapture.Length >= 2)
        {
            destination = moveWithoutCapture.Substring(moveWithoutCapture.Length - 2);
            moveWithoutCapture = moveWithoutCapture.Substring(0, moveWithoutCapture.Length - 2);
        }

        // Parse disambiguation (remaining characters)
        if (moveWithoutCapture.Length > 0)
        {
            foreach (char c in moveWithoutCapture)
            {
                if (char.IsLetter(c))
                {
                    sourceFile = c;
                }
                else if (char.IsDigit(c))
                {
                    sourceRank = c - '0';
                }
            }
        }

        // Find the source square
        int destFile = fileToIndex[destination[0]];
        int destRank = destination[1] - '1';

        (int srcRank, int srcFile) = FindSourceSquare(piece, destRank, destFile, sourceFile, sourceRank);

        // Build long algebraic notation
        string source = $"{indexToFile[srcFile]}{srcRank + 1}";
        string dest = $"{indexToFile[destFile]}{destRank + 1}";
        string longMove = $"{source}{dest}";

        if (promotionPiece.HasValue)
        {
            longMove += $"={promotionPiece}";
        }

        // Execute the move on the board
        ExecuteMove(srcRank, srcFile, destRank, destFile, promotionPiece);

        whiteToMove = !whiteToMove;

        return longMove;
    }

    /// <summary>
    /// Handles a castling move, updates the internal board state for both the king and rook,
    /// and returns the corresponding LAN string.
    /// </summary>
    /// <param name="kingside">
    /// <c>true</c> for kingside castling (O-O); <c>false</c> for queenside castling (O-O-O).
    /// </param>
    /// <returns>
    /// The LAN representation of the king's castling move:
    /// <c>"e1g1"</c> (white kingside), <c>"e8g8"</c> (black kingside),
    /// <c>"e1c1"</c> (white queenside), or <c>"e8c8"</c> (black queenside).
    /// </returns>
    private string HandleCastling(bool kingside)
    {
        int rank = whiteToMove ? 0 : 7;
        string result;

        if (kingside)
        {
            result = whiteToMove ? "e1g1" : "e8g8";
            // Move king
            board[rank, 6] = board[rank, 4];
            board[rank, 4] = '.';
            // Move rook
            board[rank, 5] = board[rank, 7];
            board[rank, 7] = '.';
        }
        else
        {
            result = whiteToMove ? "e1c1" : "e8c8";
            // Move king
            board[rank, 2] = board[rank, 4];
            board[rank, 4] = '.';
            // Move rook
            board[rank, 3] = board[rank, 0];
            board[rank, 0] = '.';
        }

        whiteToMove = !whiteToMove;
        return result;
    }

    /// <summary>
    /// Searches the board for the source square of the piece that can legally move to the
    /// specified destination, optionally filtered by a known source file or rank.
    /// </summary>
    /// <param name="piece">
    /// The piece type as an uppercase character: <c>'P'</c>, <c>'N'</c>, <c>'B'</c>,
    /// <c>'R'</c>, <c>'Q'</c>, or <c>'K'</c>.
    /// </param>
    /// <param name="destRank">Zero-based destination rank (0 = rank 1, 7 = rank 8).</param>
    /// <param name="destFile">Zero-based destination file (0 = a-file, 7 = h-file).</param>
    /// <param name="sourceFile">
    /// Optional disambiguation file character (e.g., <c>'g'</c> in <c>"Ngf3"</c>),
    /// or <c>null</c> if not provided.
    /// </param>
    /// <param name="sourceRank">
    /// Optional disambiguation rank number (e.g., <c>1</c> in <c>"R1e5"</c>),
    /// or <c>null</c> if not provided.
    /// </param>
    /// <returns>
    /// A tuple containing the zero-based <c>(rank, file)</c> of the first matching source square.
    /// </returns>
    /// <exception cref="InvalidMoveException">
    /// Thrown when no piece of the specified type can reach the destination square from the
    /// current board state under the given disambiguation constraints.
    /// </exception>
    private (int rank, int file) FindSourceSquare(char piece, int destRank, int destFile,
        char? sourceFile, int? sourceRank)
    {
        char searchPiece = whiteToMove ? piece : char.ToLower(piece);
        List<(int rank, int file)> candidates = [];

        // Find all pieces of the correct type
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                if (board[rank, file] == searchPiece)
                {
                    if (sourceFile.HasValue && fileToIndex[sourceFile.Value] != file)
                        continue;
                    if (sourceRank.HasValue && sourceRank.Value - 1 != rank)
                        continue;

                    if (CanMoveTo(piece, rank, file, destRank, destFile))
                    {
                        candidates.Add((rank, file));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            throw new InvalidMoveException($"No valid piece found to move to {indexToFile[destFile]}{destRank + 1}");
        }

        return candidates[0];
    }

    /// <summary>
    /// Determines whether a piece of the specified type on a given source square can
    /// reach the specified destination square according to standard chess movement rules.
    /// </summary>
    /// <param name="piece">
    /// The piece type as an uppercase character: <c>'P'</c>, <c>'N'</c>, <c>'B'</c>,
    /// <c>'R'</c>, <c>'Q'</c>, or <c>'K'</c>.
    /// </param>
    /// <param name="srcRank">Zero-based source rank.</param>
    /// <param name="srcFile">Zero-based source file.</param>
    /// <param name="destRank">Zero-based destination rank.</param>
    /// <param name="destFile">Zero-based destination file.</param>
    /// <returns>
    /// <c>true</c> if the piece can move from the source to the destination; otherwise,
    /// <c>false</c>.
    /// </returns>
    private bool CanMoveTo(char piece, int srcRank, int srcFile, int destRank, int destFile)
    {
        piece = char.ToUpper(piece);

        switch (piece)
        {
            case 'P':
                return CanPawnMoveTo(srcRank, srcFile, destRank, destFile);
            case 'N':
                return CanKnightMoveTo(srcRank, srcFile, destRank, destFile);
            case 'B':
                return CanBishopMoveTo(srcRank, srcFile, destRank, destFile);
            case 'R':
                return CanRookMoveTo(srcRank, srcFile, destRank, destFile);
            case 'Q':
                return CanQueenMoveTo(srcRank, srcFile, destRank, destFile);
            case 'K':
                return CanKingMoveTo(srcRank, srcFile, destRank, destFile);
            default:
                return false;
        }
    }

    /// <summary>
    /// Determines whether a pawn on the given source square can move to the destination square,
    /// considering single pushes, double pushes from the starting rank, and diagonal captures.
    /// </summary>
    private bool CanPawnMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        int direction = whiteToMove ? 1 : -1;
        int startRank = whiteToMove ? 1 : 6;

        // Capture
        if (Math.Abs(destFile - srcFile) == 1 && destRank - srcRank == direction)
        {
            return board[destRank, destFile] != '.';
        }

        // Single push
        if (srcFile == destFile && destRank - srcRank == direction)
        {
            return board[destRank, destFile] == '.';
        }

        // Double push
        if (srcFile == destFile && srcRank == startRank && destRank - srcRank == 2 * direction)
        {
            return board[destRank, destFile] == '.' && board[srcRank + direction, srcFile] == '.';
        }

        return false;
    }

    /// <summary>
    /// Determines whether a knight on the given source square can reach the destination square
    /// using its L-shaped movement pattern.
    /// </summary>
    private bool CanKnightMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        int rankDiff = Math.Abs(destRank - srcRank);
        int fileDiff = Math.Abs(destFile - srcFile);
        return (rankDiff == 2 && fileDiff == 1) || (rankDiff == 1 && fileDiff == 2);
    }

    /// <summary>
    /// Determines whether a bishop on the given source square can reach the destination square
    /// along a clear diagonal path.
    /// </summary>
    private bool CanBishopMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        if (Math.Abs(destRank - srcRank) != Math.Abs(destFile - srcFile))
            return false;

        return IsPathClear(srcRank, srcFile, destRank, destFile);
    }

    /// <summary>
    /// Determines whether a rook on the given source square can reach the destination square
    /// along a clear rank or file.
    /// </summary>
    private bool CanRookMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        if (srcRank != destRank && srcFile != destFile)
            return false;

        return IsPathClear(srcRank, srcFile, destRank, destFile);
    }

    /// <summary>
    /// Determines whether a queen on the given source square can reach the destination square
    /// along a clear rank, file, or diagonal.
    /// </summary>
    private bool CanQueenMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        return CanRookMoveTo(srcRank, srcFile, destRank, destFile) ||
                CanBishopMoveTo(srcRank, srcFile, destRank, destFile);
    }

    /// <summary>
    /// Determines whether a king on the given source square can move one step in any direction
    /// to the destination square.
    /// </summary>
    private bool CanKingMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        return Math.Abs(destRank - srcRank) <= 1 && Math.Abs(destFile - srcFile) <= 1;
    }

    /// <summary>
    /// Determines whether all squares between the source and destination are empty, enabling
    /// sliding piece movement verification for bishops, rooks, and queens.
    /// </summary>
    /// <param name="srcRank">Zero-based source rank.</param>
    /// <param name="srcFile">Zero-based source file.</param>
    /// <param name="destRank">Zero-based destination rank.</param>
    /// <param name="destFile">Zero-based destination file.</param>
    /// <returns>
    /// <c>true</c> if every square strictly between the source and destination is empty
    /// (represented by <c>'.'</c>); otherwise, <c>false</c>.
    /// </returns>
    private bool IsPathClear(int srcRank, int srcFile, int destRank, int destFile)
    {
        int rankStep = Math.Sign(destRank - srcRank);
        int fileStep = Math.Sign(destFile - srcFile);

        int currentRank = srcRank + rankStep;
        int currentFile = srcFile + fileStep;

        while (currentRank != destRank || currentFile != destFile)
        {
            if (board[currentRank, currentFile] != '.')
                return false;

            currentRank += rankStep;
            currentFile += fileStep;
        }

        return true;
    }

    /// <summary>
    /// Applies a move to the internal board by moving the piece character from the source square
    /// to the destination square and replacing the source with an empty-square marker.
    /// Handles pawn promotion by replacing the pawn character with the promotion piece character.
    /// </summary>
    /// <param name="srcRank">Zero-based source rank.</param>
    /// <param name="srcFile">Zero-based source file.</param>
    /// <param name="destRank">Zero-based destination rank.</param>
    /// <param name="destFile">Zero-based destination file.</param>
    /// <param name="promotionPiece">
    /// The uppercase promotion piece character (e.g., <c>'Q'</c>), or <c>null</c> for non-promotion moves.
    /// For black promotions the character is automatically converted to lowercase.
    /// </param>
    private void ExecuteMove(int srcRank, int srcFile, int destRank, int destFile, char? promotionPiece)
    {
        char piece = board[srcRank, srcFile];

        if (promotionPiece.HasValue)
        {
            piece = whiteToMove ? promotionPiece.Value : char.ToLower(promotionPiece.Value);
        }

        board[destRank, destFile] = piece;
        board[srcRank, srcFile] = '.';
    }

    /// <summary>
    /// Prints the current board state to the console with colored output for debugging purposes.
    /// </summary>
    /// <remarks>
    /// White pieces are displayed in white text, black pieces in black text. Board squares
    /// alternate between white and blue backgrounds to represent light and dark squares.
    /// File labels (a–h) are printed above and below the board; rank labels (1–8) are printed
    /// on each side. The side to move is displayed below the board in green.
    /// </remarks>
    public void PrintBoard()
    {
        PrintFileLabels();

        for (int rank = 7; rank >= 0; rank--)
        {
            PrintRankLabel(rank + 1);

            for (int file = 0; file < 8; file++)
                PrintSquare(rank, file);

            PrintRankLabel(rank + 1);
            Console.WriteLine();
        }

        PrintFileLabels();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(whiteToMove ? "White to move" : "Black to move");
        Console.ResetColor();
    }

    private void PrintFileLabels()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  a b c d e f g h");
        Console.ResetColor();
    }

    private void PrintRankLabel(int rank)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{rank} ");
        Console.ResetColor();
    }

    private void PrintSquare(int rank, int file)
    {
        var square = board[rank, file];
        var isEmpty = square == '.';
        var isWhiteSquare = (file + rank) % 2 == 0;
        var isWhitePiece = 'A' <= square && square <= 'Z';

        var color = isEmpty && !isWhiteSquare
            ? ConsoleColor.Black
            : !isEmpty && !isWhitePiece
                ? ConsoleColor.Black
                : ConsoleColor.White;

        var backgroundColor = isWhiteSquare ? ConsoleColor.White : ConsoleColor.Blue;

        Console.ForegroundColor = color;
        Console.BackgroundColor = backgroundColor;
        Console.Write(isEmpty ? " " : board[rank, file]);
        Console.Write(" ");
        Console.ResetColor();
    }
}

/// <summary>
/// The exception that is thrown when a PGN move cannot be converted to LAN because no legal
/// piece of the appropriate type can reach the specified destination square from the current
/// board state.
/// </summary>
/// <param name="message">
/// A message that describes the invalid move condition, typically including the destination
/// square that could not be reached (e.g., <c>"No valid piece found to move to f3"</c>).
/// </param>
/// <remarks>
/// This exception typically indicates one of the following conditions:
/// <list type="bullet">
///   <item><description>The PGN move is illegal given the current position.</description></item>
///   <item><description>The internal board state is out of sync because a previous move was
///   not converted through the same <see cref="PgnToLanConverter"/> instance.</description></item>
///   <item><description>The PGN source contains an error such as a missing or incorrect move.</description></item>
/// </list>
/// Call <see cref="PgnToLanConverter.Reset"/> to restore the board to the starting position
/// before retrying the conversion sequence.
/// </remarks>
/// <seealso cref="PgnToLanConverter"/>
public class InvalidMoveException(string message) : Exception(message);
