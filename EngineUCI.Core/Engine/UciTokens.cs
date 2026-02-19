namespace EngineUCI.Core.Engine;

/// <summary>
/// Defines constants for Universal Chess Interface (UCI) protocol tokens.
/// Contains all standard UCI commands, parameters, and response tokens as specified in the UCI protocol.
/// </summary>
public abstract class UciTokens
{
    /// <summary>
    /// Contains UCI command tokens that are sent from the GUI to the engine.
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Tells the engine to use the UCI protocol. The engine must respond with "uciok".
        /// </summary>
        public const string Uci = "uci";

        /// <summary>
        /// Synchronizes the engine and GUI. The engine must respond with "readyok" when ready.
        /// </summary>
        public const string IsReady = "isready";

        /// <summary>
        /// Tells the engine to start a new game and reset internal state.
        /// </summary>
        public const string UciNewGame = "ucinewgame";

        /// <summary>
        /// Sets up the position on the board. Used with "startpos" or "fen" followed by moves.
        /// </summary>
        public const string Position = "position";

        /// <summary>
        /// Starts calculating on the current position with specified search parameters.
        /// </summary>
        public const string Go = "go";

        /// <summary>
        /// Stops calculating as soon as possible and returns the best move found so far.
        /// </summary>
        public const string Stop = "stop";

        /// <summary>
        /// Tells the engine that the user has played the expected move while pondering.
        /// </summary>
        public const string PonderHit = "ponderhit";

        /// <summary>
        /// Tells the engine to quit as soon as possible.
        /// </summary>
        public const string Quit = "quit";

        /// <summary>
        /// Sets an engine option. Must be followed by "name" and optionally "value".
        /// </summary>
        public const string SetOption = "setoption";
    }

    /// <summary>
    /// Contains position-related tokens used with the "position" command.
    /// </summary>
    public static class Position
    {
        /// <summary>
        /// Indicates the standard chess starting position (rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1).
        /// </summary>
        public const string StartPos = "startpos";

        /// <summary>
        /// Indicates that a FEN (Forsyth-Edwards Notation) string follows to describe the position.
        /// </summary>
        public const string Fen = "fen";

        /// <summary>
        /// Indicates that a list of moves in algebraic notation follows.
        /// </summary>
        public const string Moves = "moves";
    }

    /// <summary>
    /// Contains search parameter tokens used with the "go" command.
    /// </summary>
    public static class Go
    {
        /// <summary>
        /// Restricts search to only the specified moves.
        /// </summary>
        public const string SearchMoves = "searchmoves";

        /// <summary>
        /// Tells the engine to start pondering (thinking on opponent's time).
        /// </summary>
        public const string Ponder = "ponder";

        /// <summary>
        /// Specifies white's remaining time on the clock in milliseconds.
        /// </summary>
        public const string WTime = "wtime";

        /// <summary>
        /// Specifies black's remaining time on the clock in milliseconds.
        /// </summary>
        public const string BTime = "btime";

        /// <summary>
        /// Specifies white's time increment per move in milliseconds.
        /// </summary>
        public const string WInc = "winc";

        /// <summary>
        /// Specifies black's time increment per move in milliseconds.
        /// </summary>
        public const string BInc = "binc";

        /// <summary>
        /// Specifies the number of moves to the next time control.
        /// </summary>
        public const string MovesToGo = "movestogo";

        /// <summary>
        /// Limits search to the specified depth in plies (half-moves).
        /// </summary>
        public const string Depth = "depth";

        /// <summary>
        /// Limits search to the specified number of nodes.
        /// </summary>
        public const string Nodes = "nodes";

        /// <summary>
        /// Searches for a mate in the specified number of moves.
        /// </summary>
        public const string Mate = "mate";

        /// <summary>
        /// Limits search to exactly the specified time in milliseconds.
        /// </summary>
        public const string MoveTime = "movetime";

        /// <summary>
        /// Tells the engine to search indefinitely until stopped.
        /// </summary>
        public const string Infinite = "infinite";
    }

    /// <summary>
    /// Contains tokens used with the "setoption" command.
    /// </summary>
    public static class SetOption
    {
        /// <summary>
        /// Specifies the name of the option to set.
        /// </summary>
        public const string Name = "name";

        /// <summary>
        /// Specifies the value to assign to the option.
        /// </summary>
        public const string Value = "value";
    }

    /// <summary>
    /// Contains response tokens that the engine sends to the GUI.
    /// </summary>
    public static class Responses
    {
        /// <summary>
        /// Confirms that the engine is using UCI protocol (response to "uci").
        /// </summary>
        public const string UciOk = "uciok";

        /// <summary>
        /// Confirms that the engine is ready (response to "isready").
        /// </summary>
        public const string ReadyOk = "readyok";

        /// <summary>
        /// Returns the best move found by the engine.
        /// </summary>
        public const string BestMove = "bestmove";

        /// <summary>
        /// Provides information about the current search progress.
        /// </summary>
        public const string Info = "info";

        /// <summary>
        /// Identifies the engine name and author.
        /// </summary>
        public const string Id = "id";

        /// <summary>
        /// Describes an engine option that can be set.
        /// </summary>
        public const string Option = "option";

        /// <summary>
        /// Indicates copy protection status.
        /// </summary>
        public const string CopyProtection = "copyprotection";

        /// <summary>
        /// Indicates registration status for commercial engines.
        /// </summary>
        public const string Registration = "registration";
    }

    /// <summary>
    /// Contains information tokens used in "info" responses to provide search progress details.
    /// </summary>
    public static class Info
    {
        /// <summary>
        /// The search depth reached in plies.
        /// </summary>
        public const string Depth = "depth";

        /// <summary>
        /// The selective search depth reached.
        /// </summary>
        public const string SelDepth = "seldepth";

        /// <summary>
        /// The time spent searching in milliseconds.
        /// </summary>
        public const string Time = "time";

        /// <summary>
        /// The number of nodes searched.
        /// </summary>
        public const string Nodes = "nodes";

        /// <summary>
        /// The principal variation (best line of play found).
        /// </summary>
        public const string Pv = "pv";

        /// <summary>
        /// The number of the principal variation in multi-PV mode.
        /// </summary>
        public const string MultiPv = "multipv";

        /// <summary>
        /// The score centipawn prefix.
        /// </summary>
        public const string Cp = "cp";

        /// <summary>
        /// The score of the position.
        /// </summary>
        public const string Score = "score";

        /// <summary>
        /// The move currently being searched.
        /// </summary>
        public const string CurrMove = "currmove";

        /// <summary>
        /// The number of the move currently being searched.
        /// </summary>
        public const string CurrMoveNumber = "currmovenumber";

        /// <summary>
        /// The hash table fill level in permill (0-1000).
        /// </summary>
        public const string HashFull = "hashfull";

        /// <summary>
        /// The nodes searched per second.
        /// </summary>
        public const string Nps = "nps";

        /// <summary>
        /// The number of tablebase hits.
        /// </summary>
        public const string TbHits = "tbhits";

        /// <summary>
        /// The number of syzygy tablebase hits.
        /// </summary>
        public const string SbHits = "sbhits";

        /// <summary>
        /// The CPU load in permill (0-1000).
        /// </summary>
        public const string CpuLoad = "cpuload";

        /// <summary>
        /// A string that will be displayed by the GUI.
        /// </summary>
        public const string String = "string";

        /// <summary>
        /// A refutation of a move.
        /// </summary>
        public const string Refutation = "refutation";

        /// <summary>
        /// Information about the current line being searched.
        /// </summary>
        public const string CurrLine = "currline";
    }

    /// <summary>
    /// Contains score-related tokens used in "info" responses.
    /// </summary>
    public static class Score
    {
        /// <summary>
        /// Centipawn score (1 pawn = 100 centipawns).
        /// </summary>
        public const string Cp = "cp";

        /// <summary>
        /// Mate score indicating mate in N moves.
        /// </summary>
        public const string Mate = "mate";

        /// <summary>
        /// Indicates the score is a lower bound (fail-low).
        /// </summary>
        public const string LowerBound = "lowerbound";

        /// <summary>
        /// Indicates the score is an upper bound (fail-high).
        /// </summary>
        public const string UpperBound = "upperbound";
    }

    /// <summary>
    /// Contains identification tokens used in "id" responses.
    /// </summary>
    public static class Id
    {
        /// <summary>
        /// The name of the chess engine.
        /// </summary>
        public const string Name = "name";

        /// <summary>
        /// The author(s) of the chess engine.
        /// </summary>
        public const string Author = "author";
    }

    /// <summary>
    /// Contains option-related tokens used in "option" responses.
    /// </summary>
    public static class Option
    {
        /// <summary>
        /// The name of the engine option.
        /// </summary>
        public const string Name = "name";

        /// <summary>
        /// The type of the engine option (check, spin, combo, button, string).
        /// </summary>
        public const string Type = "type";

        /// <summary>
        /// The default value of the option.
        /// </summary>
        public const string Default = "default";

        /// <summary>
        /// The minimum value for numeric options.
        /// </summary>
        public const string Min = "min";

        /// <summary>
        /// The maximum value for numeric options.
        /// </summary>
        public const string Max = "max";

        /// <summary>
        /// A possible value for combo options.
        /// </summary>
        public const string Var = "var";
    }

    /// <summary>
    /// Contains the different types of engine options as defined in the UCI protocol.
    /// </summary>
    public static class OptionTypes
    {
        /// <summary>
        /// Boolean option type (true/false).
        /// </summary>
        public const string Check = "check";

        /// <summary>
        /// Numeric option type with min/max values.
        /// </summary>
        public const string Spin = "spin";

        /// <summary>
        /// Selection option type with predefined choices.
        /// </summary>
        public const string Combo = "combo";

        /// <summary>
        /// Button option type for triggering actions.
        /// </summary>
        public const string Button = "button";

        /// <summary>
        /// Text string option type.
        /// </summary>
        public const string String = "string";
    }

    public static class OptionId
    {
        // Standard UCI Options
        public const string Hash = "Hash";
        public const string NalimovPath = "NalimovPath";
        public const string NalimovCache = "NalimovCache";
        public const string Ponder = "Ponder";
        public const string OwnBook = "OwnBook";
        public const string MultiPv = "MultiPV";
        public const string UciShowCurrLine = "UCI_ShowCurrLine";
        public const string UciShowRefutations = "UCI_ShowRefutations";
        public const string UciLimitStrength = "UCI_LimitStrength";
        public const string UciElo = "UCI_Elo";
        public const string UciAnalyseMode = "UCI_AnalyseMode";
        public const string UciOpponent = "UCI_Opponent";
        public const string UciEngineAbout = "UCI_EngineAbout";
        public const string UciShredderbasesPath = "UCI_ShredderbasesPath";
        public const string UciSetPositionValue = "UCI_SetPositionValue";
        
        // Common Engine-Specific Options (widely used)
        public const string Threads = "Threads";
        public const string ClearHash = "Clear Hash";
        public const string Contempt = "Contempt";
        public const string MoveOverhead = "Move Overhead";
        public const string SlowMover = "Slow Mover";
        public const string NodesTime = "nodestime";
        public const string MinimumThinkingTime = "Minimum Thinking Time";
        public const string SyzygyPath = "SyzygyPath";
        public const string SyzygyProbeDepth = "SyzygyProbeDepth";
        public const string SyzygyProbeLimit = "SyzygyProbeLimit";
        public const string Syzygy50MoveRule = "Syzygy50MoveRule";
    }
}