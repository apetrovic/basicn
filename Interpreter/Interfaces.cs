// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using BasicN.Tokenizer;
using BasicN.Lib;

namespace BasicN.Interpreter {
	public enum InterpreterStatus { Run, End, Ok }

	public interface IItem {
		TLine Line { get; }
		InterpreterStatus Execute(IContext c);
	}

	public interface IValue : IItem {
		string ValueAsString { get; }
	}

	public interface IValue<T> : IValue {
		T Value { get; }
	}

	public interface IVariable {
		string Name { get; }
	}

	public interface IVariable<T> : IVariable, IValue<T> {
		void SetValue(T val);
	}
}
