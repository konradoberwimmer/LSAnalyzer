grammar VirtualVariableCompute;

expression : term EOF;
term : VARIABLE | NUMBER | ((VARIABLE | NUMBER) OPERATOR term);

fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGITS : [0-9];

VARIABLE : (LOWERCASE|UPPERCASE) (LOWERCASE|UPPERCASE|DIGITS)*;
NUMBER : [-]? DIGITS+ ([.]DIGITS+)?;
OPERATOR : ('+'|'-'|'*'|'/');

WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE : ('\r'? '\n' | '\r')+ -> skip;