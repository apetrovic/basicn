using System.Collections.Generic;

namespace BasicN.Parser {
	public struct BNValue {
		private object _val;

		public object Value {
			set { _val = value; }
			get { return _val; }
		}

		public Token Token {
			set { _val = value; }
			get { return (Token)_val; }
		}

		public Statement Statement {
			set { _val = value; }
			get { return (Statement)_val; }
		}

		public List<Statement> Statements {
			set { _val = value; }
			get { return (List<Statement>)_val; }
		}

		public StringStatement StringStatement {
			set { _val = value; }
			get { return (StringStatement)_val; }
		}

		public NumStatement NumStatement {
			set { _val = value; }
			get { return (NumStatement)_val; }
		}

		public BooleanStatement BooleanStatement {
			set { _val = value; }
			get { return (BooleanStatement)_val; }
		}

		public Variable Variable {
			set { _val = value; }
			get { return (Variable)_val; }
		}

		public Keyword Keyword {
			set { _val = value; }
			get { return (Keyword)_val; }
		}
	}
}
