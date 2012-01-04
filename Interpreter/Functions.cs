// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using BasicN.Parser;
using BasicN.Tokenizer;
using BasicN.Lib;

namespace BasicN.Interpreter {
	public delegate T FuncVoid<T>();

	public class FunctionImp<T1> : BaseItem, IValue<T1> {
		private T1 _retValue;
		private readonly Func<IContext, T1> _worker;

		public FunctionImp(TLine line, NBInterpreter i) : base( line, i ) { }
		public FunctionImp(TLine line, NBInterpreter i, Func<IContext, T1> worker) : base( line, i ) { _worker = worker; }

		public override InterpreterStatus Execute(IContext c) {
			_retValue = _worker( c );
			return InterpreterStatus.Ok;
		}

		public T1 Value { get { return _retValue; } }
		public string ValueAsString { get { return _retValue.ToString(); } }

		public override string ToString() { return "Func: " + Line.Statement.GetType().Name + " [" + ( _retValue != null ? ValueAsString : "" ) + "]"; }
	}

	public class FunctionImp<T1, T2> : BaseItem, IValue<T1> {
		private readonly IValue<T2> _value1;
		private T1 _retValue;
		private readonly Func<IValue<T2>, T1> _worker;

		public FunctionImp(TLine line, NBInterpreter i) : base( line, i ) { }

		public FunctionImp(TLine line, NBInterpreter i, Func<IValue<T2>, T1> worker) : base( line, i ) {
			_worker = worker;
			_value1 = (IValue<T2>)Interpreter.Make( line.Clone( ( (Function1)line.Statement ).Param1 ) );
		}

		public override InterpreterStatus Execute(IContext c) {
			_value1.Execute( c );
			_retValue = _worker( _value1 );

			return InterpreterStatus.Ok;
		}

		public T1 Value { get { return _retValue; } }
		public string ValueAsString { get { return _retValue.ToString(); } }

		public override string ToString() { return "Func: " + Line.Statement.GetType().Name + " [" + ( _retValue != null ? ValueAsString : "" ) + "]"; }
	}

	public class FunctionImp<T1, T2, T3> : BaseItem, IValue<T1> {
		private readonly IValue<T2> _value1;
		private readonly IValue<T3> _value2;
		private T1 _retValue;
		private readonly Func<IValue<T2>, IValue<T3>, T1> _worker;

		public FunctionImp(TLine line, NBInterpreter i) : base( line, i ) { }

		public FunctionImp(TLine line, NBInterpreter i, Func<IValue<T2>, IValue<T3>, T1> worker) : base( line, i ) {
			_worker = worker;
			_value1 = (IValue<T2>)Interpreter.Make( line.Clone( ( (Function2)line.Statement ).Param1 ) );
			_value2 = (IValue<T3>)Interpreter.Make( line.Clone( ( (Function2)line.Statement ).Param2 ) );
		}

		public override InterpreterStatus Execute(IContext c) {
			_value1.Execute( c );
			_value2.Execute( c );
			_retValue = _worker( _value1, _value2 );

			return InterpreterStatus.Ok;
		}

		public T1 Value { get { return _retValue; } }
		public string ValueAsString { get { return _retValue.ToString(); } }

		public override string ToString() { return "Func: " + Line.Statement.GetType().Name + " [" + ( _retValue != null ? ValueAsString : "" ) + "]"; }
	}

	public class FunctionImp<T1, T2, T3, T4> : BaseItem, IValue<T1> {
		private readonly IValue<T2> _value1;
		private readonly IValue<T3> _value2;
		private readonly IValue<T4> _value3;
		private T1 _retValue;
		private readonly Func<IValue<T2>, IValue<T3>, IValue<T4>, T1> _worker;

		public FunctionImp(TLine line, NBInterpreter i) : base( line, i ) { }

		public FunctionImp(TLine line, NBInterpreter i, Func<IValue<T2>, IValue<T3>, IValue<T4>, T1> worker)
			: base( line, i ) {
			_worker = worker;
			_value1 = (IValue<T2>)Interpreter.Make( line.Clone( ( (Function3)line.Statement ).Param1 ) );
			_value2 = (IValue<T3>)Interpreter.Make( line.Clone( ( (Function3)line.Statement ).Param2 ) );
			_value3 = (IValue<T4>)Interpreter.Make( line.Clone( ( (Function3)line.Statement ).Param3 ) );
		}

		public override InterpreterStatus Execute(IContext c) {
			_value1.Execute( c );
			_value2.Execute( c );
			_value3.Execute( c );
			_retValue = _worker( _value1, _value2, _value3 );

			return InterpreterStatus.Ok;
		}

		public T1 Value { get { return _retValue; } }
		public string ValueAsString { get { return _retValue.ToString(); } }

		public override string ToString() { return "Func: " + Line.Statement.GetType().Name + " [" + ( _retValue != null ? ValueAsString : "" ) + "]"; }
	}

}
