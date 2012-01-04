// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System.Collections.Generic;
using BasicN.Parser;
using BasicN.Tokenizer;
using BasicN.Lib;
using System.Threading;

namespace BasicN.Interpreter {
	public class PrintCommand : BaseItem {
		private readonly IValue _value;
		private readonly bool _printLine;

		public PrintCommand(TLine pl, NBInterpreter i) : base( pl, i ) {
			var print = (KwrPrint)pl.Statement;
			_value = (IValue)Interpreter.Make( pl.Clone( print.Statement ) );
			_printLine = print.NewLine;
		}

		public override InterpreterStatus Execute(IContext c) {
			InterpreterStatus ret = _value.Execute( c );
			if( ret == InterpreterStatus.Ok ) {
				if( _printLine )
					c.PrintLine( _value.ValueAsString );
				else
					c.Print( _value.ValueAsString );
			}
			return ret;
		}

		public override string ToString() { return "PRINT " + (_value != null ? _value.ToString() : ""); }
	}

	public class EndCommand : BaseItem {
		public EndCommand(TLine pl, NBInterpreter i) : base( pl, i ) { }
		public override InterpreterStatus Execute(IContext c) { return InterpreterStatus.End; }
		public override string ToString() { return "END"; }
	}

	public class NoOp : BaseItem {
		public NoOp(TLine pl, NBInterpreter i) : base( pl, i ) { }
		public override InterpreterStatus Execute(IContext c) { return InterpreterStatus.Ok;  }
		public override string ToString() { return "NOP"; }
	}

	public class ClsCommand : BaseItem {
		public ClsCommand(TLine pl, NBInterpreter i) : base( pl, i ) { }
		public override InterpreterStatus Execute(IContext c) {
			c.Cls();
			return InterpreterStatus.Ok;
		}

		public override string ToString() { return "CLS"; }
	}

	public class LocateCommand : BaseItem {
		private readonly IValue<double> _x;
		private readonly IValue<double> _y;
		public LocateCommand(TLine pl, NBInterpreter i) : base( pl, i ) {
			KwLocate loc = (KwLocate)pl.Statement;
			_x = (IValue<double>)Interpreter.Make( pl.Clone( loc.X ) );
			_y = (IValue<double>)Interpreter.Make( pl.Clone( loc.Y ) );
		}

		public override InterpreterStatus Execute(IContext c) {
			_x.Execute( c );
			_y.Execute( c );
			c.Locate( (int)_x.Value, (int)_y.Value );
			return InterpreterStatus.Ok;
		}

		public override string ToString() { return "LOCATE " + _x + ", " + _y; }
	}

	public class RunCommand : BaseItem {
		public RunCommand(TLine pl, NBInterpreter i) : base( pl, i ) { }
		public override InterpreterStatus Execute(IContext c) {
			return InterpreterStatus.Run;
		}

		public override string ToString() { return "RUN"; }
	}

	public class RandomizeCommand : BaseItem {
		private readonly IValue<double> _seed;

		public RandomizeCommand(TLine pl, NBInterpreter i) : base( pl, i ) {
			KwRandomize rnd = (KwRandomize)pl.Statement;
			if( rnd.Statement != null )
				_seed = (IValue<double>)Interpreter.Make( pl.Clone( rnd.Statement ) );
		}

		public override InterpreterStatus Execute(IContext c) {
			if( _seed != null ) {
				_seed.Execute( c );
				Interpreter.Randomize( (int)_seed.Value );
			}
			else
				Interpreter.Randomize();

			return InterpreterStatus.Ok;
		}

		public override string ToString() { return "RANDOMIZE"; }
	}

	public class PauseCommand : BaseItem {
		private readonly IValue<double> _interval;
		public PauseCommand(TLine pl, NBInterpreter i) : base( pl, i ) {
			KwPause pause = (KwPause)pl.Statement;
			if( pause != null )
				_interval = (IValue<double>)Interpreter.Make( pl.Clone( pause.Interval ) );
		}

		public override InterpreterStatus Execute(IContext c) {
			_interval.Execute( c );
			Thread.Sleep( (int)_interval.Value );
			return InterpreterStatus.Ok;
		}

		public override string ToString() {
			return "PAUSE " + _interval;
		}
	}


	public class RndCommand : BaseItem, IValue<double> {
		private readonly IValue<double> _maxValue;
		private double _retValue;

		public RndCommand(TLine pl, NBInterpreter i) : base( pl, i ) {
			_maxValue = (IValue<double>)Interpreter.Make( pl.Clone( ( (Function1)pl.Statement ).Param1 ) );
		}
		public override InterpreterStatus Execute(IContext c) {
			_maxValue.Execute( c );
			_retValue = Interpreter.Rnd( (int)_maxValue.Value );
			return InterpreterStatus.Ok;
		}

		public double Value { get { return _retValue; } }
		public string ValueAsString { get { return _retValue.ToString(); } }

		public override string ToString() { return "RND [" + _retValue + "]"; }
	}

	public class BaseBinaryOperator<T1, T2> : BaseItem, IValue<T1> {
		protected Func<IValue<T2>, IValue<T2>, T1> _worker;
		protected string _operator = "??";

		protected IValue<T2> _left;
		protected IValue<T2> _right;
		protected T1 _value;

		public BaseBinaryOperator(TLine line, NBInterpreter i) : base( line, i ) {
			BinaryOperator sb = (BinaryOperator)line.Statement;
			Statement left = sb.Left;
			Statement right = sb.Right;

			_left = (IValue<T2>)Interpreter.Make( line.Clone( left ) );
			_right = (IValue<T2>)Interpreter.Make( line.Clone( right ) );
		}

		protected BaseBinaryOperator(TLine line, NBInterpreter i, IValue<T2> left, IValue<T2> right, Func<IValue<T2>, IValue<T2>, T1> worker)
			: base( line, i ) {
			_left = left;
			_right = right;
			_worker = worker;
		}

		public override InterpreterStatus Execute(IContext c) {
			InterpreterStatus ret = _left.Execute( c );
			if( ret != InterpreterStatus.Ok )
				return ret;

			ret = _right.Execute( c );
			if( ret != InterpreterStatus.Ok )
				return ret;

			_value = _worker( _left, _right );

			return InterpreterStatus.Ok;
		}

		public T1 Value {
			get { return _value; }
		}

		public string ValueAsString {
			get { return _value.ToString(); }
		}

		public override string ToString() { return _left + " " + _operator + " " + _right; }
	}

	public class BinaryOperatorString : BaseBinaryOperator<string, string> {
		public BinaryOperatorString(TLine line, NBInterpreter i) : base(line, i) {
			_operator = ((BinaryOperator)line.Statement).Operator;
			switch( _operator ) {
				case "=": _worker = (l, r) => { ((IVariable<string>)l).SetValue( _right.Value ); return r.Value; }; break;
				case "+": _worker = (l, r) => l.Value + r.Value ; break;

				default: throw new InterpreterException( line, "Unknown string operator: " + _operator );
			}
		}
	}

	public class BinaryOperatorDouble : BaseBinaryOperator<double, double> {
		public BinaryOperatorDouble(TLine line, NBInterpreter i): base( line, i ) {
			_operator = ( (BinaryOperator)line.Statement ).Operator;
			_worker = MakeWorker( line, _operator );
		}

		private static Func<IValue<double>, IValue<double>, double> MakeWorker(TLine pg, string oper) {
			switch( oper ) {
				case "=": return (l, r) => { ((IVariable<double>)l).SetValue( r.Value ); return r.Value; };
				case "+": return (l, r) => l.Value + r.Value ;
				case "-": return (l, r) => l.Value - r.Value ;
				case "*": return (l, r) => l.Value * r.Value ;
				case "/": return (l, r) => l.Value / r.Value ;
				case "&": return (l, r) => (double)( (int)l.Value & (int)r.Value );
				case "|": return (l, r) => (double)( (int)l.Value | (int)r.Value );
				case "^": return (l, r) => (double)( (int)l.Value ^ (int)r.Value );
				default: throw new InterpreterException( pg, "Unknown num operator: " + oper );
			}
		}

		internal BinaryOperatorDouble(TLine line, NBInterpreter i, IValue<double> left, IValue<double> right, string oper) :
			base( line, i, left, right, MakeWorker( line, oper ) ) { _operator = oper; }
	}

	public class BinaryOperatorBool : BaseBinaryOperator<bool, bool> {
		public BinaryOperatorBool(TLine line, NBInterpreter i) : base( line, i ) {
			_operator = ( (BinaryOperator)line.Statement ).Operator;
			switch( _operator ) {
				case "&&": _worker = (l, r) => l.Value && r.Value; break;
				case "||": _worker = (l, r) => l.Value || r.Value; break;
				case "^^": _worker = (l, r) => l.Value ^ r.Value; break;
				default: throw new InterpreterException( line, "Unknown boolean operator: " + _operator );
			}
		}
	}

	public class BinaryOperatorBoolDouble : BaseBinaryOperator<bool, double> {
		public BinaryOperatorBoolDouble(TLine line, NBInterpreter i) : base( line, i ) {
			_operator = ( (BinaryOperator)line.Statement ).Operator;
			_worker = GetWorker( line, _operator );
		}

		private static Func<IValue<double>, IValue<double>, bool> GetWorker(TLine line, string oper) {
			switch( oper ) {
				case "==": return (l, r) => l.Value == r.Value;
				case "<": return (l, r) => l.Value < r.Value;
				case "<=": return (l, r) => l.Value <= r.Value;
				case ">": return (l, r) => l.Value > r.Value;
				case ">=": return (l, r) => l.Value >= r.Value;
				case "<>": return (l, r) => l.Value != r.Value;
				default: throw new InterpreterException( line, "Unknown boolean operator: " + oper );
			}
		}

		internal BinaryOperatorBoolDouble(TLine line, NBInterpreter i, IValue<double> left, IValue<double> right, string oper)
			: base( line, i, left, right, GetWorker( line, oper ) ) { _operator = oper; }
	}

	public class BinaryOperatorBoolString : BaseBinaryOperator<bool, string> {
		public BinaryOperatorBoolString(TLine line, NBInterpreter i) : base( line, i ) {
			_operator = ( (BinaryOperator)line.Statement ).Operator;
			switch( _operator ) {
				case "==": _worker = (l, r) => l.Value.CompareTo( r.Value ) == 0; break;
				case "<": _worker = (l, r) => l.Value.CompareTo( r.Value ) < 0; break;
				case "<=": _worker = (l, r) => l.Value.CompareTo( r.Value ) <= 0; break;
				case ">": _worker = (l, r) => l.Value.CompareTo( r.Value ) > 0; break;
				case ">=": _worker = (l, r) => l.Value.CompareTo( r.Value ) >= 0; break;
				case "<>": _worker = (l, r) => l.Value.CompareTo( r.Value ) != 0; break;
				default: throw new InterpreterException( line, "Unknown boolean operator: " + _operator );
			}
		}
	}

	public class UnaryOperator<T> : BaseItem, IValue<T> {
		private T _returnValue;
		private readonly IValue<T> _value;
		private readonly Func<T, T> _worker;

		public UnaryOperator(TLine pl, NBInterpreter i, Func<Statement, Statement> getter, Func<T, T> worker) : base( pl, i ) {
			_value = (IValue<T>)Interpreter.Make( pl.Clone( getter( pl.Statement ) ) );
			_worker = worker;
		}

		public override InterpreterStatus Execute(IContext c) {
			InterpreterStatus ret = _value.Execute( c );
			if( ret != InterpreterStatus.Ok )
				return ret;

			_returnValue = _worker( _value.Value );
			return ret;
		}

		public T Value {
			get { return _returnValue; }
		}

		public string ValueAsString {
			get { return Value.ToString(); }
		}

		public override string ToString() { return "(unary)" + ValueAsString; }
	}

	public class DimCommand : BaseItem {
		public DimCommand(TLine line, NBInterpreter i) : base( line, i ) { }

		public override InterpreterStatus Execute(IContext c) {
			KwrDim dim = (KwrDim)Line.Statement;
			VariableArray arr = dim.Array;
			if( Interpreter.Arrays.ContainsKey( arr.Name ) )
				throw new InterpreterException( Line, "Array " + arr.Name + " is already defined!" );

			var dimensions = new List<int>();
			foreach( var dimension in arr.Dimensions ) {
				IValue<double> f = (IValue<double>)Interpreter.Make( Line.Clone( dimension ) );
				f.Execute( c );
				dimensions.Add( (int)f.Value );
			}

			if( arr.Name.EndsWith( "$" ) )
				Interpreter.Arrays.Add( arr.Name, new StringArrayStore( dimensions ) );
			else
				Interpreter.Arrays.Add( arr.Name, new ArrayValueStore<double>( dimensions ) );

			_name = arr.Name;
			_dimensions = "";
			foreach( int d in dimensions )
				_dimensions += _dimensions.Length == 0 ? d.ToString() : "," + d;

			return InterpreterStatus.Ok;
		}

		private string _name;
		private string _dimensions;
		public override string ToString() { return "DIM " + _name + "(" + _dimensions + ")"; }
	}

	public abstract class JumpCommand : BaseItem {
		public int JumpPos { get; set; }

		protected JumpCommand(TLine pg, NBInterpreter i, int jumpPos) : base( pg, i ) {
			JumpPos = jumpPos;
		}
	}

	public class GotoCommand : JumpCommand {
		public GotoCommand(TLine pg, NBInterpreter i) : base( pg, i, ( (KwrGoto)pg.Statement ).JumpPos ) { }
		public override InterpreterStatus Execute(IContext c) { Interpreter.PrepareGoto( Line, JumpPos ); return InterpreterStatus.Ok; }
		public override string ToString() { return "GOTO " + JumpPos; }
	}

	public class GosubCommand : JumpCommand {
		public GosubCommand(TLine pg, NBInterpreter i) : base( pg, i, ( (KwrGosub)pg.Statement ).JumpPos ) { }
		public override InterpreterStatus Execute(IContext c) { Interpreter.PrepareGosub( Line, JumpPos ); return InterpreterStatus.Ok; }
		public override string ToString() { return "GOSUB " + JumpPos; }
	}

	public class JumpIfTrueCommand : JumpCommand {
		private readonly IValue<bool> _condition;
		public JumpIfTrueCommand(TLine pg, NBInterpreter i) : base( pg, i, -1 ) {
			var jmp = (KwrJumpIfTrue)pg.Statement;
			_condition = (IValue<bool>)Interpreter.Make( pg.Clone( jmp.Condition ) );
			JumpPos = jmp.JumpPos;
		}

		public override InterpreterStatus Execute(IContext c) {
			InterpreterStatus ret = _condition.Execute( c );
			if( ret != InterpreterStatus.Ok )
				return ret;

			if( _condition.Value )
				Interpreter.PrepareGoto( Line, JumpPos );

			return InterpreterStatus.Ok;
		}

		public override string ToString() { return "JumpIfTrue to " + JumpPos + ": " + _condition; }
	}

	public class JumpIfNotTrueCommand : JumpCommand {
		private readonly IValue<bool> _condition;
		public JumpIfNotTrueCommand(TLine pg, NBInterpreter i) : base( pg, i, -1 ) {
			var jmp = (KwrJumpIfNotTrue)pg.Statement;
			_condition = (IValue<bool>)Interpreter.Make( pg.Clone( jmp.Condition ) );
			JumpPos = jmp.JumpPos;
		}

		public override InterpreterStatus Execute(IContext c) {
			InterpreterStatus ret = _condition.Execute( c );
			if( ret != InterpreterStatus.Ok )
				return ret;

			if( !_condition.Value )
				Interpreter.PrepareGoto( Line, JumpPos );

			return InterpreterStatus.Ok;
		}

		public override string ToString() { return "JumpIfNotTrue to " + JumpPos + ": " + _condition; }
	}


	public class ReturnCommand : BaseItem {
		public ReturnCommand(TLine pg, NBInterpreter i) : base( pg, i ) { }

		public override InterpreterStatus Execute(IContext c) {
			Interpreter.PrepareReturn( Line );
			return InterpreterStatus.Ok;
		}

		public override string ToString() { return "RETURN"; }
	}

	public class InputCommand : BaseItem {
		private readonly string _varName;
		private readonly Action<IContext> _worker;

		public InputCommand(TLine pg, NBInterpreter interpreter) : base( pg, interpreter ) {
			string prompt = null;
			bool questionMark = true;

			KwInput i = (KwInput)pg.Statement;
			if( i.Prompt != null )
				prompt = i.Prompt.Value;

			if( i.Separator.HasValue && i.Separator.Value == ',' )
				questionMark = false;

			_varName = i.Variable.Name;

			if( i.Variable is NumVariable || i.Variable is NumArray ) {
				IVariable<double> variable = (IVariable<double>)Interpreter.Make( pg.Clone( (Statement)i.Variable ) );
				_worker = context => {
					variable.Execute( context );
					variable.SetValue( BNLib.InputDouble( context, prompt, questionMark ) );
				};
			}
			else {
				IVariable<string> variable = (IVariable<string>)Interpreter.Make( pg.Clone( (Statement)i.Variable ) );
				_worker = context => {
					variable.Execute( context );
					variable.SetValue( BNLib.InputString( context, prompt, questionMark ) );
				};
			}
		}

		public override InterpreterStatus Execute(IContext context) {
			_worker( context );
			return InterpreterStatus.Ok;
		}

		public override string ToString() {
			return "INPUT " + _varName;
		}
	}

	public class ReadCommand : BaseItem {
		private string _read;
		private readonly Func<IContext, InterpreterStatus> _worker;

		public ReadCommand(TLine line, NBInterpreter i) : base( line, i ) {
			KwRead read = (KwRead)line.Statement;
			if( read.Variable is NumVariable || read.Variable is NumArray ) {
				IVariable<double> doubleVar = (IVariable<double>)Interpreter.Make( line.Clone( (Statement)read.Variable ) );
				_worker = c => {
					string next = Interpreter.ReadNextString();
					if( next == null )
						throw new InterpreterException( line, "READ overflow! Variable: " + doubleVar.Name );

					double val;
					if( !double.TryParse( next, out val ) )
						throw new InterpreterException( line, "READ error! Variable: " + doubleVar.Name + " value: " + next );

					doubleVar.Execute( c );
					doubleVar.SetValue( val );
					_read = val.ToString();

					return InterpreterStatus.Ok;
				};
			}
			else {
				IVariable<string> stringVar = (IVariable<string>)Interpreter.Make( line.Clone( (Statement)read.Variable ) );
				_worker = c => {
					string next = Interpreter.ReadNextString();
					if( next == null )
						throw new InterpreterException( line, "READ overflow! Variable: " + stringVar.Name );

					stringVar.Execute( c );
					stringVar.SetValue( next );
					_read = next;

					return InterpreterStatus.Ok;
				};
			}
		}

		public override InterpreterStatus Execute(IContext c) {
			return _worker( c );
		}

		public override string ToString() { return "READ [" + _read + "]"; }
	}
}
