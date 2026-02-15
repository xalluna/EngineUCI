namespace EngineUCI.Core;

/// <summary>
/// Converts chess moves from PGN (Portable Game Notation) to Long Algebraic Notation
/// </summary>
public class PgnToLongAlgebraicConverter
{
    private readonly Dictionary<char, int> fileToIndex = new()
    {
        {'a', 0}, {'b', 1}, {'c', 2}, {'d', 3}, {'e', 4}, {'f', 5}, {'g', 6}, {'h', 7}
    };

    private readonly Dictionary<int, char> indexToFile = new()
    {
        {0, 'a'}, {1, 'b'}, {2, 'c'}, {3, 'd'}, {4, 'e'}, {5, 'f'}, {6, 'g'}, {7, 'h'}
    };

    private char[,] board;
    private bool whiteToMove;

    public PgnToLongAlgebraicConverter()
    {
        board = InitializeBoard();
    }

    /// <summary>
    /// Initialize the chess board to starting position
    /// </summary>
    private char[,] InitializeBoard()
    {
        var board = new char[8, 8];
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

        return board;
    }

    /// <summary>
    /// Reset the board to starting position
    /// </summary>
    public void Reset()
    {
        board = InitializeBoard();
        whiteToMove = true;
    }

    /// <summary>
    /// Convert a PGN move to Long Algebraic Notation and update the board
    /// </summary>
    /// <param name="pgnMove">Move in PGN format (e.g., "Nf3", "exd5", "O-O")</param>
    /// <returns>Move in Long Algebraic Notation (e.g., "g1-f3", "e4xd5")</returns>
    public string ConvertMove(string pgnMove)
    {
        pgnMove = pgnMove.TrimEnd('+', '#', '!', '?').Trim();

        if (pgnMove == "O-O" || pgnMove == "0-0")
        {
            return HandleCastling(true);
        }
        if (pgnMove == "O-O-O" || pgnMove == "0-0-0")
        {
            return HandleCastling(false);
        }

        char piece = 'P'; // Default to pawn
        char? promotionPiece = null;
        bool isCapture = pgnMove.Contains('x');
        char? sourceFile = null;
        int? sourceRank = null;
        string destination = "";

        // Check for promotion
        if (pgnMove.Contains('='))
        {
            int promotionIndex = pgnMove.IndexOf('=');
            promotionPiece = pgnMove[promotionIndex + 1];
            pgnMove = pgnMove.Substring(0, promotionIndex);
            isCapture = pgnMove.Contains('x');
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
        string captureSymbol = isCapture ? "x" : "-";
        string longMove = $"{source}{captureSymbol}{dest}";

        if (promotionPiece.HasValue)
        {
            longMove += $"={promotionPiece}";
        }

        // Execute the move on the board
        ExecuteMove(srcRank, srcFile, destRank, destFile, promotionPiece);

        whiteToMove = !whiteToMove;

        return longMove;
    }

    private string HandleCastling(bool kingside)
    {
        int rank = whiteToMove ? 0 : 7;
        string result;

        if (kingside)
        {
            result = whiteToMove ? "e1-g1" : "e8-g8";
            // Move king
            board[rank, 6] = board[rank, 4];
            board[rank, 4] = '.';
            // Move rook
            board[rank, 5] = board[rank, 7];
            board[rank, 7] = '.';
        }
        else
        {
            result = whiteToMove ? "e1-c1" : "e8-c8";
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

    private (int rank, int file) FindSourceSquare(char piece, int destRank, int destFile,
        char? sourceFile, int? sourceRank)
    {
        char searchPiece = whiteToMove ? piece : char.ToLower(piece);
        List<(int rank, int file)> candidates = new List<(int, int)>();

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
            throw new Exception($"No valid piece found to move to {indexToFile[destFile]}{destRank + 1}");
        }

        return candidates[0];
    }

    private bool CanMoveTo(char piece, int srcRank, int srcFile, int destRank, int destFile)
    {
        piece = char.ToUpper(piece);

        return piece switch
        {
            'P' => CanPawnMoveTo(srcRank, srcFile, destRank, destFile),
            'N' => CanKnightMoveTo(srcRank, srcFile, destRank, destFile),
            'B' => CanBishopMoveTo(srcRank, srcFile, destRank, destFile),
            'R' => CanRookMoveTo(srcRank, srcFile, destRank, destFile),
            'Q' => CanQueenMoveTo(srcRank, srcFile, destRank, destFile),
            'K' => CanKingMoveTo(srcRank, srcFile, destRank, destFile),
            _ => false,
        };
    }

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

    private bool CanKnightMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        int rankDiff = Math.Abs(destRank - srcRank);
        int fileDiff = Math.Abs(destFile - srcFile);
        return (rankDiff == 2 && fileDiff == 1) || (rankDiff == 1 && fileDiff == 2);
    }

    private bool CanBishopMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        if (Math.Abs(destRank - srcRank) != Math.Abs(destFile - srcFile))
            return false;

        return IsPathClear(srcRank, srcFile, destRank, destFile);
    }

    private bool CanRookMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        if (srcRank != destRank && srcFile != destFile)
            return false;

        return IsPathClear(srcRank, srcFile, destRank, destFile);
    }

    private bool CanQueenMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        return CanRookMoveTo(srcRank, srcFile, destRank, destFile) ||
                CanBishopMoveTo(srcRank, srcFile, destRank, destFile);
    }

    private bool CanKingMoveTo(int srcRank, int srcFile, int destRank, int destFile)
    {
        return Math.Abs(destRank - srcRank) <= 1 && Math.Abs(destFile - srcFile) <= 1;
    }

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
    /// Print the current board state (for debugging)
    /// </summary>
    public void PrintBoard()
    {
        Console.WriteLine("  a b c d e f g h");
        for (int rank = 7; rank >= 0; rank--)
        {
            Console.Write($"{rank + 1} ");
            for (int file = 0; file < 8; file++)
            {
                Console.Write(board[rank, file] + " ");
            }
            Console.WriteLine($"{rank + 1}");
        }
        Console.WriteLine("  a b c d e f g h");
        Console.WriteLine(whiteToMove ? "White to move" : "Black to move");
    }
}

// Example usage
class Program
{
    static void Main(string[] args)
    {
        var converter = new PgnToLongAlgebraicConverter();

        // Example: Italian Game opening
        string[] pgnMoves = ["e4", "e5", "Nf3", "Nc6", "Bc4", "Bc5", "O-O", "Nf6", "d3", "d6"];

        Console.WriteLine("Converting PGN moves to Long Algebraic Notation:\n");

        foreach (var pgnMove in pgnMoves)
        {
            string longMove = converter.ConvertMove(pgnMove);
            Console.WriteLine($"{pgnMove,-10} -> {longMove}");
        }

        Console.WriteLine("\nFinal board position:");
        converter.PrintBoard();

        // Example with captures and more complex moves
        Console.WriteLine("\n\n=== Another example with captures ===\n");
        converter.Reset();

        string[] complexMoves = ["e4", "c5", "Nf3", "d6", "d4", "cxd4", "Nxd4", "Nf6", "Nc3", "a6"];

        foreach (var pgnMove in complexMoves)
        {
            string longMove = converter.ConvertMove(pgnMove);
            Console.WriteLine($"{pgnMove,-10} -> {longMove}");
        }

        Console.WriteLine("\nFinal board position:");
        converter.PrintBoard();
    }
}