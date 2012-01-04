// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace BasicN {
	public delegate TResult Func<T1, TResult>(T1 t1);
	public delegate TResult Func<T1, T2, TResult>(T1 t1, T2 t2);
	public delegate TResult Func<T1, T2, T3, TResult>(T1 t1, T2 t2, T3 t3);
	public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 t1, T2 t2, T3 t3, T4 t4);

	public delegate void Action<T1>(T1 t1);
	public delegate void Action<T1, T2>(T1 t1, T2 t2);
	public delegate void Action<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
}

namespace BasicN.Parser {
	public sealed partial class Scanner {
		public string ErrorReport;
		public int? ErrorColumn;
		public string ErrorText;
		public override void yyerror(string format, params object[] args) {
			ErrorReport =  String.Format( format, args );
			ErrorColumn = yycol + 1;
			ErrorText = yytext;
		}
	}

	public partial class BNParser {
		public BNParser(Scanner scanner) : base( scanner ) { }

		public List<Statement> Output;

		public static List<T> AddToList<T>(List<T> l, T s) {
			if( l != null )
				l.Add( s );

			return l;
		}
	}

	public enum ParseReport { Ok, SyntaxError, EmptyLine, LineFormatError }

	public class ParsedStatements {
		public List<Statement> Statements;
		public string ErrorMessage;
		public int? ErrorColumn;

		public ParseReport Report = ParseReport.EmptyLine;
	}

	public class Line : ParsedStatements {
		public int? LineNum;
		public int? OriginalLinePosition;
		public string OriginalLine;
	}

	public class BasicNParser {
		public BasicNParser() { }

		public ParsedStatements ParseStatements(string statements, ParsedStatements ret) {
			if( ret == null )
				ret = new ParsedStatements();

			string st = statements.Trim();
			if( String.IsNullOrEmpty( st ) ) {
				ret.ErrorMessage = "EmptyLine";
				return ret;
			}

			var sr = new MemoryStream( new ASCIIEncoding().GetBytes( st ) );
			var scanner = new Scanner( sr );
			var parser = new BNParser( scanner );
			bool r = parser.Parse();

			if( !r ) {
				ret.Report = ParseReport.SyntaxError;
				ret.ErrorMessage = scanner.ErrorReport;
				ret.ErrorColumn = scanner.ErrorColumn;
				return ret;
			}

			ret.Report = ParseReport.Ok;
			ret.Statements = new List<Statement>();
			foreach( var stat in parser.Output )
				ret.Statements.Add( stat );

			return ret;
		}

		public Line ParseLine(string line) {
			var ret = new Line();

			line = line.Trim();
			if( String.IsNullOrEmpty( line ) ) {
				ret.ErrorMessage = "Syntax error, empty line";
				ret.Report = ParseReport.EmptyLine;
				return ret;
			}

			Match ln = Regex.Match( line, @"^\s*(\d+)\s+(.*?)\s*$" );
			if( !ln.Success ) {
				ret.ErrorMessage = "Syntax error, bad format";
				ret.Report = ParseReport.LineFormatError;
				return ret;
			}

			ret.LineNum = Int32.Parse( ln.Groups[1].Value );
			string lnum = ret.LineNum + " ";
			string statements = ln.Groups[2].Value;

			ParseStatements( statements, ret );
			ret.OriginalLine = lnum + statements;
			int columnCorrection = lnum.Length;

			if( ret.ErrorColumn.HasValue )
				ret.ErrorColumn = ret.ErrorColumn.Value + columnCorrection;

			return ret;
		}

		public IEnumerable<Line> ParseLines(IEnumerable<string> lines, bool stopAfterFirstError) {
			int lineCounter = 1;
			var ret = new List<Line>();
			foreach( var line in lines ) {

				string ln = line.Trim();
				if( String.IsNullOrEmpty( ln ) )
					continue;

				Line parsedLine = ParseLine( ln );
				parsedLine.OriginalLinePosition = lineCounter++;
				ret.Add( parsedLine );

				if( parsedLine.Report != ParseReport.Ok && stopAfterFirstError )
					return ret;
			}

			return ret;
		}
	}
}
