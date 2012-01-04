// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using System.Collections.Generic;
using BasicN.Parser;
using BasicN.Tokenizer;
using BasicN.Lib;
using System.Text;

namespace BasicN.Interpreter {

	public interface IValueStore {
		void Reset();
	}

	public class ValueStore<T> : IValueStore {
		private readonly T _initial;

		public ValueStore(T initialValue) {
			_initial = initialValue;
			Value = _initial;
		}

		public T Value { get; set; }

		public void Reset() {
			Value = _initial;
		}

		public override string ToString() {
			return typeof(T) == typeof(double) ? Value.ToString() : "'" + Value + "'";
		}
	}

	public class ArrayValueStore<T> : IValueStore {
		public List<int> Dimensions = new List<int>();
		private readonly List<int> Multipliers = new List<int>();
		protected readonly int _size;
		public T[] Value;

		public ArrayValueStore(IEnumerable<int> dimensions) {
			_size = 1;
			foreach( int dim in dimensions ) {
				Dimensions.Add( dim );
				Multipliers.Add( _size );
				_size *= ( dim + 1 );
			}

			Reset();
		}

		public virtual void Reset() {
			Value = new T[_size];
		}

		public T this[List<int> dimensions] {
			get {
				int pos = GetPos( dimensions );
				return Value[pos];
			}

			set {
				int pos = GetPos( dimensions );
				Value[pos] = value;
			}
		}

		public T GetValue(List<int> dimensions) {
			int pos = GetPos( dimensions );
			return Value[pos];
		}

		public void SetValue(List<int> dimensions, T value) {
			int pos = GetPos( dimensions );
			Value[pos] = value;
		}

		private int GetPos(IList<int> dimensions) {
			if( dimensions.Count != Dimensions.Count )
				throw new Exception( "Wrong number of dimensions" );

			int pos = 0;
			for( int i = 0; i < Dimensions.Count; ++i ) {
				if( dimensions[i] > Dimensions[i] )
					throw new Exception( "Dimension out of range, dimension: " + ( i + 1 ) + " max: " + Dimensions[i] + " value: " + dimensions[i] );

				pos += dimensions[i] * Multipliers[i];
			}
			return pos;
		}

		public override string ToString() {
			Func<T, string> toString = t => typeof( T ) == typeof( double ) ? t.ToString() : "'" + t + "'";
			StringBuilder sb = null;
			foreach( T t in Value ) {
				if( sb == null )
					sb = new StringBuilder( toString( t ) );
				else
					sb.Append( ", " + toString(t) );
			}

			return sb == null ? "" : sb.ToString();
		}
	}

	public class StringArrayStore: ArrayValueStore<string> {
		public StringArrayStore(IEnumerable<int> dimensions) : base( dimensions ) {}
		public override void Reset() {
			base.Reset();
			for( int i = 0; i < _size; ++i )
				Value[i] = "";
		}
	}

	public abstract class BaseItem : IItem {
		public TLine Line { get; protected set; }
		public Line OriginalLine { get { return Line.OriginalLine; } }
		protected NBInterpreter Interpreter { get; set; }

		protected BaseItem(TLine line, NBInterpreter interpreter) {
			Line = line;
			Interpreter = interpreter;
		}

		public abstract InterpreterStatus Execute(IContext c);

		public abstract override string ToString();
	}


	public class Constant<T> : BaseItem, IValue<T> {
		public T Value { get; protected set; }
		public string ValueAsString { get { return Value.ToString(); } }

		public Constant(TLine pl, NBInterpreter i, Func<Statement, T> getter) : base( pl, i ) {
			Value = getter( pl.Statement );
		}

		//internal Constant(TLine pl, T val) : base( pl ) { Value = val; }

		public override InterpreterStatus Execute(IContext c) { return InterpreterStatus.Ok;  }

		public override string ToString() { return "{" + ValueAsString + "}"; }
	}

	public class Variable<T> : BaseItem, IVariable<T> {
		ValueStore<T> _store;

		public string Name { get; protected set; }

		public Variable(TLine pl, NBInterpreter i, Func<Statement, string> nameGetter) : base( pl, i ) {
			Name = nameGetter( pl.Statement );
		}

		public void SetValue(T val) {
			_store.Value = val;
		}

		public void SetStore(ValueStore<T> store) {
			_store = store;
		}

		public T Value {
			get { return _store.Value; }
		}

		public string ValueAsString {
			get { return _store.Value.ToString(); }
		}

		public override InterpreterStatus Execute(IContext c) { return InterpreterStatus.Ok; }

		public override string ToString() { return "Var: " + Name + " [=" + ValueAsString + "]"; }
	}


	public class ArrayVariable<T> : BaseItem, IVariable<T> {
		ArrayValueStore<T> _store;
		List<IValue<double>> _coordinates = new List<IValue<double>>();
		List<int> _dimensions;

		public string Name { get; protected set; }

		public ArrayVariable(TLine pl, NBInterpreter i, Func<Statement, string> nameGetter) : base( pl, i ) {
			Name = nameGetter( pl.Statement );

			VariableArray va = (VariableArray)pl.Statement;
			foreach( var dimension in va.Dimensions ) {
				var coordinate = (IValue<double>)Interpreter.Make( pl.Clone( dimension ) );
				_coordinates.Add( coordinate );
			}
		}


		public override InterpreterStatus Execute(IContext c) {
			if( _store == null ) {
				IValueStore store;
				if( !Interpreter.Arrays.TryGetValue( Name, out store ) || (_store = (ArrayValueStore<T>)store) == null )
					throw new InterpreterException( Line, "Unknown array " + Name );
			}

			_dimensions = new List<int>();
			foreach( var coordinate in _coordinates ) {
				var ret = coordinate.Execute( c );
				if( ret != InterpreterStatus.Ok )
					return ret;

				int val = (int)coordinate.Value;
				_dimensions.Add( val );
			}

			return InterpreterStatus.Ok;
		}

		public void SetValue(T val) {
			_store[_dimensions] = val;
		}


		public T Value {
			get { return _store[_dimensions]; }
		}

		public string ValueAsString {
			get { return Value.ToString(); }
		}

		public override string ToString() {
			string ret = "Var: " + Name + "( ";

			string dm = "";
			if( _dimensions != null )
				foreach( int d in _dimensions )
					dm += dm.Length == 0 ? d.ToString() : ", " + d.ToString();

			ret += ( dm + " )" );

			if( _store != null )
				ret += " [=" + ValueAsString + "]";

			return ret;
		}
	}
}
