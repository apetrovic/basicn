using System;
using System.Collections.Generic;

namespace BasicN.Parser {
	
	public static class ArrayHelper {
		public static IEnumerable<T> Map<T>(IEnumerable<T> source, Func<T, T> func) {
			foreach( var elem in source )
				yield return func( elem );
		}
	}

	public class Token {
		public Tokens Kind { get; set; }
		public string Str { get; set; }
		public Token(Tokens t, string s) { Kind = t; Str = s; }
	}

	public interface Statement {}

	//---

	public interface StringStatement : Statement { }
	public interface NumStatement : Statement { }
	public interface Variable { string Name { get; } }
	public interface VariableArray : Variable { IList<NumStatement> Dimensions { get; } }

	public class BinaryOperator : Statement {
		public Statement Left { get; private set; }
		public Statement Right { get; private set; }
		public string Operator { get; private set; }

		public BinaryOperator(Statement l, Statement r, string o) { Left = l; Right = r; Operator = o; }
	}

	public class NumVariable : NumStatement, Variable {
		public string Name { get; private set; }
		public NumVariable(string name) { Name = name.ToUpper(); }
	}

	public class NumArray : NumStatement, VariableArray {
		public string Name { get; private set; }
		public IList<NumStatement> Dimensions { get; private set; }

		public NumArray(string name, IList<NumStatement> dimensions) { Name = name.ToUpper(); Dimensions = dimensions; }
	}

	public class StringVariable : StringStatement, Variable {
		public string Name { get; private set; }
		public StringVariable(string name) { Name = name.ToUpper(); }
	}

	public class StringArray : StringStatement, VariableArray {
		public string Name { get; private set; }
		public IList<NumStatement> Dimensions { get; private set; }

		public StringArray(string name, IList<NumStatement> dimensions) { Name = name.ToUpper(); Dimensions = dimensions; }
	}

	public class Integer: NumStatement {
		public int Value { get; private set; }
		public Integer(string val) { Value = int.Parse( val ); }
	}

	public class NumConstant : NumStatement {
		public double Value { get; private set; }
		public NumConstant(string val) { Value = double.Parse( val ); } 
	}

	public class StringConstant : StringStatement {
		public string Value { get; private set; }
		public StringConstant(string val) { Value = val.Trim('"'); }
	}

	public class NumUnaryMinus : NumStatement {
		public NumStatement Statement  { get; private set; }
		public NumUnaryMinus(NumStatement st) { Statement = st; }
	}

	public class NotOperator : NumStatement {
		public NumStatement Statement { get; private set; }
		public NotOperator(NumStatement st) { Statement = st; }
	}

	public class NumBinaryOperator : BinaryOperator, NumStatement {
		public NumBinaryOperator(NumStatement l, NumStatement r, string op) : base( l, r, op ) { }
	}

	public class StringBinaryOperator : BinaryOperator, StringStatement {
		public StringBinaryOperator(StringStatement l, StringStatement r, string op) : base( l, r, op ) { }
	}

	public interface BooleanStatement : Statement { }

	public class BoolBinaryOperator : BinaryOperator, BooleanStatement {
		public BoolBinaryOperator(BooleanStatement l, BooleanStatement r, string op) : base( l, r, op ) { }
	}

	public class NumBoolBinaryOperator : BinaryOperator, BooleanStatement {
		public NumBoolBinaryOperator(NumStatement l, NumStatement r, string op) : base( l, r, op ) { }
	}

	public class StringBoolBinaryOperator : BinaryOperator, BooleanStatement {
		public StringBoolBinaryOperator(StringStatement l, StringStatement r, string op) : base( l, r, op ) { }
	}

	public class NotBooleanOperator : BooleanStatement {
		public BooleanStatement Statement { get; private set; }
		public NotBooleanOperator(BooleanStatement stat) { Statement = stat; }
	}

	public interface Keyword : Statement { }


	public class KwInput : Keyword {
		public Variable Variable { get; private set; }
		public StringConstant Prompt { get; private set; }
		public char? Separator { get; private set; }

		public KwInput(Variable var) { Variable = var; }
		public KwInput(Variable var, StringConstant str, char separator) { Variable = var; Prompt = str; Separator = separator; }
	}

	public class Jump : Keyword {
		public Integer Value { get; private set; }
		public Jump(Integer i) { Value = i; }
	}

	public class KwGoto : Jump {
		public KwGoto(Integer i) : base( i ) {}
	}

	public class KwGosub : Jump {
		public KwGosub(Integer i) : base( i ) {}
	}

	public class KwReturn : Keyword {}

	public class KwRun : Keyword {}

	public class KwCls : Keyword {}

	public class KwEnd : Keyword {}

	public class KwRem : Keyword {
		private readonly Token _token;
		private string _comment;
		public string Comment {
			get {
				if( _comment == null )
					_comment = _token.Str.Trim().Substring( 4 );
				return _comment;
			}
		}
		public KwRem(Token t) { _token = t; }
		public KwRem(string comment) { _comment = comment; }
	}

	public class KwData : Keyword {
		private readonly Token _token;
		private List<string> _data;

		public List<string> Data {
			get {
				if( _data == null ) {
					string[] dt = _token.Str.Trim().Substring(5).Split( ',' );
					_data = new List<string>( ArrayHelper.Map( dt, elem => elem.Trim() ) );
				}
				return _data;
			}
		}

		public KwData(Token t) { _token = t; }
	}



	public class KwDim : Keyword {
		public List<VariableArray> ArrayList { get; private set; }
		public KwDim(List<VariableArray> array) { ArrayList = array; }
	}

	public class KwLocate : Keyword {
		public NumStatement X { get; private set; }
		public NumStatement Y { get; private set; }
		public KwLocate(NumStatement x, NumStatement y) { X = x; Y = y; }
	}

	public class KwPause : Keyword {
		public NumStatement Interval { get; private set; }
		public KwPause(NumStatement interval) { Interval = interval; }
	}

	public class KwRead : Keyword {
		public Variable Variable { get; private set; }
		public KwRead(Variable variable) { Variable = variable; }
	}

	public class KwRandomize : Keyword {
		public NumStatement Statement { get; private set; }
		public KwRandomize(NumStatement numStatement) { Statement = numStatement; }
	}

	public interface Function { }
	public interface Function1 : Function {
		Statement Param1 { get; }
	}

	public interface Function2 : Function1 {
		Statement Param2 { get; }
	}

	public interface Function3 : Function2 {
		Statement Param3 { get; }
	}
	
	public interface StringFunction : StringStatement { }

	public class SfMid : StringFunction, Function3 {
		public StringStatement Statement { get; private set; }
		public NumStatement PositionStart { get; private set; }
		public NumStatement PositionEnd { get; private set; }

		public SfMid(StringStatement statement, NumStatement positionStart, NumStatement positionEnd) {
			Statement = statement;
			PositionStart = positionStart;
			PositionEnd = positionEnd;
		}

		public Statement Param1 { get { return Statement; } }
		public Statement Param2 { get { return PositionStart; } }
		public Statement Param3 { get { return PositionEnd; } }
	}

	public class SfLeft : StringFunction, Function2 {
		public StringStatement Statement { get; private set; }
		public NumStatement PositionStart { get; private set; }

		public SfLeft(StringStatement statement, NumStatement positionStart) {
			Statement = statement;
			PositionStart = positionStart;
		}

		public Statement Param1 { get { return Statement; } }
		public Statement Param2 { get { return PositionStart; } }
	}

	public class SfRight : StringFunction, Function2 {
		public StringStatement Statement { get; private set; }
		public NumStatement PositionStart { get; private set; }

		public SfRight(StringStatement statement, NumStatement positionStart) {
			Statement = statement;
			PositionStart = positionStart;
		}

		public Statement Param1 { get { return Statement; } }
		public Statement Param2 { get { return PositionStart; } }
	}

	public class SfStr : StringFunction, Function1 {
		public NumStatement Statement { get; private set; }
		public SfStr(NumStatement numStatement) { Statement = numStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class SfChr : StringFunction, Function1 {
		public NumStatement Statement { get; private set; }
		public SfChr(NumStatement numStatement) { Statement = numStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class SfInkey : StringFunction, Function {}

	public interface NumFunction : NumStatement { }
	
	public class NfLen : NumFunction, Function1 {
		public StringStatement Statement { get; private set; }
		public NfLen(StringStatement stringStatement) { Statement = stringStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class NfAsc : NumFunction, Function1 {
		public StringStatement Statement { get; private set; }
		public NfAsc(StringStatement stringStatement) { Statement = stringStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class NfVal : NumFunction, Function1 {
		public StringStatement Statement { get; private set; }
		public NfVal(StringStatement stringStatement) { Statement = stringStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class NfInt : NumFunction, Function1 {
		public NumStatement Statement { get; private set; }
		public NfInt(NumStatement numStatement) { Statement = numStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class NfFrac : NumFunction, Function1 {
		public NumStatement Statement { get; private set; }
		public NfFrac(NumStatement numStatement) { Statement = numStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class NfRnd : NumFunction, Function1 {
		public NumStatement Statement { get; private set; }
		public NfRnd(NumStatement numStatement) { Statement = numStatement; }
		public Statement Param1 { get { return Statement; } }
	}

	public class NfTimer : NumFunction, Function {}

	// keywords to be replaced with simpler commands in the tokenizer step

	public class KwIf : Keyword {
		public BooleanStatement Condition { get; private set; }
		public List<Statement> Statements { get; private set; }
		public List<Statement> ElseStatements { get; private set; }

		public KwIf(BooleanStatement cond, List<Statement> stat, List<Statement> elseStat) { Condition = cond; Statements = stat; ElseStatements = elseStat; }
	}

	public class KwOn : Keyword {
		public enum OnKind { Goto, Gosub };
		public OnKind Kind { get; private set; }
		public List<Integer> JumpList { get; private set; }
		public NumStatement Statement { get; private set; }

		public KwOn(NumStatement statement, object list) {
			Statement = statement;
			var l = (KwOnJumpList)list;
			Kind = l.Kind;
			JumpList = l.JumpList;
		}

		public static KwOnJumpList MakeGotoJumpList(object jumpList) { return new KwOnJumpList( OnKind.Goto, (List<Integer>)jumpList ); }
		public static KwOnJumpList MakeGosubJumpList(object jumpList) { return new KwOnJumpList( OnKind.Gosub, (List<Integer>)jumpList ); }

		public class KwOnJumpList {
			public OnKind Kind;
			public List<Integer> JumpList;

			public KwOnJumpList(OnKind kind, List<Integer> jumpList) { Kind = kind; JumpList = jumpList; }
		}
	}

	public class KwFor : Keyword {
		public NumVariable Variable { get; private set; }
		public NumStatement Initial { get; private set; }
		public NumStatement End { get; private set; }
		public NumStatement Step { get; private set; }

		public KwFor(NumVariable var, NumStatement initial, NumStatement end, NumStatement step) {
			Variable = var;
			Initial = initial;
			End = end;
			Step = step;
		}
	}

	public class KwNext : Keyword {
		public NumVariable Variable { get; private set; }
		public KwNext(NumVariable var) { Variable = var; }
	}

	public class KwPrint : Keyword {
		public class Group {
			public Statement Statement;
			public string EndChar;

			public Group(Statement s) : this( s, null ) { }
			public Group(Statement s, string ec) { Statement = s; EndChar = ec; }
		}

		public List<Group> PrintList { get; private set; }

		public KwPrint() { }
		public KwPrint(Statement s) { PrintList = new List<Group> { new Group( s ) }; }
		public KwPrint(object list) { SetList( list ); }
		public KwPrint(object list, Statement s) { SetList( list ); PrintList.Add( new Group( s ) ); }

		public void SetList(object list) {
			PrintList = (List<Group>)list;
		}

		public static List<Group> AddToList(object l, object g) {
			List<Group> ls = l as List<Group> ?? new List<Group>();
			ls.Add( (Group)g );
			return ls;
		}
	}
}
