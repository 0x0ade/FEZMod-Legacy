#!/bin/bash
cd FEZ.Mod.mm
perl -0777 -pi -e 's/public static string Version = ".*";/public static string Version = "dev-'${BUILD_NUMBER}'";/gm' ./FEZ.Mod.mm/FezGame/Mod/FEZMod.cs
xbuild
cd ..
