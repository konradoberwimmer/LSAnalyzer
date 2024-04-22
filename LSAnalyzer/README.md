# ToDo-List
Test and debug!

* handle SPSS files in folders with non-ASCII characters
* add more default dataset types: class and school level variants, TIMSS 2023, ICILS, PIAAC, ...
* build the MSI for IQS software center
* split application core and UI in separate assemblies to possibly switch to AvaloniaUI
* create an automated test suite for a wider range of published trustful coefficients from known data (PIRLS, PISA school level, ...) that conducts realistic workflows
* refactoring of data table creation and management OR switch from BIFIEsurvey to a specialized R package for LSAnalyzer
* dynamically show warning message for logistic regression with groups when BIFIEsurvey version is too low
* correct display of buttons for moving around variables in request analysis dialogs; also implement double-click as a way to move around variables
* implement (optional) specification of variable display names to hide away regex in PVs
* success message at dataset type import and after reloading defaults; add incremental number on dataset type import when name is already in use

# Would-be-nice-but-hard-to-implement
* GUIs for BIFIE.twolevel() and BIFIE.pathmodel()

Since these functions use formula or lavaan syntax which are tools for professionals, a GUI would need to have a full graphical representation to make them accessible for the kind of users this software aims at. There are also data handling issues (assumption of sorted cluster variable) for BIFIE.twolevel().

