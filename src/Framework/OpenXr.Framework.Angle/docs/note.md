## Crash analyzer

```shell
adb logcat | "C:\Android\Sdk\ndk\30.0.14904198\ndk-stack.cmd" -sym "\\wsl.localhost\Ubuntu\home\aguer\src\angle\out\android-arm64\lib.unstripped"
```