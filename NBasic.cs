// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using BasicN.Interpreter;
using BasicN.Lib;
using BasicN.Tokenizer;
using System.IO;
using BasicN.Compiler;

namespace BasicN {
	public class ConsoleContext : IContext, IErrorPrinter {
		public void Print(string s) { Console.Write( s ); }
		public void PrintLine(string s) { Console.WriteLine( s ); }

		public string ReadLine() {
			Console.CursorVisible = true;
			string ret = Console.ReadLine();
			Console.CursorVisible = false;
			return ret;
		}

		public string Read() { return Console.KeyAvailable ? Console.ReadKey(true).KeyChar.ToString() : ""; }
		public void Cls() { Console.Clear(); }
		public void Locate(int x, int y) { Console.CursorLeft = x; Console.CursorTop = y; }
		public void PrintError(string message) { Console.WriteLine( message ); }
	}

	class BasicN {
		static void Main(string[] args) {
			if( args.Length < 1 ) {
				PrintUsage();
				return;
			}

			if( args.Length == 1 ) {
				Interpret( args[0], false );
				return;
			}

			if( args[0].ToLower() == "/d" && args.Length == 2 ) {
				Interpret( args[1], true );
				return;
			}

			if( args[0].ToLower() == "/c" && args.Length == 3 ) {
				DoCompile( args );
				return;
			}

			PrintUsage();
		}

		private static void Interpret(string path, bool debug){
			if( !File.Exists( path ) ) {
				Console.WriteLine( "Error opening file {0}", path );
				return;
			}

			Console.CursorVisible = false;
			var i = new NBInterpreter( new ConsoleContext() );
			i.Load( path );

			Console.CancelKeyPress += delegate {
				Console.CursorVisible = true;
				Console.WriteLine( "\n\nBreak in {0}\n\n", i.CurrentLineNumber );

				if( debug )
					i.PrintAllVariables();

				Console.WriteLine( "\n" );
			};

			i.Run();

			Console.CursorVisible = true;

			if( debug )
				i.PrintAllVariables();

			Console.WriteLine( "\n" );
		}

		private static void DoCompile(string[] args) {
			var fileName = args[1];
			if( !File.Exists( fileName ) ) {
				Console.WriteLine( "Error opening file {0}", fileName );
				return;
			}

			var programType = args[2].Split( '.' )[0] + "Class";

			Console.WriteLine( "Input file: {0}", args[1] );
			Console.WriteLine( "Output file: {0}", args[2] );
			Console.WriteLine( "Class name: {0}", programType );

			NBCompiler.Compile( new ConsoleContext(), fileName, programType, args[2] );
		}

		private static void PrintUsage() {
			Console.WriteLine( "" );
			Console.WriteLine( "Usage: " );
			Console.WriteLine( "  BasicN [program name] - interprets the program" );
			Console.WriteLine( "  BasicN /d [program name] - interprets the program (debug mode)" );
			Console.WriteLine( "  BasicN /c [program name] [output file] - compiles the program" );
			Console.WriteLine( "" );
		}
	}
}
