echo Signing assembly "%~2\%~1".dll
@"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\ildasm" "%~2\%~1".dll /out:"%~3\%~1".il
@"%Windir%\Microsoft.NET\Framework64\v4.0.30319\ilasm" "%~3\%~1".il /res:"%~3\%~1".res /dll /opt /key:..\..\IgorSoft\IgorSoft.OpenSource.snk /out:"%~3\%~1".dll /quiet
@del %3\%1.il
@del %3\%1.res
