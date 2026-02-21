namespace EngineUCI.Core.Engine.Evaluations;

/// <summary>
/// Represents the evaluation result for a single principal variation at a specific search depth,
/// as reported by a UCI chess engine.
/// </summary>
/// <param name="Depth">
/// The search depth in plies (half-moves) at which this evaluation was produced.
/// Higher depths generally indicate more accurate evaluations but require more computation time.
/// </param>
/// <param name="Rank">
/// The one-based rank of this principal variation within a Multi-PV search.
/// A rank of <c>1</c> corresponds to the engine's best line; higher values correspond to
/// progressively weaker alternatives. In single-PV mode this value is always <c>1</c>.
/// </param>
/// <param name="Score">
/// The position score as reported by the engine. Typically expressed in centipawns
/// (e.g., <c>"34"</c> means white is better by 0.34 pawns), but may also be a mate score
/// (e.g., <c>"mate 3"</c> means forced checkmate in 3 moves). Positive values favor white;
/// negative values favor black.
/// </param>
/// <remarks>
/// <para>
/// <see cref="Evaluation"/> instances are produced by <see cref="EvaluationCollection"/> and
/// are returned from <see cref="../IUciEngine.EvaluateAsync(int, System.Threading.CancellationToken)"/>
/// after the engine completes its search.
/// </para>
/// <para>
/// The <see cref="Score"/> string is taken directly from the UCI <c>info score cp</c> token and
/// is not further parsed; callers that need numeric comparisons should parse it themselves.
/// </para>
/// </remarks>
/// <seealso cref="EvaluationCollection"/>
public record Evaluation(int Depth, int Rank, string Score);
