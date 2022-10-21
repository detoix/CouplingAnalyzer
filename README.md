![example workflow](https://github.com/detoix/CouplingAnalyzer/actions/workflows/dotnet.yml/badge.svg)

CouplingAnalyzer analyzes [solution file](CouplingAnalyzer.Tests.Resources/CouplingAnalyzer.Tests.Resources.sln) to generate [coupling report](CouplingAnalyzer.Tests/CouplingAnalyzer.Tests.Resources.tsv).

```
dotnet run path/to/solution.sln
```

Running this command will generate **solution.tsv** file next to your **solution.sln**. It's going to look like this:

| FromProject   | FromFile      | FromType  | ToProject         | ToFile | ToType |
| ------------- | ------------- | --------- | ----------------- | ------ | ------ |
| AssemblyName  | Foo.cs        | Foo       | OtherAssemblyName | Bar.cs | Bar    |

Having this data you can put it into Excel spreadsheet, create a pivot table and analyze coupling efficiently.
