using System.Linq.Expressions;
using System.Reflection;
using EngineUCI.Core.Engine;

namespace EngineUCI.Core.Parsing;

// ex info depth 20 seldepth 33 multipv 1 score cp 34 nodes 782174 nps 1092421 hashfull 309 tbhits 0 time 716 
// pv e2e4 e7e5 g1f3 b8c6 f1b5 g8f6 e1g1 f6e4 f1e1 e4d6 f3e5 c6e5 b1c3 f8e7 e1e5 e8g8 b5f1 e7f6 e5e1 d6f5 c3d5 f6h4 d2d4 c7c6 d5f4 d7d5 g2g3 h4e7 c2c3 c8d7 a2a4

public class UciInfoResponseParser
{
    public UciInfoLine InfoLine { get; set; } = new();
    
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

public class UciInfoLine
{
    public int Depth { get; set; }
    public int? SelDepth { get; set; }
    public int MultiPv { get; set; }
    public string Score { get; set; } = string.Empty;
    public int? Nodes { get; set; }
    public int? Nps { get; set; }
    public int? HashFull { get; set; }
    public int? TbHits { get; set; }
    public int? TimeMs { get; set; }
    public string? Pv { get; set; }
}
