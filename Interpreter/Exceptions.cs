using System;
using BasicN.Tokenizer;

namespace BasicN.Interpreter {
	public class InterpreterException : Exception {
		public TLine Line;
		public InterpreterException(TLine line, string message) : base( message ) {
			Line = line;
		} 
	}

	public class FactoryException : InterpreterException {
		public FactoryException(TLine line, string message) : base( line, message ) { } 
	}
}
