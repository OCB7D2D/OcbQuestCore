@echo off

call MC7D2D QuestCore.dll /reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" ^
  -recurse:Source\*.cs && echo Successfully compiled QuestCore.dll

pause