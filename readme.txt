Overview


BasicN is simple interpreter and compiler for BASIC programs. BasicN
and programs compiled with BasicN requires .NET 3.5 framework (and above)
to run.

I wrote BasicN couple of years ago, part for fun, part as a learning exercise,
and part for sentimental reasons, to mark 25 years since I ran my first program
on friend's Commodore 64.

There's one example program in folder Example (Letters.nb), a simple typing exercise.




BASIC


BasicN dialect recognizes following keywords:

AND, OR, XOR, NOT
GOTO, GOSUB, ON X GOTO, ON X GOSUB, RETURN
PRINT, INPUT, CLS, LOCATE
IF - THEN - ELSE, FOR - TO - STEP - NEXT
LET, DIM
LEN, MID$, LEFT$, RIGHT$, STR$, CHR$
ASC, VAL, INT, FRAC
RUN, END, PAUSE, REM, INKEY, TIMER
READ, DATA, RANDOMIZE, RND

All program lines must have numbers at the start.
String variables must have suffix $, like A$, NAME$.
Arrays are allocated with DIM.




About the code


BasicN uses Garden Point Parser Generator (gppg.codeplex.com) for lexing and parsing
BASIC code. GPPG uses lex/yacc like syntax for lexer and parser definition. BasicN
lexer is defined in GPPG\BNParserLex.lex. For parser definition look at GPPG\NBParser.y.

Parsed program is handled by tokenizer (files in Tokenizer folder). The role of the tokenizer
is simplification of the program - all loops are reduced to IFs and GOTOs, ON x GOTO
and ON x GOSUB statements are converted to series of IFs, etc. Tokenizer output is more
suitable for the compiler and interpreter part than "raw" parser output.

The Compiler (Compiler folder) and Interpreter (Interpreter folder) parts works with
tokenizer output to produce standard .net executable or to interpret the program line
by line.




License

BasicN is licensed under Simplified BSD License. See file copyright.txt for details.




Contact

For any questions or comments about the code contact me at apetrovic (at) gmail dot com.