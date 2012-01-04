// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using BasicN.Parser;

namespace BasicN.Tokenizer {
	public interface IErrorPrinter {
		void PrintError(string message);
	}

	public class TLine {
		public Statement Statement;
		public Line OriginalLine;

		public TLine(Line line, Statement statement) {
			Statement = statement;
			OriginalLine = line;
		}

		public TLine Clone(Statement statement) {
			return new TLine( OriginalLine, statement );
		}
	}
}
