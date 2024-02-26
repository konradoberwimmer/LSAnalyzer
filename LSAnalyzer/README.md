# ToDo-List
Test and debug!

* WIP dataverse support: 
	* test current functionality
	* suppress batch analyze reload of files from data provider
	* add file type to dataverse data provider
* FIX: batch analyze should reapply subsetting when not reloading files
* add more default dataset types: class and school level variante, TIMSS 2023, ICILS, PIAAC, ...
* build the MSI for IQS software center
* split application core and UI in separate assemblies to possibly switch to AvaloniaUI
* create an automated test suite for a wider range of published trustful coefficients from known data (PIRLS, PISA school level, ...) that conducts realistic workflows
* refactoring of data table creation and management OR switch from BIFIEsurvey to a specialized R package for LSAnalyzer

# Would-be-nice-but-hard-to-implement
* implement variable display name to hide away regex in PVs

BIFIEsurveys options for choosing PV vars are limited and possibly corrupt. This leads to regex magic in dataset types like PISA, eg. "PV[0-9]+MATH$" ...<br>
It would be nice if such variables were then shown without the "technical" parts of the regex, eg. "PVMATH". But this is very hard to do as there is no general logic on what are "technical" and non-technical elements of the regex. Even when such a thing - having a display name instead of BIFIEsurveys internal name - was achieved, it would have to be set in result tables as well as many GUI elements etc.

* GUIs for BIFIE.twolevel() and BIFIE.pathmodel()

Since these functions use formula or lavaan syntax which are tools for professionals, a GUI would need to have a full graphical representation to make them accessible for the kind of users this software aims at. There are also data handling issues (assumption of sorted cluster variable) for BIFIE.twolevel().

