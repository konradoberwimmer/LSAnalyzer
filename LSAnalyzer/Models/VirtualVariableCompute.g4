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
    : number=NUMBER                                                #number
    | variable=ID                                                  #variable
    | 'isNA(' term ')'                                             #isNa
    | func=('sum('|'mean('|'factorscores(') termlist optbool? ')'  #combine
    | func=('linear('|'logarithmic(') term optnumber* optbool? ')' #scale
    | '(' term ')'                                                 #parentheses
    ;

termlist: term | (term ',' termlist);
optbool: ',' param=ID '=' val=BOOLEAN;
optnumber: ',' param=ID '=' val=NUMBER;

fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGITS : [0-9];

BOOLEAN : ('T'|'F');
ID : (LOWERCASE|UPPERCASE) (LOWERCASE|UPPERCASE|DIGITS)*;
NUMBER : DIGITS+ ([.]DIGITS+)?;

WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE : ('\r'? '\n' | '\r')+ -> skip;