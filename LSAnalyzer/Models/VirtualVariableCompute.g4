grammar VirtualVariableCompute;

expression: term EOF;

term  
    : '-' value                                             #negation
    | <assoc=right> left=term '^' right=term                #exponent
    | left=term op=('*'|'/') right=term                     #operation
    | left=term op=('+'|'-') right=term                     #operation
    | 'isNa(' term ')'                                      #isNa
    | left=term op=('=='|'!='|'<'|'<='|'>='|'>') right=term #comparion
    | value                                                 #valueTerm
    ;

value
    : number=NUMBER     #number
    | variable=VARIABLE #variable
    | '(' term ')'      #parentheses
    ;

fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGITS : [0-9];

VARIABLE : (LOWERCASE|UPPERCASE) (LOWERCASE|UPPERCASE|DIGITS)*;
NUMBER : DIGITS+ ([.]DIGITS+)?;

WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE : ('\r'? '\n' | '\r')+ -> skip;