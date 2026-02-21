namespace EngineUCI.Core.Conversions;

/// <summary>
/// Defines a stateful converter that translates chess moves from PGN (Portable Game Notation)
/// algebraic notation to LAN (Long Algebraic Notation) while maintaining an internal board
/// representation to resolve move disambiguation.
/// </summary>
/// <remarks>
/// <para>
/// PGN algebraic notation (e.g., <c>"Nf3"</c>, <c>"exd5"</c>, <c>"O-O"</c>) is compact but
/// ambiguous without board context, because it does not specify the source square. LAN
/// (e.g., <c>"g1f3"</c>, <c>"e4d5"</c>, <c>"e1g1"</c>) is unambiguous and is the format
/// accepted by UCI engine commands such as <c>position ... moves</c>.
/// </para>
/// <para>
/// Implementations must maintain an internal board state that is updated with each converted
/// move. Call <see cref="Reset"/> before replaying a new game or after an error to restore the
/// initial position.
/// </para>
/// </remarks>
/// <seealso cref="PgnToLanConverter"/>
public interface IPgnMoveConverter
{
    /// <summary>
    /// Resets the internal board representation to the standard chess starting position and
    /// sets the side to move to white.
    /// </summary>
    /// <remarks>
    /// Call this method before beginning to convert moves for a new game, or to recover
    /// from an invalid move that may have left the board in an inconsistent state.
    /// </remarks>
    public void Reset();

    /// <summary>
    /// Converts a single move from PGN algebraic notation to Long Algebraic Notation (LAN)
    /// and updates the internal board state to reflect the move.
    /// </summary>
    /// <param name="pgnMove">
    /// The move in PGN algebraic notation. Examples include:
    /// <list type="bullet">
    ///   <item><description>Pawn push: <c>"e4"</c>, <c>"d5"</c></description></item>
    ///   <item><description>Piece move: <c>"Nf3"</c>, <c>"Bb5"</c></description></item>
    ///   <item><description>Capture: <c>"exd5"</c>, <c>"Nxf7"</c></description></item>
    ///   <item><description>Kingside castling: <c>"O-O"</c> or <c>"0-0"</c></description></item>
    ///   <item><description>Queenside castling: <c>"O-O-O"</c> or <c>"0-0-0"</c></description></item>
    ///   <item><description>Promotion: <c>"e8=Q"</c>, <c>"a1=N"</c></description></item>
    ///   <item><description>Annotations (<c>!</c>, <c>?</c>) and check symbols (<c>+</c>, <c>#</c>) are stripped automatically.</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// The move in Long Algebraic Notation, consisting of the source square followed by the
    /// destination square (e.g., <c>"g1f3"</c>, <c>"e4d5"</c>, <c>"e1g1"</c>). For promotions
    /// the promotion piece is appended with an equals sign (e.g., <c>"e7e8=Q"</c>).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pgnMove"/> is <c>null</c>, empty, or consists only of
    /// whitespace.
    /// </exception>
    /// <exception cref="InvalidMoveException">
    /// Thrown when no legal piece of the appropriate type can reach the specified destination
    /// square from the current board state, indicating that the move is illegal or that the
    /// board is out of sync with the game being converted.
    /// </exception>
    public string ConvertMove(string pgnMove);
}
