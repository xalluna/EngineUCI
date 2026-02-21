namespace EngineUCI.Core.Conversions;

public interface IPgnMoveConverter
{
     /// <summary>
    /// Reset the board to starting position
    /// </summary>
    public void Reset();

    /// <summary>
    /// Convert a PGN move to Long Algebraic Notation and update the board
    /// </summary>
    /// <param name="pgnMove">Move in PGN format (e.g., "Nf3", "exd5", "O-O")</param>
    /// <returns>Move in Long Algebraic Notation (e.g., "g1f3", "e4d5")</returns>
    public string ConvertMove(string pgnMove);
}