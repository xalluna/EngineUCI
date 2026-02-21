using System.Collections;

namespace EngineUCI.Core.Engine.Evaluations;

/// <summary>
/// Represents an ordered, read-only collection of <see cref="Evaluation"/> results returned by a
/// UCI engine search, sorted by principal variation rank from best to worst.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="EvaluationCollection"/> is produced at the end of each engine search and is
/// returned by <see cref="IUciEngine.EvaluateAsync(int, System.Threading.CancellationToken)"/>
/// and its overloads. In single-PV mode the collection contains exactly one entry; in Multi-PV
/// mode it contains one entry per requested principal variation (see
/// <see cref="IUciEngine.SetMultiPvAsync"/>).
/// </para>
/// <para>
/// The collection implements <see cref="IEnumerable{T}"/> so it can be used directly in
/// <c>foreach</c> loops and LINQ queries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evaluations = await engine.EvaluateAsync(depth: 20);
///
/// // Access the best line directly
/// Console.WriteLine($"Best score: {evaluations.BestEvaluation.Score}");
///
/// // Iterate over all principal variations
/// foreach (var eval in evaluations)
///     Console.WriteLine($"Rank {eval.Rank} (depth {eval.Depth}): {eval.Score}");
/// </code>
/// </example>
/// <seealso cref="Evaluation"/>
public class EvaluationCollection(IEnumerable<Evaluation> evaluations) : IEnumerable<Evaluation>
{
    /// <summary>
    /// The internal read-only list of evaluations, ordered ascending by <see cref="Evaluation.Rank"/>.
    /// </summary>
    private IReadOnlyList<Evaluation> Evaluations { get; } = [..evaluations.OrderBy(x => x.Rank)];

    /// <summary>
    /// Returns an enumerator that iterates over the <see cref="Evaluation"/> entries in rank order,
    /// starting from rank 1 (the engine's best line).
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<Evaluation> GetEnumerator() => Evaluations.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the highest-ranked evaluation, representing the engine's best assessed line of play.
    /// </summary>
    /// <value>
    /// The <see cref="Evaluation"/> with <see cref="Evaluation.Rank"/> equal to <c>1</c>.
    /// This is always the engine's preferred move at the deepest depth searched.
    /// </value>
    /// <remarks>
    /// In single-PV mode this is the only element in the collection. In Multi-PV mode this is
    /// the first of multiple ranked lines. Access to this property assumes the collection is
    /// non-empty; it will throw an <see cref="ArgumentOutOfRangeException"/> if the collection
    /// was constructed from an empty sequence.
    /// </remarks>
    public Evaluation BestEvaluation => Evaluations[0];
}
