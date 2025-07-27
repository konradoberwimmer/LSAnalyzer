Maintenance is organized via issues in GitHub. The only thing to keep here is ... 

# Would-be-nice-but-hard-to-implement
* handle SPSS files in folders with non-ASCII characters

No general solution found using only R-core so far!

* GUIs for BIFIE.twolevel() and BIFIE.pathmodel()

Since these functions use formula or lavaan syntax which are tools for professionals, a GUI would need to have a full graphical representation to make them accessible for the kind of users this software aims at. There are also data handling issues (assumption of sorted cluster variable) for BIFIE.twolevel().

