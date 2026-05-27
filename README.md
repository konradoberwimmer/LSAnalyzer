# LSAnalyzer Repo
LSAnalyzer is my take on a software that enables GUI-driven data analysis for Large-Scale Assessment studies like PISA, PIRLS and so forth. Therefore, it might provide an alternative to IEA IDB Analyzer ([https://www.iea.nl/data-tools/tools](https://www.iea.nl/data-tools/tools)), but relies on a very different tech stack.

Any version is written in C#, uses .NET (>=7.0) and follows the MVVM pattern.

## Version 1 - R-based and Windows-only
The first version (this repository) uses a fork of [R.NET](https://www.nuget.org/packages/R.NET/) to call into an R.dll and makes calculations via R-package [BIFIEsurvey](https://cran.r-project.org/web/packages/BIFIEsurvey/index.html), which is co-authored by me. In order to access R.dll, a separate R installation is necessary und package BIFIEsurvey has to be installed in its global library.

Version 1 uses WPF as GUI framework and is therefore Windows-only.

Version 1 has [releases](https://github.com/konradoberwimmer/LSAnalyzer/releases) and will be further maintained (bugfixes and minor extensions).

Besides the main project, there are projects for the Microsoft installer (powered by the WiX toolset) and a test project with unit and integration tests (xUnit).

### Quality Assurance

Version 1 has already been in use in several publication projects of the Federal Institute for Quality Assurance in the Austrian Schooling System (IQS). In May 2026, a systematic check for agreeing coefficients between calculations done with LSAnalyzer and the following publications was conducted by Nina Rheinfrank (IQS):

- George, A. C., Schreiner, C., Wiesner, C., Pointinger, M., & Pacher, K. (Eds.). (2019). _Fünf Jahre flächendeckende Bildungsstandardüberprüfungen in Österreich: Vertiefende Analysen zum Zyklus 2012 bis 2016 [Five years of nationwide educational standards assessments in Austria: In-depth analyses of the 2012–2016 cycle]_. Waxmann. (chapters 6 and 9; using a specific, non-disclosed dataset type configuration)
- Rölz, M., & Höller, I. (Eds.). (2024). _ICILS 2023: Digitale Kompetenzen von Schülerinnen und Schülern im internationalen Vergleich [Five years of nationwide educational standards assessments in Austria: In-depth analyses of the 2012–2016 cycle]_. Institut für Qualitätssicherung im österreichischen Schulwesen. [https://doi.org/10.17888/icils2023-eb](https://doi.org/10.17888/icils2023-eb) (default dataset type configuration [601_icils_student.json](https://konradoberwimmer.github.io/LSAnalyzer/601_icils_student.json))
- Schmich, J., & Itzlinger-Bruneforth, U. (Eds.). (2019). _TALIS 2018 (Band 1): Rahmenbedingungen des schulischen Lehrens und Lernens aus Sicht von Lehrkräften und Schulleitungen im internationalen Vergleich [TALIS 2018 (Part 1): Frameworks for teaching and learning in schools from the perspective of teachers and principals in an international comparison]_. Bundesinstitut für Bildungsforschung, Innovation und Entwicklung des österreichischen Schulwesens. [https://doi.org/10.17888/talis2018-1](https://doi.org/10.17888/talis2018-1) (checking appendix [https://doi.org/10.17888/talis2018-1-dat](https://doi.org/10.17888/talis2018-1-dat); default dataset type configurations [401_talis_principal.json](https://konradoberwimmer.github.io/LSAnalyzer/401_talis_principal.json) and [402_talis_teacher.json](https://konradoberwimmer.github.io/LSAnalyzer/402_talis_teacher.json))
- Schmich, J., Wallner-Paschon, C., & Illetschko, M. (Eds.). (2023). _PIRLS 2021: Die Lesekompetenz am Ende der Volksschule: Erste Ergebnisse [PIRLS 2021: Reading literacy at the end of elementary school: First results]_. Institut für Qualitätssicherung im österreichischen Schulwesen. [https://doi.org/10.17888/pirls2021-eb.2](https://doi.org/10.17888/pirls2021-eb.2) (default dataset type configuration [102_pirls_since_2016_student.json](https://konradoberwimmer.github.io/LSAnalyzer/102_pirls_since_2016_student.json))
- Suchań, B., Höller, I., & Wallner-Paschon, C. (Eds.). (2019). _PISA 2018: Grundkompetenzen am Ende der Pflichtschulzeit im internationalen Vergleich [PISA 2018: Basis skills at the end of compulsory education in international comparison]_. Leykam. [https://doi.org/10.17888/pisa2018-eb](https://doi.org/10.17888/pisa2018-eb) (default dataset type configuration [302_pisa_since_2015.json](https://konradoberwimmer.github.io/LSAnalyzer/302_pisa_since_2015.json))

Overall, results cited in those publications were almost always reproducable. In case of (generally small) discrepancies, explanations due to differences in methodology (like applying sum-preserving rounding or non-linear regression modelling) were found.

## Version 2 - "Stand-alone" and cross-platform
The second version in repository [https://github.com/konradoberwimmer/LSAnalyzer2/](https://github.com/konradoberwimmer/LSAnalyzer2/) aims at overcoming the limitations version 1 imposes: On the one hand, I try to cut the dependency to an R installation by including my own Rust crate [replicest](https://github.com/konradoberwimmer/replicest) for calculations. On the other hand, [Avalonia](https://www.nuget.org/packages/avalonia) is used as GUI framework, making LSAnalyzer cross-platform with Windows, Linux and MacOS as targets.

Version 2 is very much **work in progress**.