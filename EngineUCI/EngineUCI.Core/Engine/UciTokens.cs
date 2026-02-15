namespace EngineUCI.Core.Engine;

public abstract class UciTokens
{
    public static class Commands
    {
        public const string Uci = "uci";
        public const string IsReady = "isready";
        public const string UciNewGame = "ucinewgame";
        public const string Position = "position";
        public const string Go = "go";
        public const string Stop = "stop";
        public const string PonderHit = "ponderhit";
        public const string Quit = "quit";
        public const string SetOption = "setoption";
    }
    
    public static class Position
    {
        public const string StartPos = "startpos";
        public const string Fen = "fen";
        public const string Moves = "moves";
    }
    
    public static class Go
    {
        public const string SearchMoves = "searchmoves";
        public const string Ponder = "ponder";
        public const string WTime = "wtime";
        public const string BTime = "btime";
        public const string WInc = "winc";
        public const string BInc = "binc";
        public const string MovesToGo = "movestogo";
        public const string Depth = "depth";
        public const string Nodes = "nodes";
        public const string Mate = "mate";
        public const string MoveTime = "movetime";
        public const string Infinite = "infinite";
    }
    
    public static class SetOption
    {
        public const string Name = "name";
        public const string Value = "value";
    }
    
    public static class Responses
    {
        public const string UciOk = "uciok";
        public const string ReadyOk = "readyok";
        public const string BestMove = "bestmove";
        public const string Info = "info";
        public const string Id = "id";
        public const string Option = "option";
        public const string CopyProtection = "copyprotection";
        public const string Registration = "registration";
    }
    
    public static class Info
    {
        public const string Depth = "depth";
        public const string SelDepth = "seldepth";
        public const string Time = "time";
        public const string Nodes = "nodes";
        public const string Pv = "pv";
        public const string MultiPv = "multipv";
        public const string Score = "score";
        public const string CurrMove = "currmove";
        public const string CurrMoveNumber = "currmovenumber";
        public const string HashFull = "hashfull";
        public const string Nps = "nps";
        public const string TbHits = "tbhits";
        public const string SbHits = "sbhits";
        public const string CpuLoad = "cpuload";
        public const string String = "string";
        public const string Refutation = "refutation";
        public const string CurrLine = "currline";
    }
    
    public static class Score
    {
        public const string Cp = "cp";
        public const string Mate = "mate";
        public const string LowerBound = "lowerbound";
        public const string UpperBound = "upperbound";
    }
    
    public static class Id
    {
        public const string Name = "name";
        public const string Author = "author";
    }
    
    public static class Option
    {
        public const string Name = "name";
        public const string Type = "type";
        public const string Default = "default";
        public const string Min = "min";
        public const string Max = "max";
        public const string Var = "var";
    }
    
    public static class OptionTypes
    {
        public const string Check = "check";
        public const string Spin = "spin";
        public const string Combo = "combo";
        public const string Button = "button";
        public const string String = "string";
    }
}