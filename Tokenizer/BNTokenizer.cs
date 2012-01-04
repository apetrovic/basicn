// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using System.Collections.Generic;
using BasicN.Parser;
using System.IO;

namespace BasicN.Tokenizer {
	public class TokenizerOutput {
		public IList<TLine> Program;
		public IList<string> Data;
	}

	public class TokenizerException : Exception {
		public TLine Line;
		public TokenizerException(TLine line, string message) : base( message ) { Line = line; }
	}

	public class NBTokenizer {
		public static TokenizerOutput TokenizeFile(IErrorPrinter errorPrinter, string fileName) {
			return Tokenize( errorPrinter, GetLines( fileName ) );
		}

		public static TokenizerOutput Tokenize(IErrorPrinter errorPrinter, IEnumerable<string> program) {
			var parser = new BasicNParser();
			IEnumerable<Line> lines = parser.ParseLines( program, false );

			bool ret = true;
			foreach( Line line in lines ) {
				if( line.Report != ParseReport.Ok ) {
					ret = false;
					PrintErrorReport( errorPrinter, line );
				}
			}

			if( !ret )
				return null;

			IList<TLine> tokenizedLines = Normalize( lines );

			return Tokenize( errorPrinter, tokenizedLines );
		}

		public static IEnumerable<string> GetLines(string fileName) {
			using( StreamReader sr = new StreamReader( fileName ) ) {
				string line;
				while( ( line = sr.ReadLine() ) != null ) {
					yield return line;
				}
			}
		}

		private static void PrintErrorReport(IErrorPrinter errorPrinter, Line line) {
			errorPrinter.PrintError( "Error: " + line.Report );
			errorPrinter.PrintError( line.ErrorMessage );

			if( !line.ErrorColumn.HasValue || !line.OriginalLinePosition.HasValue )
				return;

			errorPrinter.PrintError( String.Format( "Position: {0}:{1}", line.OriginalLinePosition, line.ErrorColumn ) );
			errorPrinter.PrintError( line.OriginalLine );
			string s = new string( ' ', line.ErrorColumn.Value - 1 ) + "^";
			errorPrinter.PrintError( s );
			errorPrinter.PrintError( "" );
		}

		private static IList<TLine> Normalize(IEnumerable<Line> lines) {
			var prog = new List<Line>( lines );
			prog.Sort( (a, b) => Comparer<int?>.Default.Compare( a.LineNum, b.LineNum ) );
			var ret = new List<TLine>();
			foreach( Line line in prog ) {
				foreach( Statement stat in line.Statements ) {
					TLine newLine = new TLine( line, stat );
					ret.Add( newLine );
				}
			}

			return ret;
		}

		class LoopData {
			public int StartLine;
			public NumVariable LoopVariable;
			public NumVariable StepVariable;
		}

		private static TokenizerOutput Tokenize(IErrorPrinter errorPrinter, IEnumerable<TLine> program) {
			var ret = new TokenizerOutput { Program = new List<TLine>() };

			var positions = new Dictionary<int, int>();
			var loopStack = new Stack<LoopData>();

			try {
				foreach( var line in program ) {
					int ln = line.OriginalLine.LineNum.Value;
					if( !positions.ContainsKey( ln ) )
						positions.Add( ln, ret.Program.Count );

					TokenizeStatement( ret, line, loopStack );
				}

				ret.Program.Add( new TLine( new Line(), new KwEnd() ) );
			}
			catch( TokenizerException e ) {
				Console.WriteLine( "ERROR!" );
				Console.WriteLine( "Line " + e.Line.OriginalLine.OriginalLinePosition + " : " + e.Line.OriginalLine.OriginalLine );
				Console.WriteLine( e.Message );
				ret = null;
			}

			if( ret == null )
				return null;

			// check jumps
			for( int i = 0; i < ret.Program.Count; ++i ) {
				TLine cmd = ret.Program[i];
				var gt = cmd.Statement as KwrJump;
				if( gt != null ) {
					if( !gt.Normalized ) {
						if( !positions.ContainsKey( gt.JumpPos ) )
							throw new TokenizerException( new TLine( cmd.OriginalLine, cmd.OriginalLine.Statements[0] ), "Invalid jump" );

						gt.JumpPos = positions[gt.JumpPos];
						gt.Normalized = true;
					}

					if( gt is KwrGosub )
						((KwrGosub)gt).ReturnAddress = i + 1;

				}
			}

			return ret;
		}

		private static void TokenizeStatement(TokenizerOutput program, TLine line, Stack<LoopData> loopStack) {
			if( line.Statement is KwGoto ) {
				program.Program.Add( line.Clone( new KwrGoto( ( (KwGoto)line.Statement ).Value.Value, false ) ) );
			}
			else if( line.Statement is KwGosub ) {
				program.Program.Add( line.Clone( new KwrGosub( ( (KwGosub)line.Statement ).Value.Value, false ) ) );
			}
			else if( line.Statement is KwData ) {
				KwData data = (KwData)line.Statement;

				if( program.Data == null )
					program.Data = new List<string>();

				( (List<string>)program.Data ).AddRange( data.Data );
				program.Program.Add( line.Clone( new KwRem( line.OriginalLine.OriginalLine ) ) );
			}
			else if( line.Statement is KwIf ) {
				MakeBranch( program, line, loopStack );
			}
			else if( line.Statement is KwFor ) {
				LoopData d = MakeLoopBegin( program, line );
				loopStack.Push( d );
			}
			else if( line.Statement is KwNext ) {
				MakeLoopEnd( program, line, loopStack );
			}
			else if( line.Statement is KwOn ) {
				MakeOnStatement( program, line );
			}
			else if( line.Statement is KwPrint ) {
				MakePrintStatement( program, line );
			}
			else if( line.Statement is KwDim ) {
				KwDim dim = (KwDim)line.Statement;
				foreach( VariableArray arr in dim.ArrayList )
					program.Program.Add( line.Clone( new KwrDim( arr ) ) );
			}
			else {
				program.Program.Add( line );
			}
		}

		private static void MakeBranch(TokenizerOutput program, TLine line, Stack<LoopData> loopStack) {
			var kwif = (KwIf)line.Statement;
			var newIf = new KwrJumpIfNotTrue( kwif.Condition, -1 );
			program.Program.Add( line.Clone( newIf ) ); // placeholder

			foreach( Statement s in kwif.Statements )
				TokenizeStatement( program, line.Clone( s ), loopStack );

			if( kwif.ElseStatements == null ) {
				newIf.JumpPos = program.Program.Count;

			}
			else {
				var gt = new KwrGoto( -1, true );
				program.Program.Add( line.Clone( gt ) );
				newIf.JumpPos = program.Program.Count;

				foreach( Statement s in kwif.ElseStatements )
					TokenizeStatement( program, line.Clone( s ), loopStack );

				gt.JumpPos = program.Program.Count;
			}
		}

		private static LoopData MakeLoopBegin(TokenizerOutput program, TLine line) {
			var kwFor = (KwFor)line.Statement;
			var ret = new LoopData{ StartLine = ( program.Program.Count + 2 ) };

			// step 1: var = initial
			NumVariable forVar = kwFor.Variable;
			program.Program.Add( line.Clone( new NumBinaryOperator( forVar, kwFor.Initial, "=" ) ) );
			ret.LoopVariable = forVar;

			// step 2: make step variable
			var stepVar = new NumVariable( "$" + ret.StartLine + forVar.Name + "_StepVariable" );
			NumStatement stepValue = kwFor.Step ?? new NumConstant( "1" );
			program.Program.Add( line.Clone( new NumBinaryOperator( stepVar, stepValue, "=" ) ) );
			ret.StepVariable = stepVar;

			// step 3: loop
			var condition = new NumBoolBinaryOperator( forVar, kwFor.End, ">" );
			var jmp = new KwrJumpIfTrue( condition, -1 );
			program.Program.Add( line.Clone( jmp ) );

			return ret;
		}

		private static void MakeLoopEnd(TokenizerOutput program, TLine line, Stack<LoopData> loopStack) {
			if( loopStack.Count < 1 )
				throw new TokenizerException( line, "Misplaced NEXT" );

			LoopData data = loopStack.Pop();
			var next = (KwNext)line.Statement;

			if( data.LoopVariable.Name != next.Variable.Name )
				throw new TokenizerException( line, "NEXT Error : Unknown variable " + next.Variable.Name );

			// step 1: change the variable
			var add = new NumBinaryOperator( data.LoopVariable, data.StepVariable, "+" );
			var assign = new NumBinaryOperator( data.LoopVariable, add, "=" );
			program.Program.Add( line.Clone( assign ) );

			// step 2: goto to the begining
			var gt = new KwrGoto( data.StartLine, true );
			program.Program.Add( line.Clone( gt ) );

			// change the top jump condition
			( (KwrJumpIfTrue)program.Program[data.StartLine].Statement ).JumpPos = program.Program.Count;
		}

		private static void MakeOnStatement(TokenizerOutput program, TLine line) {
			var kw = (KwOn)line.Statement;

			for( int j = 0; j < kw.JumpList.Count; ++j ) {
				var cnd = new NumBoolBinaryOperator( kw.Statement, new NumConstant( (j + 1).ToString() ), "==" );
				var ifc = new KwrJumpIfNotTrue( cnd, -1 );
				program.Program.Add( line.Clone( ifc ) );

				int jumpPos = kw.JumpList[j].Value;
				KwrJump jmp = kw.Kind == KwOn.OnKind.Goto ? new KwrGoto( jumpPos, false ) : (KwrJump)new KwrGosub( jumpPos, false );
				program.Program.Add( line.Clone( jmp ) );

				ifc.JumpPos = program.Program.Count;
			}
		}

		private static void MakePrintStatement(TokenizerOutput program, TLine line) {
			var print = (KwPrint)line.Statement;

			if( print.PrintList == null ) {
				program.Program.Add( line.Clone( new KwrPrint( new StringConstant( "" ), true ) ) );
				return;
			}

			foreach( KwPrint.Group group in print.PrintList ) {
				bool newLine = String.IsNullOrEmpty( group.EndChar );
				program.Program.Add( line.Clone( new KwrPrint( group.Statement, newLine ) ) );

				if( !newLine && group.EndChar == "," )
					program.Program.Add( line.Clone( new KwrPrint( new StringConstant( "\t" ), false ) ) );
			}
		}
	}
}
