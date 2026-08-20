grammar VirtualVariableCompute;

expression: term EOF;

term  
    : '-' value
    | term op=('*'|'/') term
    | term op=('+'|'-') term
    | value
    ;

value
    : NUMBER
    | variable
    | '(' term ')'
    ;

variable : VARIABLE;

fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGITS : [0-9];

VARIABLE : (LOWERCASE|UPPERCASE) (LOWERCASE|UPPERCASE|DIGITS)*;
NUMBER : DIGITS+ ([.]DIGITS+)?;

WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE : ('\r'? '\n' | '\r')+ -> skip;