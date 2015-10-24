#!/bin/bash
cd FEZ.Mod.mm
if [ -z ${BUILD_BUMBER+x} ]; then
  echo 'build_main: Leaving version default.'
else
  echo 'build_main: Replacing version string in FEZMod.'
  perl -0777 -pi -e 's/public static string Version = ".*";/public static string Version = "dev-'${BUILD_NUMBER}'";/gm' ./FezGame/Mod/FEZMod.cs
fi
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
