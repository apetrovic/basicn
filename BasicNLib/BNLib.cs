// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;

namespace BasicN.Lib {
	public interface IContext {
		void Print(string s);
		void PrintLine(string s);
		string ReadLine();
		string Read();
		void Cls();
		void Locate(int x, int y);
	}

	public class DefaultConsoleContext : IContext {
		public void Print(string s) { Console.Write( s ); }
		public void PrintLine(string s) { Console.WriteLine( s ); }

		public string ReadLine() {
			Console.CursorVisible = true;
			string ret = Console.ReadLine();
			Console.CursorVisible = false;
			return ret;
		}

		public string Read() { return Console.KeyAvailable ? Console.ReadKey( true ).KeyChar.ToString() : ""; }
		public void Cls() { Console.Clear(); }
		public void Locate(int x, int y) { Console.CursorLeft = x; Console.CursorTop = y; }
	}

	public class BNLib {
		public static void InitStringArray(Array stringArray) {
			int[] coordinates = new int[stringArray.Rank];

			while( true ) {
				stringArray.SetValue( "", coordinates );
				coordinates[0]++;

				if( coordinates[0] > stringArray.GetUpperBound( 0 ) ) { // carry
					coordinates[0] = 0;

					bool found = false;
					for( int x = 1; x < stringArray.Rank; ++x ) {
						coordinates[x]++;
						if( coordinates[x] <= stringArray.GetUpperBound( x ) ) {
							found = true;
							break;
						}

						coordinates[x] = 0;
					}

					if( !found )
						return;
				}
			}
		}

		public static string Left(string str, int charCount) {
			return charCount <= 0 ? "" : str.Substring( 0, charCount > str.Length ? str.Length : charCount );
		}

		public static string Right(string str, int charCount) {
			if( charCount <= 0 )
				return "";

			int pos = str.Length - charCount;
			return pos <= 0 ? str : str.Substring( pos );
		}

		public static string Mid(string str, int beginIndex, int len) {
			beginIndex -= 1;
			if( beginIndex < 0 || beginIndex > str.Length || len <= 0 )
				return "";

			return beginIndex + len > str.Length ? str.Substring( beginIndex ) : str.Substring( beginIndex, len );
		}

		public static double InputDouble(IContext context, string prompt, bool questionMark) {
			PrintPrompt( context, prompt, questionMark );
			double ret;
			for( ; ; ) {
				string read = context.ReadLine();
				if( double.TryParse( read, out ret ) )
					break;

				context.PrintLine( "Error!" );
				PrintPrompt( context, prompt, questionMark );
			}

			return ret;
		}

		public static string InputString(IContext context, string prompt, bool questionMark) {
			PrintPrompt( context, prompt, questionMark );
			return context.ReadLine();
		}

		private static void PrintPrompt(IContext context, string prompt, bool questionMark) {
			if( prompt != null )
				context.Print( prompt );

			if( questionMark )
				context.Print( "? " );

		}
	}
}
