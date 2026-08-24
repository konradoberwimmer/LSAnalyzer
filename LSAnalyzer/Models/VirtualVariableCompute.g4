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
    : number=NUMBER     #number
    | variable=VARIABLE #variable
    | 'isNa(' term ')'  #isNa
    | '(' term ')'      #parentheses
    ;

fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGITS : [0-9];

VARIABLE : (LOWERCASE|UPPERCASE) (LOWERCASE|UPPERCASE|DIGITS)*;
NUMBER : DIGITS+ ([.]DIGITS+)?;

WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE : ('\r'? '\n' | '\r')+ -> skip;