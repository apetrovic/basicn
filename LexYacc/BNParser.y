%namespace BasicN.Parser
%partial
%parsertype BNParser
%visibility public
%valuetype BNValue

%start line

%token<Token> Eq Plus Minus Mul Div OpenBrace CloseBrace Semicolon Comma
%token<Token> And Or Xor Not
%token<Token> Lt Le Gt Ge Neq
%token<Token> Bool_Eq Bool_And Bool_Or Bool_Xor Bool_Not
%token<Token> Number String NumVariable StringVariable Integer
%token<Token> Print Input Goto Gosub Return If Then Else Rem Data For To Next Step Dim Pause
%token<Token> On On_Goto On_Gosub
%token<Token> Len MidS LeftS RightS StrS ChrS Asc Val Int Frac Run End Cls Let Read Randomize Rnd Locate InkeyS Timer
%token<Token> Separator NewLine
%token<Token> UMINUS IFX

%left Not
%left And Or Xor
%left Lt Le Gt Ge Neq
%left Plus Minus
%left Mul Div
%nonassoc UMINUS IFX Else

%type<Statements> statementList
%type<Statement> statement printStatement
%type<Value> printList printGroup integerList onJumpStatement arrayList numList
%type<Variable> variable array
%type<NumStatement> numStatement numExpression numBinaryOperator numEquals number integer numVariable numArray numFunction
%type<StringStatement> stringStatement stringExpression stringBinaryOperator stringEquals string stringVariable stringArray
%type<StringStatement> stringFunction sf_mid
%type<BooleanStatement> booleanStatement booleanExpression numBooleanExpression stringBooleanExpression
%type<Keyword> keyword kw_print kw_input kw_if kw_on kw_for kw_dim kw_read

%%

line			:	statementList						{ Output = $1; }
				;

statementList	:	statement							{ $$ = AddToList<Statement>( new List<Statement>(), $1 ); }
				|	statementList Separator statement	{ $$ = AddToList<Statement>( (List<Statement>)$1, $3 ); }
				;

statement		:	numEquals				{ $$ = $1; }
				|	stringEquals			{ $$ = $1; }
				|	keyword
				;

numEquals		:	numVariable Eq numStatement				{ $$ = new NumBinaryOperator( $1, $3, "=" ); }
				|	Let numVariable Eq numStatement			{ $$ = new NumBinaryOperator( $2, $4, "=" ); }
				|	numArray Eq numStatement				{ $$ = new NumBinaryOperator( $1, $3, "=" ); }
				;

numStatement	:	numExpression			{ $$ = $1; }
				|	numEquals				{ $$ = $1; }
				;

numExpression	:	Minus numExpression %prec UMINUS	{ $$ = new NumUnaryMinus( $2 ); }
				|	numFunction							{ $$ = $1; }
				|	numVariable							{ $$ = $1; }
				|	numArray							{ $$ = $1; }
				|	number								{ $$ = $1; }
				|	numBinaryOperator					{ $$ = $1; }
				|	OpenBrace numExpression CloseBrace	{ $$ = $2; }
				|	Not numExpression					{ $$ = new NotOperator( $2 ); }
				;

numFunction		:	Len OpenBrace stringExpression CloseBrace		{ $$ = new NfLen( $3 ); }
				|	Asc OpenBrace stringExpression CloseBrace		{ $$ = new NfAsc( $3 ); }
				|	Val OpenBrace stringExpression CloseBrace		{ $$ = new NfVal( $3 ); }
				|	Int OpenBrace numExpression CloseBrace			{ $$ = new NfInt( $3 ); }
				|	Frac OpenBrace numExpression CloseBrace			{ $$ = new NfFrac( $3 ); }
				|	Rnd OpenBrace numExpression CloseBrace			{ $$ = new NfRnd( $3 ); }
				|	Timer											{ $$ = new NfTimer(); }
				;

variable		:	numVariable				{ $$ = (Variable)$1; }
				|	stringVariable			{ $$ = (Variable)$1; }
				;

array			:	numArray				{ $$ = (Variable)$1; }
				|	stringArray				{ $$ = (Variable)$1; }
				;

numVariable		:	NumVariable				{ $$ = new NumVariable( $1.Str ); }
				;

numArray		:	NumVariable OpenBrace numList CloseBrace	{ $$ = new NumArray( $1.Str, (List<NumStatement>)$3 ); }
				;

numList			:	numExpression					{ $$ = AddToList<NumStatement>( new List<NumStatement>(), $1 ); }
				|	numList Comma numExpression		{ $$ = AddToList<NumStatement>( (List<NumStatement>)$1, $3 ); }
				;

integerList		:	integer							{ $$ = AddToList<Integer>( new List<Integer>(), (Integer)$1 ); }
				|	integerList Comma integer		{ $$ = AddToList<Integer>( (List<Integer>)$1, (Integer)$3 ); }
				;

number			:	Number					{ $$ = new NumConstant( $1.Str ); }
				|	integer					{ $$ = $1; }
				;

integer			: Integer					{ $$ = new Integer( $1.Str ); }
				;


numBinaryOperator	:	numExpression Mul numExpression			{ $$ = new NumBinaryOperator( $1, $3, "*" ); }
					|	numExpression Div numExpression			{ $$ = new NumBinaryOperator( $1, $3, "/" ); }
					|	numExpression Plus numExpression		{ $$ = new NumBinaryOperator( $1, $3, "+" ); }
					|	numExpression Minus numExpression		{ $$ = new NumBinaryOperator( $1, $3, "-" ); }
					|	numExpression And numExpression			{ $$ = new NumBinaryOperator( $1, $3, "&" ); }
					|	numExpression Or numExpression			{ $$ = new NumBinaryOperator( $1, $3, "|" ); }
					|	numExpression Xor numExpression			{ $$ = new NumBinaryOperator( $1, $3, "^" ); }
					;

stringEquals		:	stringVariable Eq stringStatement		{ $$ = new StringBinaryOperator( $1, $3, "=" ); }
					|	Let stringVariable Eq stringStatement	{ $$ = new StringBinaryOperator( $2, $4, "=" ); }
					|	stringArray Eq stringStatement			{ $$ = new StringBinaryOperator( $1, $3, "=" ); }
					;


stringStatement		:	stringExpression		{ $$ = $1; }
					|	stringEquals			{ $$ = $1; }
					;

stringExpression	:	stringVariable			{ $$ = $1; }
					|	stringArray				{ $$ = $1; }
					|	string					{ $$ = $1; }
					|	stringBinaryOperator	{ $$ = $1; }
					|	stringFunction			{ $$ = $1; }
					;

stringVariable		:	StringVariable			{ $$ = new StringVariable( $1.Str ); }
					;

stringArray			:	StringVariable OpenBrace numList CloseBrace	{ $$ = new StringArray( $1.Str, (List<NumStatement>)$3 ); }
					;

string				:	String					{ $$ = new StringConstant( $1.Str ); }
					;

stringBinaryOperator	:	stringExpression Plus stringExpression  { $$ = new StringBinaryOperator( $1, $3, "+" ); }
						;

stringFunction		:	sf_mid																	{ $$ = $1; }
					|	LeftS OpenBrace stringExpression Comma numExpression CloseBrace			{ $$ = new SfLeft( $3, $5 ); }
					|	RightS OpenBrace stringExpression Comma numExpression CloseBrace		{ $$ = new SfRight( $3, $5 ); }
					|	InkeyS																	{ $$ = new SfInkey(); }
					;

sf_mid				:	MidS OpenBrace stringExpression Comma numExpression CloseBrace						{ $$ = new SfMid( $3, $5, null ); }
					|	MidS OpenBrace stringExpression Comma numExpression Comma numExpression CloseBrace	{ $$ = new SfMid( $3, $5, $7 ); }
					|	StrS OpenBrace numExpression CloseBrace												{ $$ = new SfStr( $3 ); }
					|	ChrS OpenBrace numExpression CloseBrace												{ $$ = new SfChr( $3 ); }
					;

keyword				:	kw_print									{ $$ = $1; }
					|	kw_input									{ $$ = $1; }
					|	kw_if										{ $$ = $1; }
					|	kw_on										{ $$ = $1; }
					|	kw_for										{ $$ = $1; }
					|	kw_dim										{ $$ = $1; }
					|	kw_read										{ $$ = $1; }
					|	Rem											{ $$ = new KwRem($1); }
					|	Data										{ $$ = new KwData($1); }
					|	Goto integer								{ $$ = new KwGoto((Integer)$2); }
					|	Gosub integer								{ $$ = new KwGosub((Integer)$2); }
					|	Return										{ $$ = new KwReturn(); }
					|	Next numVariable							{ $$ = new KwNext((NumVariable)$2); }
					|	Randomize numExpression						{ $$ = new KwRandomize($2); }
					|	Run											{ $$ = new KwRun(); }
					|	Cls											{ $$ = new KwCls(); }
					|	End											{ $$ = new KwEnd(); }
					|	Locate numExpression Comma numExpression	{ $$ = new KwLocate( $2, $4 ); }
					|	Pause numExpression							{ $$ = new KwPause( $2 ); }
					;

kw_print			:	Print							{ $$ = new KwPrint(); }
					|	Print printStatement			{ $$ = new KwPrint( $2 ); }
					|	Print printList					{ $$ = new KwPrint( $2 ); }
					|	Print printList printStatement	{ $$ = new KwPrint( $2, $3 ); }
					;

printList			:	printGroup						{ $$ = KwPrint.AddToList( null, $1 ); }
					|	printList printGroup			{ $$ = KwPrint.AddToList( $1, $2 ); }
					;

printGroup			:	Semicolon						{ $$ = new KwPrint.Group( new StringConstant(""), ";" ); }
					|	Comma							{ $$ = new KwPrint.Group( new StringConstant(""), "," ); }
					|	printStatement Semicolon		{ $$ = new KwPrint.Group( $1, ";" ); }
					|	printStatement Comma			{ $$ = new KwPrint.Group( $1, "," ); }
					;

printStatement		:	numStatement				{ $$ = $1; }
					|	stringStatement				{ $$ = $1; }
					;

kw_input			:	Input variable								{ $$ = new KwInput($2); }
					|	Input string Semicolon variable				{ $$ = new KwInput($4, (StringConstant)$2, ';'); }
					|	Input string Comma variable					{ $$ = new KwInput($4, (StringConstant)$2, ','); }
					|	Input array									{ $$ = new KwInput($2); }
					|	Input string Semicolon array				{ $$ = new KwInput($4, (StringConstant)$2, ';'); }
					|	Input string Comma array					{ $$ = new KwInput($4, (StringConstant)$2, ','); }
					;

kw_if				:	If booleanStatement Then statementList %prec IFX			{ $$ = new KwIf( $2, $4, null ); }
					|	If booleanStatement Then statementList Else statementList	{ $$ = new KwIf( $2, $4, $6 ); }
					;

kw_read				:	Read variable								{ $$ = new KwRead($2); }
					|	Read array									{ $$ = new KwRead($2); }
					;


booleanStatement		:	booleanExpression								{ $$ = $1; }
						|	Bool_Not booleanStatement						{ $$ = new NotBooleanOperator( $2 ); }
						|	booleanStatement Bool_And booleanStatement		{ $$ = new BoolBinaryOperator( $1, $3, "&&" ); }
						|	booleanStatement Bool_Or booleanStatement		{ $$ = new BoolBinaryOperator( $1, $3, "||" ); }
						|	booleanStatement Bool_Xor booleanStatement		{ $$ = new BoolBinaryOperator( $1, $3, "^^" ); }
						|	OpenBrace booleanStatement CloseBrace			{ $$ = $2; }
						;

booleanExpression		:	numBooleanExpression						{ $$ = $1; }
						|	stringBooleanExpression						{ $$ = $1; }
						;

numBooleanExpression	:	numExpression Bool_Eq numExpression			{ $$ = new NumBoolBinaryOperator( $1, $3, "==" ); }
						|	numExpression Lt numExpression				{ $$ = new NumBoolBinaryOperator( $1, $3, "<" ); }
						|	numExpression Le numExpression				{ $$ = new NumBoolBinaryOperator( $1, $3, "<=" ); }
						|	numExpression Gt numExpression				{ $$ = new NumBoolBinaryOperator( $1, $3, ">" ); }
						|	numExpression Ge numExpression				{ $$ = new NumBoolBinaryOperator( $1, $3, ">=" ); }
						|	numExpression Neq numExpression				{ $$ = new NumBoolBinaryOperator( $1, $3, "<>" ); }
						;

stringBooleanExpression	:	stringExpression Bool_Eq stringExpression	{ $$ = new StringBoolBinaryOperator( $1, $3, "==" ); }
						|	stringExpression Lt stringExpression		{ $$ = new StringBoolBinaryOperator( $1, $3, "<" ); }
						|	stringExpression Le stringExpression		{ $$ = new StringBoolBinaryOperator( $1, $3, "<=" ); }
						|	stringExpression Gt stringExpression		{ $$ = new StringBoolBinaryOperator( $1, $3, ">" ); }
						|	stringExpression Ge stringExpression		{ $$ = new StringBoolBinaryOperator( $1, $3, ">=" ); }
						|	stringExpression Neq stringExpression		{ $$ = new StringBoolBinaryOperator( $1, $3, "<>" ); }
						;

kw_on					:	On numExpression onJumpStatement				{ $$ = new KwOn( $2, $3 ); }
						;

onJumpStatement			:	On_Goto integerList							{ $$ = KwOn.MakeGotoJumpList( $2 ); }
						|	On_Gosub integerList						{ $$ = KwOn.MakeGosubJumpList( $2 ); }
						;

kw_for					:	For numVariable Eq numStatement To numStatement						{ $$ = new KwFor( (NumVariable)$2, $4, $6, null ); }
						|	For numVariable Eq numStatement To numStatement Step numStatement	{ $$ = new KwFor( (NumVariable)$2, $4, $6, $8 ); }
						;

kw_dim					:	Dim arrayList								{ $$ = new KwDim( (List<VariableArray>)$2 ); }
						;

arrayList				:	array										{ $$ = AddToList<VariableArray>( new List<VariableArray>(), (VariableArray)$1 ); }
						|	arrayList Comma array						{ $$ = AddToList<VariableArray>( (List<VariableArray>)$1, (VariableArray)$3 ); }
						;