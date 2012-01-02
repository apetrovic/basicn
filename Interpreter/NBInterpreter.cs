using System;
using System.Collections;
using System.Collections.Generic;
using BasicN.Tokenizer;
using BasicN.Lib;

namespace BasicN.Interpreter {
	public partial class NBInterpreter : IErrorPrinter {
		#region fields

		private readonly IContext _context;
		private int? _jumpLine;
		private Stack<int> _subStack;

		private Random _random = new Random();

		private List<string> _readList = new List<string>();
		private int _readPos;

		#endregion

		#region Public stuff
		public NBInterpreter(IContext context) {
			_context = context;
			Reset();
		}

		public IList<IItem> Program { get; private set; }
		public int CurrentLine { get; protected set; }
		public int CurrentLineNumber { get; protected set; }

		public void PrintError(string message) { _context.PrintLine( message ); }
		public void AddToReadList(IEnumerable<string> str) { _readList.AddRange( str ); }

		public bool Load(IEnumerable<string> program) {
			Reset();
			TokenizerOutput to = NBTokenizer.Tokenize( this, program );

			return LoadProgram( to );
		}

		public bool Load(string fileName) {
			Reset();
			TokenizerOutput to = NBTokenizer.TokenizeFile( this, fileName );
			return LoadProgram( to );
		}

		public void Run() {
			while( true ) {
				InterpreterStatus ret = Step();
				if( ret == InterpreterStatus.Run ) {
					Reset();
					continue;
				}

				if( ret == InterpreterStatus.End )
					return;
			}
		}

		public InterpreterStatus Step() {
			if( Program == null || CurrentLine >= Program.Count || _context == null )
				return InterpreterStatus.End;

			IItem command = Program[CurrentLine];
			CurrentLineNumber = command.Line.OriginalLine.LineNum ?? 0;

			InterpreterStatus ret;
			try {
				ret = command.Execute( _context );
			}
			catch( Exception e ) {
				_context.PrintLine( "" );
				_context.PrintLine( "Error: " + e.Message );
				_context.PrintLine( "Line:" + ( command.Line.OriginalLine.LineNum ?? 0 ) );
				_context.PrintLine( command.Line.OriginalLine.OriginalLine );
				return InterpreterStatus.End;
			}

			CurrentLine = _jumpLine ?? CurrentLine + 1;
			_jumpLine = null;

			if( ret == InterpreterStatus.Ok && CurrentLine >= Program.Count )
				ret = InterpreterStatus.End;

			if( ret == InterpreterStatus.End )
				CurrentLine = Program.Count + 100;

			if( ret == InterpreterStatus.Run ) {
				Reset();
				ret = InterpreterStatus.Ok;
			}

			return ret;
		}

		#endregion

		#region For commands use

		internal Dictionary<string, IValueStore> Variables;
		internal Dictionary<string, IValueStore> Arrays;

		internal int Rnd(int maxValue) { return _random.Next( maxValue ); }
		internal void Randomize(int seed) { _random = new Random( seed ); }
		internal void Randomize() { _random = new Random(); }

		internal void PrepareGoto(TLine pg, int line) {
			if( line < 0 || line >= Program.Count )
				throw new InterpreterException( pg, "Invalid GOTO jump" );

			_jumpLine = line;
		}

		internal void PrepareGosub(TLine pg, int line) {
			if( _subStack.Count > 50 )
				throw new InterpreterException( pg, "GOSUB stack overflow" );

			if( line < 0 || line >= Program.Count )
				throw new InterpreterException( pg, "Invalid GOSUB jump" );

			_subStack.Push( CurrentLine + 1 );
			_jumpLine = line;
		}

		internal void PrepareReturn(TLine pg) {
			if( _subStack.Count == 0 )
				throw new InterpreterException( pg, "RETURN without GOSUB in " + pg.OriginalLine.LineNum );

			_jumpLine = _subStack.Pop();
		}

		internal string ReadNextString() {
			return _readPos >= _readList.Count ? null : _readList[_readPos++];
		}

		#endregion

		#region Debugging

		public IValueStore GetVariable(string name) {
			IValueStore ret;
			Variables.TryGetValue( name.ToUpper(), out ret );
			return ret;
		}

		public IValueStore GetArray(string name) {
			IValueStore ret;
			Arrays.TryGetValue( name.ToUpper(), out ret );
			return ret;
		}

		public void PrintAllVariables() {
			_context.PrintLine( "" );
			_context.PrintLine( "Variables:" );
			foreach( var pair in Variables ) {
				if( !pair.Key.StartsWith( "$" ) )
					_context.PrintLine( string.Format( "{0} = {1}", pair.Key, pair.Value ) );
			}

			_context.PrintLine( "" );
			_context.PrintLine( "Arrays:" );
			foreach( var pair in Arrays ) {
				_context.PrintLine( string.Format( "{0} = [", pair.Key ) );
				_context.PrintLine( pair.Value.ToString() );
				_context.PrintLine( "]" );
			}
		}



		#endregion

		#region Internal stuff

		private void Reset() {
			CurrentLine = 0;
			_subStack = new Stack<int>();
			_readList = new List<string>();
			_readPos = 0;
			Variables = new Dictionary<string, IValueStore>();
			Arrays = new Dictionary<string, IValueStore>();
		}

		private bool LoadProgram(TokenizerOutput program) {
			_readList.Clear();
			Program = null;

			if( program == null )
				return false;

			if( program.Data != null )
				_readList.AddRange( program.Data );

			Program = new List<IItem>();
			foreach( TLine line in program.Program ) {
				Program.Add( Make( line ) );
			}

			return true;
		}

		#endregion
	}
}
