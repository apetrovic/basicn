// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

namespace BasicN.Parser {
	public sealed partial class Scanner {
		private bool _ifState = false;
		private bool _onState = false;

		private void ResetState() { _ifState = false; _onState = false; }

		private Token MakeToken(string text) {
			Tokens token = GetToken( text );

			switch( token ) {
				case Tokens.If:
					_ifState = true;
					break;

				case Tokens.Then:
					_ifState = false;
					break;

				case Tokens.On:
					_onState = true;
					break;
			}

			Token ret = new Token( token, text );
			return ret;
		}

		private Tokens GetToken(string text) {
			switch( text.ToUpper() ) {
				case "=": return _ifState ? Tokens.Bool_Eq : Tokens.Eq;
				case "AND": return _ifState ? Tokens.Bool_And : Tokens.And;
				case "OR": return _ifState ? Tokens.Bool_Or : Tokens.Or;
				case "XOR": return _ifState ? Tokens.Bool_Xor : Tokens.Xor;
				case "NOT": return _ifState ? Tokens.Bool_Not : Tokens.Not;

				case "GOTO": return _onState ? Tokens.On_Goto : Tokens.Goto;
				case "GOSUB": return _onState ? Tokens.On_Gosub : Tokens.Gosub;

				case "PRINT": return Tokens.Print;
				case "INPUT": return Tokens.Input;
				case "ON": return Tokens.On;
				case "RETURN": return Tokens.Return;
				case "IF": return Tokens.If;
				case "THEN": return Tokens.Then;
				case "ELSE": return Tokens.Else;
				case "FOR": return Tokens.For;
				case "TO": return Tokens.To;
				case "STEP": return Tokens.Step;
				case "NEXT": return Tokens.Next;
				case "DIM": return Tokens.Dim;

				case "LEN": return Tokens.Len;
				case "MID$": return Tokens.MidS;
				case "LEFT$": return Tokens.LeftS;
				case "RIGHT$": return Tokens.RightS;
				case "STR$": return Tokens.StrS;
				case "CHR$": return Tokens.ChrS;
				case "ASC": return Tokens.Asc;
				case "VAL": return Tokens.Val;
				case "INT": return Tokens.Int;
				case "FRAC": return Tokens.Frac;
				case "RUN": return Tokens.Run;
				case "END": return Tokens.End;
				case "CLS": return Tokens.Cls;
				case "LET": return Tokens.Let;
				case "READ": return Tokens.Read;
				case "RANDOMIZE": return Tokens.Randomize;
				case "RND": return Tokens.Rnd;
				case "LOCATE": return Tokens.Locate;
				case "INKEY$": return Tokens.InkeyS;
				case "TIMER": return Tokens.Timer;
				case "PAUSE": return Tokens.Pause;

				case "REM":
					BEGIN( CommentState );
					ResetState();
					return Tokens.Rem;

				case "DATA":
					BEGIN( CommentState );
					ResetState();
					return Tokens.Data;
			}

			// name aren't recognized
			if( text.EndsWith( "$" ) )
				return Tokens.StringVariable;
			else
				return Tokens.NumVariable;
		}

	}
}
