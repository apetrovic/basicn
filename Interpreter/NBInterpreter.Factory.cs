// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using System.Collections.Generic;
using BasicN.Parser;
using BasicN.Tokenizer;
using BasicN.Lib;

namespace BasicN.Interpreter {
	public partial class NBInterpreter {
		private readonly Dictionary<string, Func<TLine, NBInterpreter, IItem>> types = new Dictionary<string, Func<TLine, NBInterpreter, IItem>> {
			{ typeof( StringConstant ).Name, ( p, i ) => new Constant<string>( p, i, s => ((StringConstant)s).Value) },
			{ typeof( NumConstant ).Name, ( p, i ) => new Constant<double>( p, i, s => ((NumConstant)s).Value) },
			{ typeof( Integer ).Name, ( p, i ) => new Constant<double>( p, i, s => ((Integer)s).Value) },
			{ typeof( NumVariable ).Name, ( p, i ) => new Variable<double>( p, i, s => ((NumVariable)s).Name ) },
			{ typeof( StringVariable ).Name, ( p, i ) => new Variable<string>( p, i, s => ((StringVariable)s).Name ) },
			{ typeof( NumArray ).Name, ( p, i ) => new ArrayVariable<double>( p, i, s => ((NumArray)s).Name ) },
			{ typeof( StringArray ).Name, ( p, i ) => new ArrayVariable<string>( p, i, s => ((StringArray)s).Name ) },
			{ typeof( NumUnaryMinus ).Name, ( p, i ) => new UnaryOperator<double>( p, i, s => ((NumUnaryMinus)s).Statement, v => -v ) },
			{ typeof( NotOperator ).Name, ( p, i ) => new UnaryOperator<double>( p, i, s => ((NotOperator)s).Statement, v => (double)((int)((uint)v ^ uint.MaxValue)) ) },
			{ typeof( NotBooleanOperator ).Name, ( p, i ) => new UnaryOperator<bool>( p, i, s =>((NotBooleanOperator)s).Statement, v => !v ) },
			{ typeof( NumBinaryOperator ).Name, ( p, i ) => new BinaryOperatorDouble( p, i ) },
			{ typeof( StringBinaryOperator ).Name, ( p, i ) => new BinaryOperatorString( p, i ) },
			{ typeof( BoolBinaryOperator ).Name, ( p, i ) => new BinaryOperatorBool( p, i ) },
			{ typeof( NumBoolBinaryOperator ).Name, ( p, i ) => new BinaryOperatorBoolDouble( p, i ) },
			{ typeof( StringBoolBinaryOperator ).Name, ( p, i ) => new BinaryOperatorBoolString( p, i ) },
			{ typeof( KwrDim ).Name, ( p, i ) => new DimCommand( p, i ) },
			{ typeof( KwReturn ).Name, ( p, i ) => new ReturnCommand( p, i ) },
			{ typeof( KwEnd ).Name, ( p, i ) => new EndCommand( p, i ) },
			{ typeof( KwRem ).Name, ( p, i ) => new NoOp( p, i ) },
			{ typeof( KwInput ).Name, ( p, i ) => new InputCommand( p, i ) },
			{ typeof( KwRead ).Name, ( p, i ) => new ReadCommand( p, i ) },
			{ typeof( KwCls ).Name, ( p, i ) => new ClsCommand( p, i ) },
			{ typeof( KwRun ).Name, ( p, i ) => new RunCommand( p, i ) },
			{ typeof( KwLocate ).Name, ( p, i ) => new LocateCommand( p, i ) },
			{ typeof( KwRandomize ).Name, ( p, i ) => new RandomizeCommand( p, i ) },
			{ typeof( KwPause).Name, ( p, i ) => new PauseCommand( p, i ) },
			{ typeof( NfRnd ).Name, ( p, i ) => new RndCommand( p, i ) },
			{ typeof( NfTimer ).Name, ( p, i ) => new FunctionImp<double>( p, i, c => Environment.TickCount / 1000.0 ) },
			{ typeof( NfLen ).Name, ( p, i ) => new FunctionImp<double, string>( p, i, v => (double)v.Value.Length ) },
			{ typeof( SfLeft ).Name, ( p, i ) => new FunctionImp<string, string, double>( p, i, (s, v) => BNLib.Left( s.Value, (int)v.Value) ) },
			{ typeof( SfRight ).Name, ( p, i ) => new FunctionImp<string, string, double>( p, i, (s, v) => BNLib.Right( s.Value, (int)v.Value ) ) },
			{ typeof( SfMid ).Name, ( p, i ) => new FunctionImp<string, string, double, double>( p, i, (s, start, len) => BNLib.Mid( s.Value, (int)start.Value, (int)len.Value ) ) },
			{ typeof( SfStr ).Name, ( p, i ) => new FunctionImp<string, double>( p, i, s => s.Value.ToString() ) },
			{ typeof( NfAsc ).Name, ( p, i ) => new FunctionImp<double, string>( p, i, s => (double)(int)s.Value[0] ) },
			{ typeof( SfChr ).Name, ( p, i ) => new FunctionImp<string, double>( p, i, s => ((char)s.Value).ToString() ) },
			{ typeof( NfVal ).Name, ( p, i ) => new FunctionImp<double, string>( p, i, s => { double ret; double.TryParse( s.Value, out ret ); return ret; } ) },
			{ typeof( NfInt ).Name, ( p, i ) => new FunctionImp<double, double>( p, i, s => (double)(int)s.Value ) },
			{ typeof( NfFrac ).Name, ( p, i ) => new FunctionImp<double, double>( p, i, s => s.Value - (int)s.Value ) },
			{ typeof( SfInkey ).Name, ( p, i ) => new FunctionImp<string>( p, i, c => c.Read() ) },
			{ typeof( KwrGoto ).Name, ( p, i ) => new GotoCommand( p, i ) },
			{ typeof( KwrGosub ).Name, ( p, i ) => new GosubCommand( p, i ) },
			{ typeof( KwrPrint ).Name, ( p, i ) => new PrintCommand( p, i ) },
			{ typeof( KwrJumpIfNotTrue).Name, ( p, i ) => new JumpIfNotTrueCommand( p, i ) },
			{ typeof( KwrJumpIfTrue).Name, ( p, i ) => new JumpIfTrueCommand( p, i ) },
		};

		public IItem Make(TLine programLine) {
			if( programLine.Statement is Variable )
				return MakeVariable( programLine );
			else
				return DoMake( programLine );
		}

		private IItem DoMake(TLine line) {
			Func<TLine, NBInterpreter, IItem> f;
			string type = line.Statement.GetType().Name;
			if( types.TryGetValue( type, out f ) )
				return f( line, this );

			throw new FactoryException( line, "Unknown type " + type );
		}

		private IItem MakeVariable(TLine programLine) {
			IVariable v = (IVariable)DoMake( programLine );
			InitVariable( programLine, v );
			return (IItem)v;
		}

		private void InitVariable(TLine programLine, IVariable v) {
			IValueStore vs;

			if( v is Variable<double> ) {
				var vvar = (Variable<double>)v;

				Variables.TryGetValue( v.Name, out vs );
				var dvs = vs as ValueStore<double>;

				if( dvs == null ) {
					dvs = new ValueStore<double>( 0 );
					Variables.Add( v.Name, dvs );
				}

				vvar.SetStore( dvs );
			}
			else if( v is Variable<string> ) {
				var vvar = (Variable<string>)v;

				Variables.TryGetValue( v.Name, out vs );
				var dvs = vs as ValueStore<string>;

				if( dvs == null ) {
					dvs = new ValueStore<string>( "" );
					Variables.Add( v.Name, dvs );
				}

				vvar.SetStore( dvs );
			}
			else if( !(v is ArrayVariable<double> || v is ArrayVariable<string>) ) {
				throw new InterpreterException( programLine, "Unknown variable" );
			}
		}
	}
}
