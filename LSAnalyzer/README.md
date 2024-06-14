# ToDo-List
Test and debug!

* split application core and UI in separate assemblies to possibly switch to AvaloniaUI
* create an automated test suite for a wider range of published trustful coefficients from known data (PIRLS, PISA school level, ...) that conducts realistic workflows
* refactoring of data table creation and management OR switch from BIFIEsurvey to a specialized R package for LSAnalyzer

# Nice-to-have
* actual list of weight variables with descriptions and optinal weight variables

# Would-be-nice-but-hard-to-implement
* handle SPSS files in folders with non-ASCII characters

No general solution found using only R-core so far!

* GUIs for BIFIE.twolevel() and BIFIE.pathmodel()

Since these functions use formula or lavaan syntax which are tools for professionals, a GUI would need to have a full graphical representation to make them accessible for the kind of users this software aims at. There are also data handling issues (assumption of sorted cluster variable) for BIFIE.twolevel().

