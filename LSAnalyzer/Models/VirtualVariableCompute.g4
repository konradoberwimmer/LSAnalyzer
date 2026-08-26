grammar VirtualVariableCompute;

expression: term EOF;

term  
    : op=('-'|'!') value                                    #negation
    | <assoc=right> left=term '^' right=term                #exponent
    | left=term op=('*'|'/') right=term                     #operation
    | left=term op=('+'|'-') right=term                     #operation
    | left=term op=('=='|'!='|'<'|'<='|'>='|'>') right=term #comparison
    | left=term op=('&'|'|') right=term                     #boolean
    | value                                                 #valueTerm
    ;

value
    : number=NUMBER                                                                          #number
    | variable=ID                                                                            #variable
    | 'isNA(' term ')'                                                                       #isNa
    | func=('sum('|'mean('|'factorscores(') tl+=term (',' tl+=term)* optbool? ')'            #combine
    | func=('linear('|'logarithmic(') term optnumber* optbool? ')'                           #scale
    | 'recode(' (tl+=term | ('[' tl+=term (',' tl+=term)* ']')) ',' '\'' recodeexpr '\'' ')' #recode
    | '(' term ')'                                                                           #parentheses
    ;

atomicnumber
    : '-' NUMBER
    | NUMBER
    ;

atomicnumberna
    : NA
    | atomicnumber
    ;

optbool: ',' param=ID '=' val=BOOLEAN;
optnumber: ',' param=ID '=' val=atomicnumber;
recodeexpr
    : (rl+=recoderule (';' rl+=recoderule)* ';')? elseexpr?
    | rl+=recoderule (';' rl+=recoderule)*
    ;
elseexpr: 'else' '=' elseval;
elseval: ID | atomicnumberna;
recoderule: criterion '=' recodeval=atomicnumberna;
criterion: cl+=critterm | ('[' cl+=critterm (',' cl+=critterm)* ']');
critterm
    : na=NA   
    | num=atomicnumber
    | left=atomicnumber '-' right=atomicnumber
    | op=('<='|'>=') num=atomicnumber
    ;

fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGITS : [0-9];

BOOLEAN : ('T'|'F');
NA : 'NA';
ID : (LOWERCASE|UPPERCASE) (LOWERCASE|UPPERCASE|DIGITS)*;
NUMBER : DIGITS+ ([.]DIGITS+)?;

WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE : ('\r'? '\n' | '\r')+ -> skip;