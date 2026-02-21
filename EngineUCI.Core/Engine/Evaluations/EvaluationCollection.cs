using System.Collections;

namespace EngineUCI.Core.Engine.Evaluations;

public class EvaluationCollection(IEnumerable<Evaluation> evaluations) : IEnumerable<Evaluation>
{
    private IReadOnlyList<Evaluation> Evaluations { get; } = [..evaluations.OrderBy(x => x.Rank)];

    public IEnumerator<Evaluation> GetEnumerator() => Evaluations.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Evaluation BestEvaluation => Evaluations[0];
}