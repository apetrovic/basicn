// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using BasicN.Parser;

namespace BasicN.Tokenizer {
	public class KwrJump : Statement {
		public int JumpPos { get; set; }
		internal bool Normalized { get; set; }

		public KwrJump(int jumpPos, bool normalized) {
			JumpPos = jumpPos;
			Normalized = normalized;
		}
	}

	public class KwrGoto : KwrJump {
		public KwrGoto(int jumpPos, bool normalized) : base( jumpPos, normalized ) { }
	}

	public class KwrGosub : KwrJump {
		public int ReturnAddress { get; set; }
		public KwrGosub(int jumpPos, bool normalized) : base( jumpPos, normalized ) {}
	}

	public class KwrJumpIfTrue : KwrJump {
		public BooleanStatement Condition { get; internal set; }

		public KwrJumpIfTrue(BooleanStatement condition, int jumpPos) : base( jumpPos, true ) {
			Condition = condition;
		}
	}

	public class KwrJumpIfNotTrue : KwrJump {
		public BooleanStatement Condition { get; internal set; }

		public KwrJumpIfNotTrue(BooleanStatement condition, int jumpPos) : base( jumpPos, true ) {
			Condition = condition;
		}
	}

	public class KwrPrint : Statement {
		public Statement Statement { get; set; }
		public bool NewLine { get; set; }

		public KwrPrint(Statement statement, bool newLine) {
			Statement = statement;
			NewLine = newLine;
		}
	}

	public class KwrDim : Statement {
		public VariableArray Array { get; private set; }
		public KwrDim(VariableArray arr) {
			Array = arr;
		}
	}
}
