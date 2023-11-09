# ToDo-List
* add two-level regression (with/without assumption of sorted cluster variable) 
* re-applying analyses on current file or file from analysis configuration in json
* think of a way of making character vectors usable or to not offer them for analyses

# Would-be-nice-but-hard-to-implement
* implement variable display name to hide away regex in PVs

BIFIEsurveys options for choosing PV vars are limited and possibly corrupt. This leads to regex magic in dataset types like PISA, eg. "PV[0-9]+MATH$" ...<br>
It would be nice if such variables were then shown without the "technical" parts of the regex, eg. "PVMATH". But this is very hard to do as there is no general logic on what are "technical" and non-technical elements of the regex. Even when such a thing - having a display name instead of BIFIEsurveys internal name - was achieved, it would have to be set in result tables as well as many GUI elements etc.

* GUI for BIFIE.pathmodel()

Since this function uses lavaan syntax which is a tool for professionals, a GUI would need to have a full graphical representation to make it accessible for the kind of users this software aims at.

