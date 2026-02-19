using System.Linq.Expressions;
using System.Reflection;
using EngineUCI.Core.Engine;

namespace EngineUCI.Core.Parsing;

// ex info depth 20 seldepth 33 multipv 1 score cp 34 nodes 782174 nps 1092421 hashfull 309 tbhits 0 time 716 
// pv e2e4 e7e5 g1f3 b8c6 f1b5 g8f6 e1g1 f6e4 f1e1 e4d6 f3e5 c6e5 b1c3 f8e7 e1e5 e8g8 b5f1 e7f6 e5e1 d6f5 c3d5 f6h4 d2d4 c7c6 d5f4 d7d5 g2g3 h4e7 c2c3 c8d7 a2a4

/// <summary>
/// Parses UCI <c>info</c> response strings into structured <see cref="UciInfoLine"/> objects.
/// </summary>
public class UciInfoResponseParser
{
    public UciInfoLine InfoLine { get; set; } = new();

    /// <summary>
    /// Parses a UCI <c>info</c> response string into a structured <see cref="UciInfoLine"/> object.
    /// </summary>
    /// <param name="infoResponse">The raw UCI info response string to parse.</param>
    /// <returns>A <see cref="UciInfoLine"/> containing the parsed values from the response.</returns>
    public UciInfoLine Parse(string infoResponse)
    {
        var tokens = infoResponse.Split(' ');
        IProcessingState state = new SearchProcessingState(InfoLine);

        foreach(var token in tokens) state = state.Process(token);

        var infoLine = InfoLine;
        InfoLine = new();
        return infoLine;
    }

    private interface IProcessingState
    {
        IProcessingState Process(string data);
    }

    private class SearchProcessingState(UciInfoLine infoLine) : IProcessingState
    {
        public IProcessingState Process(string data)
        {
            return data switch
            {
                UciTokens.Info.Depth => new IntProcessingState(infoLine, x => x.Depth),
                UciTokens.Info.SelDepth => new IntProcessingState(infoLine, x => x.SelDepth),
                UciTokens.Info.MultiPv => new IntProcessingState(infoLine, x => x.MultiPv),
                UciTokens.Info.Nodes => new IntProcessingState(infoLine, x => x.Nodes),
                UciTokens.Info.HashFull => new IntProcessingState(infoLine, x => x.HashFull),
                UciTokens.Info.TbHits => new IntProcessingState(infoLine, x => x.TbHits),
                UciTokens.Info.Time => new IntProcessingState(infoLine, x => x.TimeMs),
                UciTokens.Info.Cp => new StringProcessingState(infoLine, x => x.Score),
                UciTokens.Info.Pv => new PvProcessingState(infoLine),
                _ => this
            };
        }
    }

    private class IntProcessingState : IProcessingState
    {
        private PropertyInfo Property { get; init; }
        private UciInfoLine InfoLine { get; init; }

        public IntProcessingState(UciInfoLine infoLine, Expression<Func<UciInfoLine, int?>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a property access.");

            if (member.Member is not PropertyInfo prop)
                throw new ArgumentException("Member is not a property.");

            Property = prop;
            InfoLine = infoLine;
        }

        public IntProcessingState(UciInfoLine infoLine, Expression<Func<UciInfoLine, int>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a property access.");

            if (member.Member is not PropertyInfo prop)
                throw new ArgumentException("Member is not a property.");

            Property = prop;
            InfoLine = infoLine;
        }

        public IProcessingState Process(string data)
        {
            Property.SetValue(InfoLine, int.Parse(data));
            return new SearchProcessingState(InfoLine);
        }
    }

    private class StringProcessingState : IProcessingState
    {
        private PropertyInfo Property { get; init; }
        private UciInfoLine InfoLine { get; init; }

        public StringProcessingState(UciInfoLine infoLine, Expression<Func<UciInfoLine, string?>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a property access.");

            if (member.Member is not PropertyInfo prop)
                throw new ArgumentException("Member is not a property.");

            Property = prop;
            InfoLine = infoLine;
        }

        public IProcessingState Process(string data)
        {
            Property.SetValue(InfoLine, data);
            return new SearchProcessingState(InfoLine);
        }
    }

    private class PvProcessingState(UciInfoLine infoLine) : IProcessingState
    {
        public IProcessingState Process(string data)
        {
            infoLine.Pv += $" {data}";
            return this;
        }
    }
}

/// <summary>
/// Represents a parsed UCI <c>info</c> response line containing search statistics and evaluation data.
/// </summary>
public class UciInfoLine
{
    /// <summary>Current search depth in plies.</summary>
    public int Depth { get; set; }

    /// <summary>Selective search depth in plies.</summary>
    public int? SelDepth { get; set; }

    /// <summary>Principal variation index (1-based) in Multi-PV mode.</summary>
    public int MultiPv { get; set; }

    /// <summary>Position evaluation score in centipawns.</summary>
    public string Score { get; set; } = string.Empty;

    /// <summary>Total number of nodes searched.</summary>
    public int? Nodes { get; set; }

    /// <summary>Search speed in nodes per second.</summary>
    public int? Nps { get; set; }

    /// <summary>Hash table utilization in per mille (0–1000).</summary>
    public int? HashFull { get; set; }

    /// <summary>Number of tablebase hits during search.</summary>
    public int? TbHits { get; set; }

    /// <summary>Time spent searching in milliseconds.</summary>
    public int? TimeMs { get; set; }

    /// <summary>Principal variation as a space-separated sequence of moves.</summary>
    public string? Pv { get; set; }
}
