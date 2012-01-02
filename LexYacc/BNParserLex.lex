%namespace BasicN.Parser

alpha [a-zA-Z]
number [0-9]
allchars [a-zA-Z0-9_]
string \"[^"]*\" 
open_string \"[^"]*
whitespace [ \t]
newline \r\n

%x CommentState

%%

{newline}						{ ResetState(); yylval.Token = new Token(Tokens.NewLine, ""); return (int)Tokens.NewLine; }
\:								{ ResetState(); yylval.Token = new Token(Tokens.Separator, yytext); return (int)Tokens.Separator; }

{number}+						{ yylval.Token = new Token(Tokens.Integer, yytext); return (int)Tokens.Integer; }
({number}*)+(\.{number}+)?		{ yylval.Token = new Token(Tokens.Number, yytext); return (int)Tokens.Number; }
{whitespace}+					{ /* ignore */ }
{alpha}{allchars}*\$?			{ yylval.Token = MakeToken(yytext); return (int)yylval.Token.Kind; }
{string}						{ yylval.Token = new Token(Tokens.String, yytext); return (int)Tokens.String; }

\+								{ yylval.Token = new Token(Tokens.Plus, yytext); return (int)Tokens.Plus; }
\-								{ yylval.Token = new Token(Tokens.Minus, yytext); return (int)Tokens.Minus; }
\*								{ yylval.Token = new Token(Tokens.Mul, yytext); return (int)Tokens.Mul; }
\/								{ yylval.Token = new Token(Tokens.Div, yytext); return (int)Tokens.Div; }
\(								{ yylval.Token = new Token(Tokens.OpenBrace, yytext); return (int)Tokens.OpenBrace; }
\)								{ yylval.Token = new Token(Tokens.CloseBrace, yytext); return (int)Tokens.CloseBrace; }
\;								{ yylval.Token = new Token(Tokens.Semicolon, yytext); return (int)Tokens.Semicolon; }
,								{ yylval.Token = new Token(Tokens.Comma, yytext); return (int)Tokens.Comma; }

">"								{ yylval.Token = new Token(Tokens.Gt, yytext); return (int)Tokens.Gt; }
"<"								{ yylval.Token = new Token(Tokens.Lt, yytext); return (int)Tokens.Lt; }
">="							{ yylval.Token = new Token(Tokens.Ge, yytext); return (int)Tokens.Ge; }
"<="							{ yylval.Token = new Token(Tokens.Le, yytext); return (int)Tokens.Le; }
"<>"							{ yylval.Token = new Token(Tokens.Neq, yytext); return (int)Tokens.Neq; }

\=								{ yylval.Token = MakeToken(yytext); return (int)yylval.Token.Kind; }

{whitespace}+{newline}			{ /* ignore */ }

.								{ ResetState(); yylval.Token = new Token(Tokens.error, yytext); return (int)Tokens.error; }
{open_string}					{ ResetState(); yylval.Token = new Token(Tokens.error, yytext); return (int)Tokens.error; }


<CommentState>.					{ yylval.Token.Str += yytext; }
<CommentState>\n				{ BEGIN(0); ResetState(); yylval.Token = new Token(Tokens.NewLine, ""); return (int)Tokens.NewLine; }