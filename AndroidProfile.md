
## Configure

<PropertyGroup Condition="'$(DiagnosticAnd)' == 'True'">
	<DiagnosticPort>9100</DiagnosticPort>
	<DiagnosticAddress>127.0.0.1</DiagnosticAddress>
	<DiagnosticSuspend>false</DiagnosticSuspend>
	<DiagnosticListenMode>connect</DiagnosticListenMode>
	<EnableDiagnostics>true</EnableDiagnostics>
</PropertyGroup>

## Simple Call

dotnet-trace collect --dsrouter android --format speedscope

## Call

adb reverse tcp:9100 tcp:9101

dotnet-dsrouter server-server -ipcs profile-router -tcps 0.0.0.0:9101

dotnet-trace collect --diagnostic-port profile-router,connect --profile dotnet-sampled-thread-time,dotnet-common --format speedscope


## Doesn't work

adb shell setprop debug.mono.profile '127.0.0.1:9001,suspend,connect'

## Enable Log

adb shell setprop debug.mono.log default,assembly,mono_log_level=debug,mono_log_mask=all

## Online res
https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/profiling?view=net-maui-10.0

https://github.com/dotnet/diagnostics/issues/4337