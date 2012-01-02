using System;
using System.Collections.Generic;
using BasicN.Tokenizer;
using BasicN.Parser;
using System.Reflection.Emit;
using System.Reflection;
using BasicN.Lib;

namespace BasicN.Compiler {
	internal class CompilingContext {
		public ILGenerator IL;
		public CompilerContext.OutputInterface Out;
		public Factory Factory;
				
		// gosub/return stuff
		public Label EndLabel;
		public Label ReturnLabel;
		public LocalBuilder GosubStack;

		// -- string conversion stuff
		public LocalBuilder DoubleConversionVar;
		public LocalBuilder CharConversionVar;

		// -- random
		public LocalBuilder Rnd;

		// -- read/data
		public FieldBuilder DataList;
		public FieldBuilder DataPointer;
		public LocalBuilder DataTempVariable;

		// -- variables
		private readonly Dictionary<string, FieldBuilder> Variables = new Dictionary<string, FieldBuilder>();
		private readonly Dictionary<string, FieldBuilder> Arrays = new Dictionary<string, FieldBuilder>();
		public TypeBuilder Program;

		private readonly Dictionary<int, Type> StringArrayTypes = new Dictionary<int, Type> {
			{ 1, typeof( string[] ) },
			{ 2, typeof( string[,] ) },
			{ 3, typeof( string[,,] ) },
			{ 4, typeof( string[,,,] ) }
		};

		private readonly Dictionary<int, Type> DoubleArrayTypes = new Dictionary<int, Type>() {
			{ 1, typeof( double[] ) },
			{ 2, typeof( double[,] ) },
			{ 3, typeof( double[,,] ) },
			{ 4, typeof( double[,,,] ) }
		};

		public FieldBuilder GetVariable(string name, Type variableType) {
			FieldBuilder ret;
			if( !Variables.TryGetValue( name, out ret ) ) {
				ret = Program.DefineField( name, variableType, FieldAttributes.Public );
				Variables.Add( name, ret );
			}

			return ret;
		}

		public FieldBuilder DeclareArray(string name, int rank) {
			bool isString = name.EndsWith( "$" );
			var dict = isString ? StringArrayTypes : DoubleArrayTypes;
			Type arrayType;
			
			if( !dict.TryGetValue( rank, out arrayType ) ) 
				throw new Exception( "Too manu dimensions for array " + name );

			FieldBuilder ret = Program.DefineField( name, arrayType, FieldAttributes.Public );
			Arrays.Add( name, ret );
			return ret;
		}

		public FieldBuilder GetArray(string name) {
			FieldBuilder ret;
			if( Arrays.TryGetValue( name, out ret ) )
				return ret;
			else
				throw new Exception( "Unknown array " + name );
		}

		private readonly Dictionary<int, Label> _labels = new Dictionary<int, Label>();
		private readonly Dictionary<int, int> _returnLabels = new Dictionary<int, int>();

		public void MakeLabel(int lineNum) {
			if( !_labels.ContainsKey( lineNum ) )
				_labels.Add( lineNum, IL.DefineLabel() );
		}

		public void MakeReturnLabel(int lineNum) {
			MakeLabel( lineNum );
			_returnLabels[lineNum] = lineNum;
		}

		public void MarkLabelIfNeeded(int lineNum) {
			Label l;
			if( _labels.TryGetValue( lineNum, out l ) )
				IL.MarkLabel( l );
		}

		public Label GetLabel(int lineNum) {
			Label l;
			if( _labels.TryGetValue( lineNum, out l ) )
				return l;

			throw new Exception( "Internal error: bad jump to line" + lineNum );
		}

		public IEnumerable<int> ReturnLabels() {
			foreach( var k in _returnLabels )
				yield return k.Value;
		}

		public int ReturnLabelsCount { get { return _returnLabels.Count; } }
	}


	public partial class NBCompiler {
		private static Factory GetFactory() {
			var f = new Factory();
			
			// Constants
			f.Add<StringConstant, string>( (c, i) => c.IL.Emit( OpCodes.Ldstr, i.Value ) );
			f.Add<NumConstant, double>( (c, i) => c.IL.Emit( OpCodes.Ldc_R8, i.Value ) );
			f.Add<Integer, double>( (c, i) => c.IL.Emit( OpCodes.Ldc_R8, (double)i.Value ) );

			// Variables
			f.AddVariable<StringVariable, string>();
			f.AddVariable<NumVariable, double>();
			f.AddArray<StringArray, string>();
			f.AddArray<NumArray, double>();

			// PRINT
			f.Add<KwrPrint>( (c, i) => {
				c.IL.Emit( OpCodes.Ldarg_1 );
				Type lastOpType = f.Execute( c, i.Statement );
				if( lastOpType == typeof( double ) ) { // we need to convert the result to string
					c.IL.Emit( OpCodes.Stloc, c.DoubleConversionVar );
					c.IL.Emit( OpCodes.Ldloca, c.DoubleConversionVar );
					c.IL.Emit( OpCodes.Call, typeof( Double ).GetMethod( "ToString", Type.EmptyTypes ) );
				}
				c.IL.Emit( OpCodes.Callvirt, i.NewLine ? c.Out.PrintLine : c.Out.Print );
			} );

			// REM
			f.Add<KwRem>( (c, i) => { } );

			// END
			f.Add<KwEnd>( (c, i) => c.IL.Emit( OpCodes.Br, c.EndLabel ) );

			// RETURN
			f.Add<KwReturn>( (c, i) => c.IL.Emit( OpCodes.Br, c.ReturnLabel ) );

			// RUN
			f.Add<KwRun>( (c, i) => {
				c.IL.Emit( OpCodes.Ldc_I4_0 );
				c.IL.Emit( OpCodes.Ret );
			} );

			// num binary operator
			f.Add<NumBinaryOperator, double>( (c, i) => {
				if( i.Operator == "=" ) {
					CVariable v = (CVariable)f.Make( i.Left.GetType().Name );
					v.Prepare( c, i.Left );
					f.Execute( c, i.Right );
					v.SetValue( c, i.Left );
					return;
				}

				f.Execute( c, i.Left );
				f.Execute( c, i.Right );

				switch( i.Operator ) {
					case "+": c.IL.Emit( OpCodes.Add ); break;
					case "-": c.IL.Emit( OpCodes.Sub ); break;
					case "*": c.IL.Emit( OpCodes.Mul ); break;
					case "/": c.IL.Emit( OpCodes.Div ); break;

					default:
						throw new Exception( "Unknown operator!" );
				}
			} );

			f.Add<NumUnaryMinus, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Neg );
			} );

			f.Add<NotOperator, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Conv_U4 );
				c.IL.Emit( OpCodes.Ldc_I4_M1 );
				c.IL.Emit( OpCodes.Xor );
				c.IL.Emit( OpCodes.Conv_R8 );
			} );

			// string binary operator
			f.Add<StringBinaryOperator, string>( (c, i) => {
				if( i.Operator == "=" ) {
					CVariable v = (CVariable)f.Make( i.Left.GetType().Name );
					v.Prepare( c, i.Left );
					f.Execute( c, i.Right );
					v.SetValue( c, i.Left );
					return;
				}

				f.Execute( c, i.Left );
				f.Execute( c, i.Right );

				switch( i.Operator ) {
					case "+": c.IL.Emit( OpCodes.Call, typeof( string ).GetMethod( "Concat", new[] { typeof( string ), typeof( string ) } ) ); break;

					default:
						throw new Exception( "Unknown operator!" );
				}
			} );

			// boolean binary operator
			f.Add<BoolBinaryOperator, bool>( (c, i) => {
				f.Execute( c, i.Left );
				f.Execute( c, i.Right );

				switch( i.Operator ) {
					case "^^": c.IL.Emit( OpCodes.Xor ); break;
					case "&&": c.IL.Emit( OpCodes.And ); break;
					case "||": c.IL.Emit( OpCodes.Or ); break;					
					default: throw new Exception( "Unknown operator!" );
				}
			} );

			f.Add<NumBoolBinaryOperator, bool>( (c, i) => {
				f.Execute( c, i.Left );
				f.Execute( c, i.Right );

				switch( i.Operator ) {
					case "==": c.IL.Emit( OpCodes.Ceq ); break;
					case "<": c.IL.Emit( OpCodes.Clt ); break;
					case ">": c.IL.Emit( OpCodes.Cgt ); break;

					case "<>":
						c.IL.Emit( OpCodes.Ceq );
						c.IL.Emit( OpCodes.Ldc_I4_0 );
						c.IL.Emit( OpCodes.Ceq );
						break;

					case ">=":
						c.IL.Emit( OpCodes.Clt );
						c.IL.Emit( OpCodes.Ldc_I4_0 );
						c.IL.Emit( OpCodes.Ceq );
						break;

					case "<=":
						c.IL.Emit( OpCodes.Cgt );
						c.IL.Emit( OpCodes.Ldc_I4_0 );
						c.IL.Emit( OpCodes.Ceq );
						break;

					default:
						throw new Exception( "Unknown operator!" );
				}
			} );

			f.Add<StringBoolBinaryOperator, bool>( (c, i) => {
				MethodInfo compare = typeof( string ).GetMethod( "Compare", new[] { typeof( string ), typeof( string ) } );

				f.Execute( c, i.Left );
				f.Execute( c, i.Right );
				c.IL.Emit( OpCodes.Call, compare );
				c.IL.Emit( OpCodes.Ldc_I4_0 );

				switch( i.Operator ) {
					case "==": c.IL.Emit( OpCodes.Ceq ); break;
					case "<": c.IL.Emit( OpCodes.Clt ); break;
					case ">": c.IL.Emit( OpCodes.Cgt ); break;

					case "<=":
						c.IL.Emit( OpCodes.Cgt );
						c.IL.Emit( OpCodes.Ldc_I4_0 );
						c.IL.Emit( OpCodes.Ceq );
						break;

					case ">=":
						c.IL.Emit( OpCodes.Clt );
						c.IL.Emit( OpCodes.Ldc_I4_0 );
						c.IL.Emit( OpCodes.Ceq );
						break;

					case "<>":
						c.IL.Emit( OpCodes.Ceq );
						c.IL.Emit( OpCodes.Ldc_I4_0 );
						c.IL.Emit( OpCodes.Ceq );
						break;

					default:
						throw new Exception( "Unknown operator!" );
				}
			} );

			f.Add<NotBooleanOperator, bool>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Ldc_I4_0 );
				c.IL.Emit( OpCodes.Ceq );
			} );

			// DIM
			f.Add<KwrDim>( (c, i) => {
				var tempTypes = new List<Type>();
				c.IL.Emit( OpCodes.Ldarg_0 );

				foreach( NumStatement num in i.Array.Dimensions ) {
					f.Execute( c, num );
					c.IL.Emit( OpCodes.Conv_I4 );
					
					c.IL.Emit( OpCodes.Ldc_I4_1 ); // we need to allocate one more item in the array
					c.IL.Emit( OpCodes.Add );

					tempTypes.Add( typeof( int ) );
				}

				FieldBuilder field = c.DeclareArray( i.Array.Name, tempTypes.Count );
				c.IL.Emit( OpCodes.Newobj, field.FieldType.GetConstructor( tempTypes.ToArray() ) );
				c.IL.Emit( OpCodes.Stfld, field );

				if( i.Array.Name.EndsWith( "$" ) ) { // we need to initialize string arrays
					c.IL.Emit( OpCodes.Ldarg_0 );
					c.IL.Emit( OpCodes.Ldfld, field );
					c.IL.Emit( OpCodes.Call, typeof( BNLib ).GetMethod( "InitStringArray" ) );
				}
			} );

			// GOTO
			f.Add<KwrGoto>( (c, i) => c.IL.Emit( OpCodes.Br, c.GetLabel( i.JumpPos ) ) );

			// GOSUB
			f.Add<KwrGosub>( (c, i) => {
				c.IL.Emit( OpCodes.Ldloc, c.GosubStack );
				c.IL.Emit( OpCodes.Ldc_I4, i.ReturnAddress );
				c.IL.Emit( OpCodes.Callvirt, typeof( Stack<int> ).GetMethod( "Push" ) );
				c.IL.Emit( OpCodes.Br, c.GetLabel( i.JumpPos ) );
			} );

			// READ
			f.Add<KwRead, string>( (c, i) => {
				CVariable v = (CVariable)f.Make( i.Variable.GetType().Name );
				v.Prepare( c, (Statement)i.Variable );

				// get the string...
				c.IL.Emit( OpCodes.Ldsfld, c.DataList );
				c.IL.Emit( OpCodes.Ldarg_0 );				
				c.IL.Emit( OpCodes.Ldfld, c.DataPointer );
				c.IL.Emit( OpCodes.Ldelem_Ref );

				// ... store the string in the temp variable...
				c.IL.Emit( OpCodes.Stloc, c.DataTempVariable );

				// ... increase data pointer...
				c.IL.Emit( OpCodes.Ldarg_0 );
				c.IL.Emit( OpCodes.Dup );
				c.IL.Emit( OpCodes.Ldfld, c.DataPointer );
				c.IL.Emit( OpCodes.Ldc_I4_1 );
				c.IL.Emit( OpCodes.Add );
				c.IL.Emit( OpCodes.Stfld, c.DataPointer );

				// ... and re-load the string
				c.IL.Emit( OpCodes.Ldloc, c.DataTempVariable );

				// do we need a conversion?
				if( v.VariableType == typeof( double ) ) {
					c.IL.Emit( OpCodes.Ldloca, c.DoubleConversionVar );
					MethodInfo m = typeof( double ).GetMethod( "TryParse", new[] { typeof( string ), typeof( double ).MakeByRefType() } );
					c.IL.Emit( OpCodes.Call, m );
					c.IL.Emit( OpCodes.Pop ); // we don't care about the return value
					c.IL.Emit( OpCodes.Ldloc, c.DoubleConversionVar ); // push the calculated value onto the stack
				}
				
				// finally, write the value into the variable
				v.SetValue( c, (Statement)i.Variable );
			} );

			// if
			f.Add<KwrJumpIfTrue>( (c, i) => {
				f.Execute( c, i.Condition );
				c.IL.Emit( OpCodes.Brtrue, c.GetLabel( i.JumpPos ) );
			} );

			f.Add<KwrJumpIfNotTrue>( (c, i) => {
				f.Execute( c, i.Condition );
				c.IL.Emit( OpCodes.Brfalse, c.GetLabel( i.JumpPos ) );
			} );

			// INPUT
			f.Add<KwInput>( (c, i) => {
				CVariable v = (CVariable)f.Make( i.Variable.GetType().Name );
				v.Prepare( c, (Statement)i.Variable );

				c.IL.Emit( OpCodes.Ldarg_1 ); // IContext
				
				// do we have a prompt?
				if( i.Prompt == null || string.IsNullOrEmpty( i.Prompt.Value ) )
					c.IL.Emit( OpCodes.Ldnull );
				else
					c.IL.Emit( OpCodes.Ldstr, i.Prompt.Value );

				// print the question mark after the prompt?
				if( i.Separator.HasValue && i.Separator.Value == ',' )
					c.IL.Emit( OpCodes.Ldc_I4_0 );
				else
					c.IL.Emit( OpCodes.Ldc_I4_1 );

				MethodInfo inputFunc = typeof( BNLib ).GetMethod( v.VariableType == typeof( double ) ? "InputDouble" : "InputString" );
				c.IL.Emit( OpCodes.Call, inputFunc );

				v.SetValue( c, (Statement)i.Variable );
			} );

			// CLS
			f.Add<KwCls>( (c, i) => {
				c.IL.Emit( OpCodes.Ldarg_1 );
				c.IL.Emit( OpCodes.Callvirt, c.Out.Cls );
			} );

			// RANDOMIZE
			f.Add<KwRandomize>( (c, i) => {
				if( i.Statement != null ) {
					f.Execute( c, i.Statement );
					c.IL.Emit( OpCodes.Conv_I4 );
					c.IL.Emit( OpCodes.Newobj, typeof( Random ).GetConstructor( new[] { typeof(int) } ) );
				}
				else {
					c.IL.Emit( OpCodes.Newobj, typeof( Random ).GetConstructor( Type.EmptyTypes ) );
				}
				
				c.IL.Emit( OpCodes.Stloc, c.Rnd );
			} );

			f.Add<KwLocate>( (c, i) => {
				c.IL.Emit( OpCodes.Ldarg_1 );
				f.Execute( c, i.X );
				c.IL.Emit( OpCodes.Conv_I4 );
				f.Execute( c, i.Y );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Callvirt, c.Out.Locate );
			} );

			// RND
			f.Add<NfRnd, double>( (c, i) => {
				c.IL.Emit( OpCodes.Ldloc, c.Rnd );
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Callvirt, typeof( Random ).GetMethod( "Next", new[] { typeof(int) } ) );
				c.IL.Emit( OpCodes.Conv_R8 );
			} );

			// TIMER
			f.Add<NfTimer, double>( (c, i) => {
				c.IL.Emit( OpCodes.Call, typeof( Environment ).GetMethod( "get_TickCount" ) );
				c.IL.Emit( OpCodes.Conv_R8 );
				c.IL.Emit( OpCodes.Ldc_R8, 1000.0 );
				c.IL.Emit( OpCodes.Div );
			} );

			// PAUSE
			f.Add<KwPause>( (c, i) => {
				f.Execute( c, i.Interval );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Call, typeof( System.Threading.Thread).GetMethod( "Sleep", new[] { typeof(int) } ) );
			} );

			// LEN
			f.Add<NfLen, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Call, typeof(string).GetMethod( "get_Length" ) );
				c.IL.Emit( OpCodes.Conv_R8 );
			} );

			f.Add<SfLeft, string>( (c, i) => {
				f.Execute( c, i.Statement );
				f.Execute( c, i.PositionStart );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Call, typeof( BNLib ).GetMethod( "Left" ) );
			} );

			f.Add<SfRight, string>( (c, i) => {
				f.Execute( c, i.Statement );
				f.Execute( c, i.PositionStart );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Call, typeof( BNLib ).GetMethod( "Right" ) );
			} );

			f.Add<SfMid, string>( (c, i) => {
				f.Execute( c, i.Statement );
				f.Execute( c, i.PositionStart );
				c.IL.Emit( OpCodes.Conv_I4 );
				f.Execute( c, i.PositionEnd );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Call, typeof( BNLib ).GetMethod( "Mid" ) );
			} );

			f.Add<SfStr, string>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Call, typeof( Double ).GetMethod( "ToString", Type.EmptyTypes ) );
			} );

			f.Add<NfAsc, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Ldc_I4_0 );
				c.IL.Emit( OpCodes.Call, typeof( string ).GetMethod( "get_Chars" ) );
				c.IL.Emit( OpCodes.Conv_R8 );
			} );

			f.Add<SfChr, string>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Conv_U2 );
				c.IL.Emit( OpCodes.Stloc, c.CharConversionVar );
				c.IL.Emit( OpCodes.Ldloca, c.CharConversionVar );
				c.IL.Emit( OpCodes.Call, typeof( char ).GetMethod( "ToString", Type.EmptyTypes ) );
			} );

			f.Add<NfVal, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Ldloca, c.DoubleConversionVar );
				c.IL.Emit( OpCodes.Call, typeof( double ).GetMethod( "TryParse", new[] { typeof( string ), typeof( double ).MakeByRefType() } ) );
				c.IL.Emit( OpCodes.Pop ); // we don't care about the return value
				c.IL.Emit( OpCodes.Ldloc, c.DoubleConversionVar ); 
			} );

			f.Add<NfInt, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Conv_R8 );
			} );

			f.Add<NfFrac, double>( (c, i) => {
				f.Execute( c, i.Statement );
				c.IL.Emit( OpCodes.Dup );
				c.IL.Emit( OpCodes.Conv_I4 );
				c.IL.Emit( OpCodes.Conv_R8 );
				c.IL.Emit( OpCodes.Sub );
			} );

			f.Add<SfInkey, string>( (c, i) => {
				c.IL.Emit( OpCodes.Ldarg_1 );
				c.IL.Emit( OpCodes.Call, c.Out.Read  );
			} );

			return f;
		}

		private static void DoCompile(CompilerContext outerContext, TokenizerOutput to) {
			var c = new CompilingContext {
				IL = outerContext.ProgramIL, 
				Out = outerContext.DefaultOutput, 
				Program = outerContext.ProgramType
			};

			CheckJumps( c, to );

			// string conversion stuff
			c.DoubleConversionVar = c.IL.DeclareLocal( typeof( Double ) );
			c.CharConversionVar = c.IL.DeclareLocal( typeof( char ) );
 
			// data / read
			c.DataList = outerContext.ProgramDataList;
			c.DataPointer = c.Program.DefineField( "DataPointer", typeof( int ), FieldAttributes.Public );
			c.DataTempVariable = c.IL.DeclareLocal( typeof( string ) );
			c.IL.Emit( OpCodes.Ldarg_0 );
			c.IL.Emit( OpCodes.Ldc_I4, 0 );
			c.IL.Emit( OpCodes.Stfld, c.DataPointer );

			// gosub/return stack
			c.GosubStack = c.IL.DeclareLocal( typeof( Stack<int> ) );
			c.IL.Emit( OpCodes.Newobj, typeof( Stack<int> ).GetConstructor( Type.EmptyTypes ) );
			c.IL.Emit( OpCodes.Stloc, c.GosubStack );

			// rnd
			c.Rnd = c.IL.DeclareLocal( typeof( Random ) );
			c.IL.Emit( OpCodes.Newobj, typeof( Random ).GetConstructor( Type.EmptyTypes ) );
			c.IL.Emit( OpCodes.Stloc, c.Rnd );

			c.EndLabel = c.IL.DefineLabel();
			c.ReturnLabel = c.IL.DefineLabel();

			// switch off the cursor
			//c.IL.Emit( OpCodes.Ldc_I4_0 );
			//c.IL.Emit( OpCodes.Call, typeof( Console ).GetMethod( "set_CursorVisible" ) );
			
			Factory f = GetFactory();
			c.Factory = f;
			for( int i = 0; i < to.Program.Count; ++i ) {
				TLine line = to.Program[i];
				c.MarkLabelIfNeeded( i );
				try {
					f.Execute( c, line.Statement );
				}
				catch( Exception e ) {
					Console.WriteLine( "Error : " + e.Message );
					Console.WriteLine( "Line: " + (line.OriginalLine.LineNum ?? -1).ToString() );
					Console.WriteLine( line.OriginalLine.OriginalLine);
				}
			}

			// switch the cursor on
			//c.IL.Emit( OpCodes.Ldc_I4_1 );
			//c.IL.Emit( OpCodes.Call, typeof( Console ).GetMethod( "set_CursorVisible" ) );

			MakeReturn( c );	

			// return
			c.IL.MarkLabel( c.EndLabel );
			c.IL.Emit( OpCodes.Ldc_I4_1 );
			c.IL.Emit( OpCodes.Ret );
		}

		private static void CheckJumps(CompilingContext context, TokenizerOutput program) {
			for( int i = 0; i < program.Program.Count; ++i ) {
				Statement s = program.Program[i].Statement;
				if( s is KwrJump ) {
					if( s is KwrGosub )
						context.MakeReturnLabel( ((KwrGosub)s).ReturnAddress );

					KwrJump jmp = s as KwrJump;
					context.MakeLabel( jmp.JumpPos );
				}
			}
		}

		private static void MakeReturn(CompilingContext c) {
			c.IL.MarkLabel( c.ReturnLabel );

			if( c.ReturnLabelsCount == 0 )
				return;

			// check if stack is empty
			Label okLabel = c.IL.DefineLabel();
			c.IL.Emit( OpCodes.Ldloc, c.GosubStack );
			c.IL.Emit( OpCodes.Callvirt, typeof( Stack<int> ).GetMethod( "get_Count" ) );
			c.IL.Emit( OpCodes.Brtrue, okLabel );
			c.IL.Emit( OpCodes.Ldstr, "RETURN without GOSUB" );
			c.IL.Emit( OpCodes.Newobj, typeof( Exception ).GetConstructor( new[] { typeof( string ) } ) );
			c.IL.Emit( OpCodes.Throw );
			
			c.IL.MarkLabel( okLabel );
			
			LocalBuilder popValue = c.IL.DeclareLocal( typeof( int ) );
			c.IL.Emit( OpCodes.Ldloc, c.GosubStack );
			c.IL.Emit( OpCodes.Call, typeof( Stack<int> ).GetMethod( "Pop" ) );
			c.IL.Emit( OpCodes.Stloc, popValue );
			
			foreach( int label in c.ReturnLabels() ) {
				Label returnLabel = c.GetLabel( label );
				c.IL.Emit( OpCodes.Ldloc, popValue );
				c.IL.Emit( OpCodes.Ldc_I4, label );
				c.IL.Emit( OpCodes.Beq, returnLabel );
			}
		}
	}
}
